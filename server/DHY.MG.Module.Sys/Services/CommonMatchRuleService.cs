
using DHY.MG.Module.Sys.Dtos;
using DHY.MG.Module.Sys.Entities;
using DHY.MG.Module.Sys.Enum;
using Newtonsoft.Json;

/// <summary>
/// 通用药嘱条目规则
/// </summary>
public class CommonMatchRuleService(SimpleRepository<CommonMatchRule> commonMatchRuleRepository
    , SimpleRepository<MedicationGuidance> medicationGuidanceRepository,
    IPrescriptionApiService prescriptionApiService) : IDynamicApiController, ITransient
{
    /// <summary>
    /// 增加通用药嘱条目规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("增加通用药嘱条目规则")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task AddAsync(CommonMatchRuleInput input)
    {
        await commonMatchRuleRepository.AsInsertable(input.Adapt<CommonMatchRule>()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新通用药嘱条目规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("更新通用药嘱条目规则")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task UpdateAsync(UpdateCommonMatchRuleInput input)
    {
        await commonMatchRuleRepository.AsUpdateable(input.Adapt<CommonMatchRule>()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 查询通用药嘱条目规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("查询通用药嘱条目规则")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<SqlSugarPagedList<CommonMatchRule>> QueryPageAsync(CommonMatchRuleQuery query)
    {
        return await commonMatchRuleRepository.AsQueryable()
            .WhereIF(query.GuidanceType.Any(), n => query.GuidanceType.Contains(n.GuidanceType))
            .OrderByDescending(n => n.UpdateTime == null ? n.CreateTime : n.UpdateTime)
            .ToPagedListAsync(query.Page, query.PageSize);
    }

    /// <summary>
    /// 删除通用药嘱条目规则
    /// </summary>
    /// <param name="input"></param>
    /// <returns>void</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除通用药嘱条目规则")]
    public async Task<bool> DeleteAsync(BaseIdInput input)
    {
        return await commonMatchRuleRepository.DeleteByIdAsync(input.Id);
    }


    /// <summary>
    /// 查看通用药嘱条目规则
    /// </summary>
    /// <returns></returns>
    [DisplayName("查看通用药嘱条目规则")]
    [ApiDescriptionSettings(Name = "Detail"), HttpPost]
    public async Task<CommonMatchRule> DetailAsync(BaseIdInput input)
    {
        return await commonMatchRuleRepository.AsQueryable().FirstAsync(u => u.Id == input.Id);
    }


    /// <summary>
    /// 查询匹配成功的通用药嘱条目规则
    /// </summary>
    /// <returns></returns>
    [DisplayName("查询匹配成功的通用药嘱条目规则")]
    [ApiDescriptionSettings(Name = "MatchSelect"), HttpPost]
    public async Task<List<MatchSelectDto>> QueryMatchSelectAsync(CommonMatchRuleQuery query)
    {
        var drugNames = new List<string>();
        if (query.GuidanceType.Any(n => n == GuidanceType.Health4 || n == GuidanceType.Contraindication))
        {
            var drugs = (await prescriptionApiService.QueryDetailsAsync(new BaseIdInput { Id = query.PrescriptionId }))?.Result;
            drugNames = drugs.Select(n => n.Name).ToList();
        }
        var commonMatchRules = await commonMatchRuleRepository.AsQueryable()
           .WhereIF(query.GuidanceType.Any(), n => query.GuidanceType.Contains(n.GuidanceType))
           .WhereIF(query.GuidanceType.Any(n => n == GuidanceType.Health4), n => drugNames.Contains(n.DrugName))
           .OrderByDescending(n => n.Level)
           .ToListAsync();
       
        var medicationGuidance = await medicationGuidanceRepository.AsQueryable()
           .WhereIF(query.GuidanceType.Any(), n => query.GuidanceType.Contains(n.GuidanceType))
           .WhereIF(!query.PrescriptionNo.IsNullOrEmpty(), n => n.PrescriptionNo == query.PrescriptionNo)
           .FirstAsync();

        var prescription = JsonConvert.DeserializeObject<CheckPresDto>(query.PrescriptionJson).Prescription;
        commonMatchRules = commonMatchRules.Where(n => n.DrugName.IsNullOrEmpty() ||
        prescription.Details.Any(t => t.DrugName == n.DrugName) || drugNames.Contains(n.DrugName)).ToList();

        var matchSelects = new List<MatchSelectDto>();
        foreach (var commonMatchRule in commonMatchRules)
        {
            if (commonMatchRule.KeyWord == null) { commonMatchRule.KeyWord = string.Empty; }
            // 规则检查
            var keyWords = commonMatchRule.KeyWord.Split("/").ToList();
          
            if ((!query.GuidanceType.Any(n => n == GuidanceType.Health4 || n == GuidanceType.Contraindication) && !keyWords.Any(n => query.PrescriptionJson.Contains(n)))
                || matchSelects.Any(n => n.Key == commonMatchRule.GuideContent)) { continue; }
            if (query.GuidanceType.Any(n => n == GuidanceType.Contraindication) && !keyWords.Any(n => drugNames.Contains(n))) { continue; }

            if (commonMatchRule.AgeMin != null && commonMatchRule.AgeMin > prescription.Age) { continue; }
            if (commonMatchRule.AgeMax != null && commonMatchRule.AgeMax < prescription.Age) { continue; }
            if (commonMatchRule.Sex != null && commonMatchRule.Sex != SexType.None && commonMatchRule.Sex != prescription.Sex) { continue; }

            // 占位符
            if (commonMatchRule.GuideContent.Contains("{}"))
            {
                if (query.GuidanceType.Any(n => n == GuidanceType.Contraindication))
                {
                    keyWords = keyWords.Where(n => drugNames.Contains(n)).ToList();
                }
                else
                {
                    keyWords = keyWords.Where(n => query.PrescriptionJson.Contains(n)).ToList();
                }

            }
            

            // 去重
            var guideContents = commonMatchRule.GuideContent.Split("/").ToList();
            guideContents.ForEach(n =>
            {
                var item = matchSelects.FirstOrDefault(t => t.Val == n);
                // 占位符
                var guideContent = n;
                if (guideContent.Contains("{}"))
                {
                    guideContent = guideContent.Replace("{}", string.Join("、", keyWords));
                }
                var isSelect = medicationGuidance != null && medicationGuidance.Content != null ? medicationGuidance.Content.Contains(guideContent) : commonMatchRule.IsDefault;
                if (item != null)
                {
                    if (!item.IsSelect && isSelect) { item.IsSelect = true; }
                    return;
                }

               
                if (!guideContent.EndsWith("。"))
                {
                    guideContent += "。";
                }
                if (matchSelects.Any(n => n.Key == guideContent)) { return; }
                //读取到已保存的数据，则以已保存的药嘱信息为主做二次修改
                matchSelects.Add(new MatchSelectDto(guideContent, guideContent, isSelect, commonMatchRule.KeyWord.IsNullOrEmpty()));
            });

        }



        var empty = "无。";
        if (medicationGuidance != null && medicationGuidance.Content != null)
        {
            var selectMgRules = matchSelects.Select(n => n.Key).ToList();
            var saveMGContents = medicationGuidance.Content.Split("/").ToList();
            var difference = saveMGContents.Except(selectMgRules);
            foreach (var item in difference)
            {
                if (item.IsNullOrEmpty())
                {
                    matchSelects.Add(new MatchSelectDto(empty, empty, true));
                }
                else
                {
                    matchSelects.Add(new MatchSelectDto(item, item, true));
                }
            }
        }

        if (!matchSelects.Any())
        {
            matchSelects.Add(new MatchSelectDto(empty, empty, true));
        }
        else if(matchSelects.Any(n=> n.IsDefault) && matchSelects.Any(n => !n.IsDefault))
        {
            matchSelects = matchSelects.Where(n=> !n.IsDefault).ToList();
        }


        return matchSelects;
    }


    /// <summary>
    /// 获取药嘱条目枚举信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取药嘱条目枚举信息")]
    [ApiDescriptionSettings(Name = "GetGuidanceType"), HttpGet]
    public async Task<List<EnumEntity>> GetGuidanceTypeAsync()
    {
        var enumData = EnumExtension.EnumToList(typeof(GuidanceType));
        return enumData;
    }

}