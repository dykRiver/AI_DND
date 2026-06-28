using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DHY.MG.Module.Sys.Dtos;
using DHY.MG.Module.Sys.Entities;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;

namespace DHY.MG.Module.Sys.Services
{
    /// <summary>
    /// DDBot Token使用记录服务
    /// 负责记录AI调用的token消耗和统计分析
    /// </summary>
    [ApiDescriptionSettings("DDBot")]
    public class DDBotTokenUsageService : IDynamicApiController, ITransient
    {
        private readonly SqlSugarRepository<DDBotTokenUsageDetail> _detailRep;
        private readonly SqlSugarRepository<DDBotTokenUsageStats> _statsRep;
        private readonly SqlSugarRepository<DDBotModelPrice> _modelPriceRep;
        private readonly DDBotOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DDBotTokenUsageService(
            SqlSugarRepository<DDBotTokenUsageDetail> detailRep,
            SqlSugarRepository<DDBotTokenUsageStats> statsRep,
            SqlSugarRepository<DDBotModelPrice> modelPriceRep,
            IOptions<DDBotOptions> options,
            IHttpContextAccessor httpContextAccessor)
        {
            _detailRep = detailRep;
            _statsRep = statsRep;
            _modelPriceRep = modelPriceRep;
            _options = options.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        #region 记录Token使用

        /// <summary>
        /// 记录单次AI调用的token使用
        /// </summary>
        [DisplayName("记录AI调用Token使用")]
        [HttpPost("recordTokenUsage")]
        public async Task RecordTokenUsageAsync(RecordTokenUsageInput input)
        {
            if (!_options.EnableTokenUsageRecording)
                return;

            var now = DateTime.Now;
            var callDate = now.Date;
            var callHour = now.Hour;

            // 获取当前用户信息
            var (userId, account, clientType) = GetCurrentUserInfo();

            // 1. 记录明细(如果启用)
            if (_options.RecordTokenUsageDetail)
            {
                var detail = new DDBotTokenUsageDetail
                {
                    CallDate = callDate,
                    CallHour = callHour,
                    ModelName = input.ModelName,
                    ApiType = input.ApiType,
                    PromptTokens = input.PromptTokens,
                    CompletionTokens = input.CompletionTokens,
                    TotalTokens = input.TotalTokens,
                    ConversationName = input.ConversationName,
                    UserId = userId,
                    UserAccount = account,
                    ClientType = clientType,
                    ProcessTimeMs = input.ProcessTimeMs,
                    IsSuccess = input.IsSuccess,
                    ErrorMessage = input.ErrorMessage
                };

                await _detailRep.AsInsertable(detail).ExecuteCommandAsync();
            }

            // 2. 更新聚合统计
            await UpdateStatsAsync(
                callDate, callHour, input.ModelName, input.ApiType,
                input.PromptTokens, input.CompletionTokens, input.TotalTokens,
                input.ProcessTimeMs, input.IsSuccess);
        }

        /// <summary>
        /// 更新聚合统计数据
        /// </summary>
        private async Task UpdateStatsAsync(
            DateTime callDate,
            int callHour,
            string modelName,
            string apiType,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            long processTimeMs,
            bool isSuccess)
        {
            // 获取模型单价
            var modelPrice = await _modelPriceRep.GetFirstAsync(p => p.ModelName == modelName && p.IsEnabled);
            decimal cost = 0;
            if (modelPrice != null)
            {
                cost = (promptTokens / 1000.0m) * modelPrice.InputPricePerThousand +
                       (completionTokens / 1000.0m) * modelPrice.OutputPricePerThousand;
            }

            // 更新小时统计
            await UpdateStatsByGranularityAsync(callDate, callHour, modelName, apiType,
                promptTokens, completionTokens, totalTokens, processTimeMs, isSuccess, cost);

            // 更新天统计(hour=0表示全天汇总)
            await UpdateStatsByGranularityAsync(callDate, 0, modelName, apiType,
                promptTokens, completionTokens, totalTokens, processTimeMs, isSuccess, cost);
        }

        /// <summary>
        /// 按粒度更新统计
        /// </summary>
        private async Task UpdateStatsByGranularityAsync(
            DateTime callDate,
            int hour,
            string modelName,
            string apiType,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            long processTimeMs,
            bool isSuccess,
            decimal cost)
        {
            var statsDate = callDate.Date;

            // 查询现有统计记录
            var existing = await _statsRep.GetFirstAsync(s =>
                s.StatsDate == statsDate &&
                s.StatsHour == hour &&
                s.ModelName == modelName &&
                s.ApiType == apiType);

            if (existing == null)
            {
                // 新增统计记录
                var newStats = new DDBotTokenUsageStats
                {
                    StatsDate = statsDate,
                    StatsHour = hour,
                    ModelName = modelName,
                    ApiType = apiType,
                    CallCount = 1,
                    SuccessCount = isSuccess ? 1 : 0,
                    FailedCount = isSuccess ? 0 : 1,
                    TotalPromptTokens = promptTokens,
                    TotalCompletionTokens = completionTokens,
                    TotalTokens = totalTokens,
                    AvgProcessTimeMs = processTimeMs,
                    EstimatedCost = cost
                };

                await _statsRep.AsInsertable(newStats).ExecuteCommandAsync();
            }
            else
            {
                // 更新现有统计记录
                existing.CallCount++;
                if (isSuccess)
                    existing.SuccessCount++;
                else
                    existing.FailedCount++;

                existing.TotalPromptTokens += promptTokens;
                existing.TotalCompletionTokens += completionTokens;
                existing.TotalTokens += totalTokens;
                existing.EstimatedCost += cost;

                // 重新计算平均耗时
                existing.AvgProcessTimeMs = (existing.AvgProcessTimeMs * (existing.CallCount - 1) + processTimeMs) / existing.CallCount;

                await _statsRep.AsUpdateable(existing)
                    .UpdateColumns(s => new {
                        s.CallCount, s.SuccessCount, s.FailedCount,
                        s.TotalPromptTokens, s.TotalCompletionTokens, s.TotalTokens,
                        s.AvgProcessTimeMs, s.EstimatedCost
                    })
                    .ExecuteCommandAsync();
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        private (string? UserId, string? Account, string? ClientType) GetCurrentUserInfo()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User == null)
                    return (null, null, null);

                // 从 Claims中获取用户信息
                var userId = httpContext.User.FindFirst("Id")?.Value;
                var account = httpContext.User.FindFirst("Account")?.Value;
                
                // 客户端类型从请求头中获取(由客户端发送)
                var clientType = httpContext.Request.Headers["X-Client-Type"].FirstOrDefault();
                
                return (userId, account, clientType);
            }
            catch
            {
                return (null, null, null);
            }
        }

