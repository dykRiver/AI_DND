using DHY.Game.Admin.Dtos;
using DHY.Game.Core.Entities;

namespace DHY.Game.Admin.Services;

/// <summary>
/// 副本模板管理服务
/// </summary>
[ApiDescriptionSettings("GameAdmin")]
public class DungeonTemplateService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameDungeonTemplate> _templateRep;

    public DungeonTemplateService(SqlSugarRepository<GameDungeonTemplate> templateRep)
    {
        _templateRep = templateRep;
    }

    /// <summary>
    /// 分页查询副本模板
    /// </summary>
    [DisplayName("分页查询副本模板")]
    [HttpGet("templateList")]
    public async Task<SqlSugarPagedList<GameDungeonTemplate>> GetTemplateListAsync([FromQuery] TemplateListQueryInput input)
    {
        var query = _templateRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword), t =>
                t.Name.Contains(input.Keyword!) ||
                t.WorldTheme.Contains(input.Keyword!) ||
                t.Description!.Contains(input.Keyword!))
            .Where(t => !t.IsDelete)
            .OrderByDescending(t => t.CreateTime);

        return await query.ToPagedListAsync(input.PageIndex, input.PageSize);
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    [DisplayName("获取模板详情")]
    [HttpGet("templateDetail")]
    public async Task<GameDungeonTemplate> GetTemplateDetailAsync([FromQuery] TemplateDetailQueryInput input)
    {
        var template = await _templateRep.GetFirstAsync(t => t.Id == input.Id && !t.IsDelete);
        if (template == null)
            throw Oops.Oh("模板不存在");
        return template;
    }

    /// <summary>
    /// 创建副本模板
    /// </summary>
    [DisplayName("创建副本模板")]
    [ApiDescriptionSettings(Name = "CreateTemplate"), HttpPost]
    public async Task<long> CreateTemplateAsync(CreateTemplateInput input)
    {
        var entity = new GameDungeonTemplate
        {
            Name = input.Name,
            WorldTheme = input.WorldTheme,
            Difficulty = input.Difficulty,
            TimeLimitDays = input.TimeLimitDays,
            Tags = input.Tags,
            Description = input.Description,
            BasePrompt = input.BasePrompt,
            MaxLevel = input.MaxLevel
        };

        var result = await _templateRep.AsInsertable(entity).ExecuteReturnEntityAsync();
        return result.Id;
    }

    /// <summary>
    /// 更新副本模板
    /// </summary>
    [DisplayName("更新副本模板")]
    [HttpPost("updateTemplate")]
    public async Task UpdateTemplateAsync(UpdateTemplateInput input)
    {
        var entity = await _templateRep.GetFirstAsync(t => t.Id == input.Id && !t.IsDelete);
        if (entity == null)
            throw Oops.Oh("模板不存在");

        entity.Name = input.Name;
        entity.WorldTheme = input.WorldTheme;
        entity.Difficulty = input.Difficulty;
        entity.TimeLimitDays = input.TimeLimitDays;
        entity.Tags = input.Tags;
        entity.Description = input.Description;
        entity.BasePrompt = input.BasePrompt;
        entity.MaxLevel = input.MaxLevel;

        await _templateRep.AsUpdateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 软删除模板
    /// </summary>
    [DisplayName("删除副本模板")]
    [HttpPost("deleteTemplate")]
    public async Task DeleteTemplateAsync(DeleteTemplateInput input)
    {
        var entity = await _templateRep.GetFirstAsync(t => t.Id == input.Id && !t.IsDelete);
        if (entity == null)
            throw Oops.Oh("模板不存在");

        entity.IsDelete = true;
        await _templateRep.AsUpdateable(entity)
            .UpdateColumns(t => new { t.IsDelete })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 按难度统计模板数量
    /// </summary>
    [DisplayName("按难度统计模板数量")]
    [ApiDescriptionSettings(Name = "GetDifficultyStats"), HttpGet]
    public async Task<List<DifficultyStatsOutput>> GetDifficultyStatsAsync()
    {
        return await _templateRep.AsQueryable()
            .Where(t => !t.IsDelete)
            .GroupBy(t => t.Difficulty)
            .Select(t => new DifficultyStatsOutput
            {
                Difficulty = t.Difficulty,
                Count = SqlFunc.AggregateCount(t.Id)
            })
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有世界观主题列表(去重)
    /// </summary>
    [DisplayName("获取世界观主题列表")]
    [ApiDescriptionSettings(Name = "GetWorldThemeList"), HttpGet]
    public async Task<List<string>> GetWorldThemeListAsync()
    {
        return await _templateRep.AsQueryable()
            .Where(t => !t.IsDelete)
            .GroupBy(t => t.WorldTheme)
            .Select(t => t.WorldTheme)
            .ToListAsync();
    }
}
