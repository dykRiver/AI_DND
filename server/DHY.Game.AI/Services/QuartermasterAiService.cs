using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using DHY.Game.Core.Services;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 物资官（Quartermaster）道具AI服务。
/// 导演推演完成后（叙事之前）依导演蓝图记账：读本轮行动+导演蓝图 item_hints+当前账本，
/// 把蓝图逐条扩展为完整数值的 <see cref="LedgerDelta"/> 并落库（物理道具走 InventoryService，无形情报走 KnownAssetService）。
/// 作为玩家资产账本的唯一写入权威。导演是资产变更的唯一来源：无 item_hints 即无需记账。
/// </summary>
public class QuartermasterAiService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly InventoryService _inventoryService;
    private readonly KnownAssetService _knownAssetService;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly ILogger<QuartermasterAiService> _logger;

    public QuartermasterAiService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        InventoryService inventoryService,
        KnownAssetService knownAssetService,
        SqlSugarRepository<GameCharacter> characterRep,
        ILogger<QuartermasterAiService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _inventoryService = inventoryService;
        _knownAssetService = knownAssetService;
        _characterRep = characterRep;
        _logger = logger;
    }

    /// <summary>
    /// 依导演蓝图记账并落库。返回实际登记的增量（用于Hub推送/日志）；无变更或调用失败时返回null。
    /// 门控由调用方直接判 item_hints 非空（导演是唯一变更来源，纯对话/观察轮导演无 hint 即跳过）。
    /// LLM 调用/解析失败时，若传入结构化蓝图 blueprint，则按规则保底落库，避免导演已宣告的资产变更静默丢失。
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="playerAction">本轮玩家行动（原始输入或提炼意图）</param>
    /// <param name="itemHints">导演蓝图中的物资清单（item_hints，事实基准）</param>
    /// <param name="currentLedger">当前账本文本（背包 + 有效情报）</param>
    /// <param name="currentRound">当前轮次（登记 AcquiredRound 用）</param>
    /// <param name="blueprint">结构化蓝图（可选，LLM记账失败时用于规则化保底落库）</param>
    public async Task<LedgerDelta?> RecordFromBlueprintAsync(
        long sessionId,
        string playerAction,
        string itemHints,
        string currentLedger,
        int currentRound,
        List<ItemHintInfo>? blueprint = null)
    {
        if (string.IsNullOrWhiteSpace(itemHints))
            return null;

        LedgerDelta? delta;
        try
        {
            if (_modelFactory.IsDebugEnabled)
                AiDebugLogger.LogCallChain("Quartermaster", $"开始记账: {playerAction}");

            var systemPrompt = _promptService.LoadTemplate("quartermaster_system");
            var ledgerText = string.IsNullOrWhiteSpace(currentLedger) ? "（当前账本为空）" : currentLedger;
            var userContent =
                $"【本轮玩家行动】\n{playerAction}\n\n" +
                $"【导演蓝图 item_hints（权威事实基准，逐条落实）】\n{itemHints}\n\n" +
                $"【当前账本（已有资产，勿重复登记）】\n{ledgerText}";

            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userContent }
            };

            var config = _modelFactory.GetModelConfig("Quartermaster");
            var client = _modelFactory.CreateClient(config);
            var result = await client.ChatCompletionAsync(messages, config, aiRole: "Quartermaster");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("物资官AI调用失败: {Error}, SessionId={SessionId}, 行动={Action}, 蓝图摘要={Hints}",
                    result.ErrorMessage, sessionId, playerAction, SummarizeHints(itemHints));
                delta = null;
            }
            else
            {
                delta = ParseLedgerDelta(result.Content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "物资官记账异常: SessionId={SessionId}, 行动={Action}, 蓝图摘要={Hints}",
                sessionId, playerAction, SummarizeHints(itemHints));
            delta = null;
        }

        // LLM记账失败 → 按导演蓝图规则化保底（蓝图字段足够构造基础记录，仅缺精细数值）
        if (delta == null && blueprint is { Count: > 0 })
        {
            delta = BuildFallbackDelta(blueprint);
            _logger.LogWarning("物资官记账降级：按蓝图规则化保底落库, SessionId={SessionId}, 条目数={Count}",
                sessionId, blueprint.Count);
        }

        if (delta == null || !delta.HasAnyChange)
        {
            if (_modelFactory.IsDebugEnabled)
                AiDebugLogger.LogCallChain("Quartermaster", "记账结果: 无资产变更");
            return null;
        }

        await ApplyDeltaAsync(sessionId, delta, currentRound);
        return delta;
    }

    /// <summary>
    /// 蓝图摘要（日志用，压缩为单行）
    /// </summary>
    private static string SummarizeHints(string itemHints) =>
        itemHints.Replace("\r", "").Replace("\n", " | ");

    /// <summary>
    /// 规则化保底：LLM记账失败时直接按导演蓝图构造基础增量（不补全精细数值，保证资产不丢）。
    /// 类别含"物品/道具"视为物理道具，其余（情报/线索等）视为无形资产。
    /// </summary>
    private static LedgerDelta BuildFallbackDelta(List<ItemHintInfo> blueprint)
    {
        var delta = new LedgerDelta
        {
            AcquiredItems = new List<AcquiredItemInfo>(),
            ConsumedItems = new List<ConsumedItemInfo>(),
            LostItems = new List<ConsumedItemInfo>(),
            AcquiredInfo = new List<AcquiredInfoInfo>(),
            InvalidatedInfo = new List<InvalidatedInfoInfo>()
        };

        foreach (var h in blueprint)
        {
            if (string.IsNullOrWhiteSpace(h.Name)) continue;
            var isPhysical = h.Category.Contains("物品") || h.Category.Contains("道具");
            var isAcquire = h.Change.Contains("获得");
            var isConsume = h.Change.Contains("消耗");

            if (isPhysical)
            {
                if (isAcquire)
                {
                    delta.AcquiredItems.Add(new AcquiredItemInfo
                    {
                        ItemName = h.Name,
                        ItemType = h.IsKey ? "关键道具" : "杂物",
                        Description = h.Note,
                        Weight = 0.5m
                    });
                }
                else if (isConsume)
                {
                    delta.ConsumedItems.Add(new ConsumedItemInfo { ItemName = h.Name, Quantity = 1, Reason = h.Note });
                }
                else // 失去/被夺/损毁
                {
                    delta.LostItems.Add(new ConsumedItemInfo { ItemName = h.Name, Quantity = 1, Reason = h.Note });
                }
            }
            else
            {
                if (isAcquire)
                {
                    delta.AcquiredInfo.Add(new AcquiredInfoInfo
                    {
                        AssetType = h.Category,
                        Name = h.Name,
                        Content = h.Note,
                        Source = "导演蓝图（规则化保底）"
                    });
                }
                else // 消耗/失去均视为情报失效
                {
                    delta.InvalidatedInfo.Add(new InvalidatedInfoInfo { Name = h.Name, Reason = h.Note });
                }
            }
        }

        return delta;
    }

    /// <summary>
    /// 将记账增量权威落库（物理道具 InventoryService，无形情报 KnownAssetService）。
    /// </summary>
    private async Task ApplyDeltaAsync(long sessionId, LedgerDelta delta, int currentRound)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
        {
            _logger.LogWarning("物资官记账落库跳过：会话{SessionId}无角色", sessionId);
            return;
        }

        // 1. 获得物理道具
        if (delta.AcquiredItems is { Count: > 0 })
        {
            foreach (var ai in delta.AcquiredItems)
            {
                if (string.IsNullOrWhiteSpace(ai.ItemName)) continue;
                try
                {
                    await _inventoryService.AddItemAsync(new AddItemInput
                    {
                        CharacterId = character.Id,
                        ItemName = ai.ItemName,
                        ItemType = ai.ItemType,
                        Description = ai.Description,
                        Quantity = 1,
                        IsKeyItem = ai.ItemType == "关键道具",
                        Weight = Math.Round(ai.Weight, 1, MidpointRounding.AwayFromZero),
                        AttributeBonus = ai.AttributeBonus,
                        LinkedAttribute = ai.LinkedAttribute,
                        MaxUses = ai.MaxUses,
                        IsUnlimited = ai.IsUnlimited
                    });
                    if (_modelFactory.IsDebugEnabled)
                        AiDebugLogger.LogOrchestration("物资官·获得", $"{ai.ItemName} ({ai.ItemType}) 重量={ai.Weight}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("物资官入库失败: {Error}, 道具={ItemName}", ex.Message, ai.ItemName);
                }
            }
        }

        // 2. 消耗物理道具
        if (delta.ConsumedItems is { Count: > 0 })
        {
            foreach (var c in delta.ConsumedItems)
            {
                if (string.IsNullOrWhiteSpace(c.ItemName)) continue;
                try
                {
                    var ok = await _inventoryService.ConsumeItemByNameAsync(character.Id, c.ItemName, c.Quantity);
                    if (_modelFactory.IsDebugEnabled)
                        AiDebugLogger.LogOrchestration("物资官·消耗", $"{c.ItemName} x{c.Quantity} {(ok ? "" : "(背包未找到,跳过)")}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("物资官扣减失败: {Error}, 道具={ItemName}", ex.Message, c.ItemName);
                }
            }
        }

        // 3. 遗失物理道具（按名称移除，与主动消耗同走扣减）
        if (delta.LostItems is { Count: > 0 })
        {
            foreach (var l in delta.LostItems)
            {
                if (string.IsNullOrWhiteSpace(l.ItemName)) continue;
                try
                {
                    var ok = await _inventoryService.ConsumeItemByNameAsync(character.Id, l.ItemName, l.Quantity);
                    if (_modelFactory.IsDebugEnabled)
                        AiDebugLogger.LogOrchestration("物资官·遗失", $"{l.ItemName} x{l.Quantity} - {l.Reason} {(ok ? "" : "(背包未找到,跳过)")}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("物资官遗失处理失败: {Error}, 道具={ItemName}", ex.Message, l.ItemName);
                }
            }
        }

        // 4. 获得无形情报
        if (delta.AcquiredInfo is { Count: > 0 })
        {
            foreach (var info in delta.AcquiredInfo)
            {
                if (string.IsNullOrWhiteSpace(info.Name)) continue;
                try
                {
                    await _knownAssetService.AddAsync(new AddKnownAssetInput
                    {
                        SessionId = sessionId,
                        CharacterId = character.Id,
                        AssetType = info.AssetType,
                        Name = info.Name,
                        Content = info.Content,
                        Source = info.Source,
                        AcquiredRound = currentRound
                    });
                    if (_modelFactory.IsDebugEnabled)
                        AiDebugLogger.LogOrchestration("物资官·情报", $"{info.AssetType}:{info.Name}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("物资官情报登记失败: {Error}, 情报={Name}", ex.Message, info.Name);
                }
            }
        }

        // 5. 失效无形情报
        if (delta.InvalidatedInfo is { Count: > 0 })
        {
            foreach (var iv in delta.InvalidatedInfo)
            {
                if (string.IsNullOrWhiteSpace(iv.Name)) continue;
                try
                {
                    var ok = await _knownAssetService.InvalidateAsync(new InvalidateKnownAssetInput
                    {
                        SessionId = sessionId,
                        Name = iv.Name
                    });
                    if (_modelFactory.IsDebugEnabled)
                        AiDebugLogger.LogOrchestration("物资官·情报失效", $"{iv.Name} - {iv.Reason} {(ok ? "" : "(未找到,跳过)")}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("物资官情报失效失败: {Error}, 情报={Name}", ex.Message, iv.Name);
                }
            }
        }
    }

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    private LedgerDelta? ParseLedgerDelta(string content)
    {
        try
        {
            var cleaned = CleanJsonContent(content);
            var delta = JsonConvert.DeserializeObject<LedgerDelta>(cleaned, _jsonSettings);
            return delta;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("物资官记账结果解析失败: {Error}, 原始内容: {Content}", ex.Message, content);
            return null;
        }
    }

    /// <summary>
    /// 清理AI输出中可能的非标准JSON（去掉markdown代码块标记等）
    /// </summary>
    private static string CleanJsonContent(string content)
    {
        content = content.Trim();

        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0)
                content = content[(firstNewline + 1)..];
            if (content.EndsWith("```"))
                content = content[..^3];
        }

        return content.Trim();
    }
}