        #endregion

        #region 统计查询

        /// <summary>
        /// 查询Token统计数据
        /// </summary>
        [DisplayName("查询Token统计数据")]
        [HttpPost("stats")]
        public async Task<DDBotTokenStatsOutput> QueryStatsAsync(DDBotTokenStatsQueryInput input)
        {
            var output = new DDBotTokenStatsOutput();

            // 构建查询条件
            var query = _statsRep.AsQueryable()
                .Where(s => s.StatsDate >= input.StartDate.Date && s.StatsDate <= input.EndDate.Date);

            // 按粒度筛选(hour=0表示天统计,>0表示小时统计)
            if (input.Granularity == "hour")
            {
                query = query.Where(s => s.StatsHour > 0);
            }
            else
            {
                query = query.Where(s => s.StatsHour == 0);
            }

            // 模型筛选
            if (!string.IsNullOrWhiteSpace(input.ModelName))
            {
                query = query.Where(s => s.ModelName == input.ModelName);
            }

            // API类型筛选
            if (!string.IsNullOrWhiteSpace(input.ApiType))
            {
                query = query.Where(s => s.ApiType == input.ApiType);
            }

            // 获取统计数据
            var statsList = await query
                .OrderBy(s => s.StatsDate)
                .OrderBy(s => s.StatsHour)
                .ToListAsync();

            // 转换为输出格式
            foreach (var stats in statsList)
            {
                var dateTime = stats.StatsDate.AddHours(stats.StatsHour);
                output.Data.Add(new DDBotTokenStatsItem
                {
                    DateTime = dateTime,
                    ModelName = stats.ModelName,
                    ApiType = stats.ApiType,
                    CallCount = stats.CallCount,
                    TotalTokens = stats.TotalTokens,
                    PromptTokens = stats.TotalPromptTokens,
                    CompletionTokens = stats.TotalCompletionTokens,
                    Cost = stats.EstimatedCost,
                    AvgTimeMs = stats.AvgProcessTimeMs
                });
            }

            // 计算汇总信息
            var summary = await _statsRep.AsQueryable()
                .Where(s => s.StatsDate >= input.StartDate.Date && s.StatsDate <= input.EndDate.Date)
                .Where(s => s.StatsHour == 0) // 只汇总天级别数据
                .Select(s => new
                {
                    TotalCalls = SqlFunc.AggregateSum((int?)s.CallCount),
                    TotalTokens = SqlFunc.AggregateSum((long?)s.TotalTokens),
                    TotalPromptTokens = SqlFunc.AggregateSum((long?)s.TotalPromptTokens),
                    TotalCompletionTokens = SqlFunc.AggregateSum((long?)s.TotalCompletionTokens),
                    TotalCost = SqlFunc.AggregateSum((decimal?)s.EstimatedCost),
                    SuccessCount = SqlFunc.AggregateSum((int?)s.SuccessCount),
                    FailedCount = SqlFunc.AggregateSum((int?)s.FailedCount)
                })
                .FirstAsync();

            if (summary != null)
            {
                output.Summary = new DDBotTokenSummary
                {
                    TotalCalls = summary.TotalCalls ?? 0,
                    TotalTokens = summary.TotalTokens ?? 0,
                    TotalPromptTokens = summary.TotalPromptTokens ?? 0,
                    TotalCompletionTokens = summary.TotalCompletionTokens ?? 0,
                    TotalCost = summary.TotalCost ?? 0,
                    SuccessCount = summary.SuccessCount ?? 0,
                    FailedCount = summary.FailedCount ?? 0,
                    SuccessRate = (summary.TotalCalls ?? 0) > 0 ? (double)(summary.SuccessCount ?? 0) / (summary.TotalCalls ?? 0) * 100 : 0
                };
            }

            return output;
        }

