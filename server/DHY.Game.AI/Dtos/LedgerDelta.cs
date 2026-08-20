using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 资产账本增量（道具AI/物资官依导演蓝图记账后输出的权威变更集）。
/// 物理道具走 InventoryService，无形情报走 KnownAssetService。
/// 未发生变更的分组输出空数组或省略。
/// </summary>
public class LedgerDelta
{
    /// <summary>本轮真实获得的物理道具（含完整数值，供直接入库）</summary>
    [JsonProperty("acquired_items")]
    public List<AcquiredItemInfo>? AcquiredItems { get; set; }

    /// <summary>本轮被使用/消耗的物理道具（按名称扣减）</summary>
    [JsonProperty("consumed_items")]
    public List<ConsumedItemInfo>? ConsumedItems { get; set; }

    /// <summary>本轮遗失/被夺/损毁的物理道具（按名称移除，区别于主动使用消耗）</summary>
    [JsonProperty("lost_items")]
    public List<ConsumedItemInfo>? LostItems { get; set; }

    /// <summary>本轮获得的无形情报（纸条内容、电话号码、暗号、记忆等信息载体）</summary>
    [JsonProperty("acquired_info")]
    public List<AcquiredInfoInfo>? AcquiredInfo { get; set; }

    /// <summary>本轮失效的无形情报（如纸条烧毁、号码遗忘）</summary>
    [JsonProperty("invalidated_info")]
    public List<InvalidatedInfoInfo>? InvalidatedInfo { get; set; }

    /// <summary>是否存在任何有效变更</summary>
    [JsonIgnore]
    public bool HasAnyChange =>
        (AcquiredItems is { Count: > 0 }) ||
        (ConsumedItems is { Count: > 0 }) ||
        (LostItems is { Count: > 0 }) ||
        (AcquiredInfo is { Count: > 0 }) ||
        (InvalidatedInfo is { Count: > 0 });
}

/// <summary>
/// 道具AI记录的无形情报（获得）
/// </summary>
public class AcquiredInfoInfo
{
    /// <summary>资产类型 (情报/线索/联系方式/记忆/暗号)</summary>
    [JsonProperty("asset_type")]
    public string AssetType { get; set; } = "情报";

    /// <summary>情报名称（简短标识，如"神秘纸条""老陈的电话"）</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>情报内容（纸条上的字、具体号码、暗号口令等）</summary>
    [JsonProperty("content")]
    public string? Content { get; set; }

    /// <summary>获得来源（谁给的/在哪拿到）</summary>
    [JsonProperty("source")]
    public string? Source { get; set; }
}

/// <summary>
/// 道具AI记录的无形情报（失效）
/// </summary>
public class InvalidatedInfoInfo
{
    /// <summary>情报名称（精确匹配已有条目）</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>失效原因（调试日志用）</summary>
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}
