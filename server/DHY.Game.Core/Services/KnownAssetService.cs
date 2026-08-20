namespace DHY.Game.Core.Services;

/// <summary>
/// 已知情报/无形资产管理服务
/// 道具AI（物资官）作为唯一写入权威，管理纸条内容、电话号码、暗号、记忆等信息载体。
/// 供分类AI做可行性判定（读有效清单）与前端"已知线索"展示。
/// </summary>
[ApiDescriptionSettings("Game")]
public class KnownAssetService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameKnownAsset> _assetRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;

    public KnownAssetService(
        SqlSugarRepository<GameKnownAsset> assetRep,
        SqlSugarRepository<GameCharacter> characterRep)
    {
        _assetRep = assetRep;
        _characterRep = characterRep;
    }

    /// <summary>
    /// 新增已知情报（同会话同名有效条目已存在时跳过，避免重复记账）
    /// </summary>
    [DisplayName("新增已知情报")]
    [HttpPost("addKnownAsset")]
    public async Task<GameKnownAsset?> AddAsync([FromBody] AddKnownAssetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            return null;

        var existing = await _assetRep.GetFirstAsync(a =>
            a.SessionId == input.SessionId && a.Name == input.Name && a.IsValid);
        if (existing != null)
            return existing;

        var characterId = input.CharacterId;
        if (characterId == 0)
        {
            var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
            characterId = character?.Id ?? 0;
        }

        var asset = new GameKnownAsset
        {
            SessionId = input.SessionId,
            CharacterId = characterId,
            AssetType = string.IsNullOrWhiteSpace(input.AssetType) ? "情报" : input.AssetType,
            Name = input.Name,
            Content = input.Content,
            Source = input.Source,
            AcquiredRound = input.AcquiredRound,
            IsValid = true,
            Timestamp = DateTime.Now
        };
        await _assetRep.AsInsertable(asset).ExecuteCommandAsync();
        return asset;
    }

    /// <summary>
    /// 使某条已知情报失效（按名称匹配，如纸条烧毁、号码遗忘）
    /// </summary>
    [DisplayName("失效已知情报")]
    [HttpPost("invalidateKnownAsset")]
    public async Task<bool> InvalidateAsync([FromBody] InvalidateKnownAssetInput input)
    {
        var asset = await _assetRep.GetFirstAsync(a =>
            a.SessionId == input.SessionId && a.Name == input.Name && a.IsValid);
        if (asset == null)
            return false;

        asset.IsValid = false;
        asset.UpdateTime = DateTime.Now;
        await _assetRep.AsUpdateable(asset)
            .UpdateColumns(a => new { a.IsValid, a.UpdateTime })
            .ExecuteCommandAsync();
        return true;
    }

    /// <summary>
    /// 获取会话的有效已知情报列表（供分类AI可行性判定与前端展示）
    /// </summary>
    [DisplayName("获取已知情报列表")]
    [HttpGet("listKnownAssets")]
    public async Task<List<GameKnownAsset>> ListValidAsync([FromQuery] SessionIdInput input)
    {
        return await _assetRep.AsQueryable()
            .Where(a => a.SessionId == input.SessionId && a.IsValid)
            .OrderBy(a => a.AcquiredRound)
            .ToListAsync();
    }
}

// ========== DTO ==========

/// <summary>
/// 新增已知情报输入
/// </summary>
public class AddKnownAssetInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>角色ID（0时按会话查找）</summary>
    public long CharacterId { get; set; }
    /// <summary>资产类型 (情报/线索/联系方式/记忆/暗号)</summary>
    public string AssetType { get; set; } = "情报";
    /// <summary>名称</summary>
    public string Name { get; set; } = "";
    /// <summary>内容</summary>
    public string? Content { get; set; }
    /// <summary>获得来源</summary>
    public string? Source { get; set; }
    /// <summary>获得轮次</summary>
    public int AcquiredRound { get; set; }
}

/// <summary>
/// 失效已知情报输入
/// </summary>
public class InvalidateKnownAssetInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>名称（精确匹配有效条目）</summary>
    public string Name { get; set; } = "";
}