        /// <summary>
        /// 查询Token使用明细分页
        /// </summary>
        [DisplayName("查询Token使用明细")]
        [HttpPost("details")]
        public async Task<SqlSugarPagedList<DDBotTokenDetailOutput>> QueryDetailsAsync(DDBotTokenDetailQueryInput input)
        {
            var query = _detailRep.AsQueryable()
                .OrderByDescending(d => d.CreateTime);

            if (input.StartDate.HasValue)
                query = query.Where(d => d.CallDate >= input.StartDate.Value.Date);

            if (input.EndDate.HasValue)
                query = query.Where(d => d.CallDate <= input.EndDate.Value.Date.AddDays(1).AddSeconds(-1));

            if (!string.IsNullOrWhiteSpace(input.ModelName))
                query = query.Where(d => d.ModelName == input.ModelName);

            if (!string.IsNullOrWhiteSpace(input.ApiType))
                query = query.Where(d => d.ApiType == input.ApiType);

            if (input.IsSuccess.HasValue)
                query = query.Where(d => d.IsSuccess == input.IsSuccess.Value);

            return await query
                .Select(d => new DDBotTokenDetailOutput
                {
                    CallTime = d.CallDate.AddHours(d.CallHour),
                    ModelName = d.ModelName,
                    ApiType = d.ApiType,
                    PromptTokens = d.PromptTokens,
                    CompletionTokens = d.CompletionTokens,
                    TotalTokens = d.TotalTokens,
                    ConversationName = d.ConversationName,
                    ProcessTimeMs = d.ProcessTimeMs,
                    IsSuccess = d.IsSuccess,
                    ErrorMessage = d.ErrorMessage
                })
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        #endregion

        #region 模型单价管理

        /// <summary>
        /// 获取模型单价列表
        /// </summary>
        [DisplayName("获取模型单价列表")]
        [HttpPost("modelPrices")]
        public async Task<List<DDBotModelPriceOutput>> GetModelPricesAsync()
        {
            return await _modelPriceRep.AsQueryable()
                .OrderBy(p => p.ModelName)
                .Select(p => new DDBotModelPriceOutput
                {
                    Id = p.Id,
                    ModelName = p.ModelName,
                    DisplayName = p.DisplayName,
                    InputPricePerThousand = p.InputPricePerThousand,
                    OutputPricePerThousand = p.OutputPricePerThousand,
                    IsEnabled = p.IsEnabled,
                    Remark = p.Remark,
                    CreatedTime = p.CreateTime,
                    UpdatedTime = p.UpdateTime
                })
                .ToListAsync();
        }

        /// <summary>
        /// 新增或更新模型单价
        /// </summary>
        [DisplayName("保存模型单价配置")]
        [HttpPost("saveModelPrice")]
        public async Task SaveModelPriceAsync(DDBotModelPriceInput input)
        {
            if (input.Id.HasValue && input.Id.Value > 0)
            {
                // 更新
                var existing = await _modelPriceRep.GetFirstAsync(p => p.Id == input.Id.Value);
                if (existing == null)
                    throw Oops.Oh("模型单价配置不存在");

                existing.ModelName = input.ModelName;
                existing.DisplayName = input.DisplayName;
                existing.InputPricePerThousand = input.InputPricePerThousand;
                existing.OutputPricePerThousand = input.OutputPricePerThousand;
                existing.IsEnabled = input.IsEnabled;
                existing.Remark = input.Remark;

                await _modelPriceRep.AsUpdateable(existing).ExecuteCommandAsync();
            }
            else
            {
                // 新增
                var newPrice = new DDBotModelPrice
                {
                    ModelName = input.ModelName,
                    DisplayName = input.DisplayName,
                    InputPricePerThousand = input.InputPricePerThousand,
                    OutputPricePerThousand = input.OutputPricePerThousand,
                    IsEnabled = input.IsEnabled,
                    Remark = input.Remark
                };

                await _modelPriceRep.AsInsertable(newPrice).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 删除模型单价配置
        /// </summary>
        [DisplayName("删除模型单价配置")]
        [HttpDelete("modelPrice/{id}")]
        public async Task DeleteModelPriceAsync(long id)
        {
            await _modelPriceRep.DeleteByIdAsync(id);
        }

        #endregion
    }
}
