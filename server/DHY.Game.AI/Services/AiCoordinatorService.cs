using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Utils;
using DHY.Game.Core.Dtos;
using DHY.Game.Core.Entities;
using DHY.Game.Core.Services;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// AI协调器服务 - 核心编排
/// </summary>
[ApiDescriptionSettings("Game")]
public class AiCoordinatorService : IDynamicApiController, ITransient
{
    private readonly ActionClassifierService _classifier;
    private readonly DirectorAiService _director;
    private readonly NarrativeAiService _narrative;
    private readonly DungeonArchitectService _architect;
    private readonly NarrativeValidatorService _validator;
    private readonly WorldStateService _worldState;
    private readonly NpcService _npcService;
    private readonly JudgmentService _judgmentService;
    private readonly InventoryService _inventoryService;
    private readonly KnownAssetService _knownAssetService;
    private readonly TimeSegmentService _timeSegmentService;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameDungeonTemplate> _templateRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameNpcProfile> _npcRep;
    private readonly SqlSugarRepository<GameNarrativeLog> _narrativeLogRep;
    private readonly ILogger<AiCoordinatorService> _logger;
    private readonly AiModelFactory _modelFactory;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public AiCoordinatorService(
        ActionClassifierService classifier,
        DirectorAiService director,
        NarrativeAiService narrative,
        DungeonArchitectService architect,
        NarrativeValidatorService validator,
        WorldStateService worldState,
        NpcService npcService,
        JudgmentService judgmentService,
        InventoryService inventoryService,
        KnownAssetService knownAssetService,
        TimeSegmentService timeSegmentService,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameDungeonTemplate> templateRep,
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameNpcProfile> npcRep,
        SqlSugarRepository<GameNarrativeLog> narrativeLogRep,
        ILogger<AiCoordinatorService> logger,
        AiModelFactory modelFactory)
    {
        _classifier = classifier;
        _director = director;
        _narrative = narrative;
        _architect = architect;
        _validator = validator;
        _worldState = worldState;
        _npcService = npcService;
        _judgmentService = judgmentService;
        _inventoryService = inventoryService;
        _knownAssetService = knownAssetService;
        _timeSegmentService = timeSegmentService;
        _sessionRep = sessionRep;
        _templateRep = templateRep;
        _characterRep = characterRep;
        _npcRep = npcRep;
        _narrativeLogRep = narrativeLogRep;
        _logger = logger;
        _modelFactory = modelFactory;
    }

    private bool DebugEnabled => _modelFactory.IsDebugEnabled;

    /// <summary>
    /// 处理玩家行动（核心流程）
    /// </summary>
    [DisplayName("处理玩家行动")]
    [HttpPost("processAction")]
    public async Task<GameActionResult> ProcessPlayerActionAsync([FromBody] ProcessActionInput input)
    {
        var sessionId = input.SessionId;
        var actionText = input.ActionText;
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        // 查询角色名称（供导演AI和叙事AI上下文使用）
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        var characterName = character?.Name ?? "";

        if (DebugEnabled)
            AiDebugLogger.LogOrchestration("开始处理玩家行动", $"SessionId={sessionId}, 输入={actionText}, 成人模式={input.IsAdultMode}");

        // 0. 成人模式快捷通道：玩家手动开启，跳过分类AI和导演AI，直接走成人叙事
        if (input.IsAdultMode)
        {
            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("成人模式快捷通道", "跳过分类AI和导演AI，直接走成人叙事");

            var adultInventory = await BuildPlayerInventoryText(sessionId);
            var adultNarrativeHistory = await _worldState.GetNarrativeHistoryAsync(
                new NarrativeHistoryQueryInput { SessionId = sessionId, Count = 5 });

            return await HandleAdultAction(sessionId, session, actionText, adultNarrativeHistory, adultInventory, characterName);
        }

        // 1. 行动分类（含可行性判定 + 成人内容判定 + 技能判定）
        var classifierState = await _worldState.GetCurrentStateForClassifierAsync(sessionId);
        var playerInventory = await BuildPlayerInventoryText(sessionId);
        var knownAssets = await BuildKnownAssetsText(sessionId);
        // 分类AI的可行性判据 = 背包（物理道具）+ 已知情报（无形资产）构成的完整“可用资产清单”
        var availableAssets = string.IsNullOrEmpty(knownAssets)
            ? playerInventory
            : $"{playerInventory}\n\n【已知情报/线索】\n{knownAssets}";
        var npcs = await _npcService.GetCriticalNpcsAsync(sessionId);
        var npcProfiles = BuildNpcProfilesText(npcs);
        var allNarrativeHistory = await _worldState.GetNarrativeHistoryAsync(new NarrativeHistoryQueryInput { SessionId = sessionId, Count = 10 });
        var classification = await _classifier.ClassifyAsync(actionText, classifierState, availableAssets, npcProfiles);

        // 1.3 叙事历史过滤（成人→非成人转换时，跳过成人记录）
        var lastRecordIsAdult = allNarrativeHistory.FirstOrDefault()?.IsAdult ?? false;
        List<GameNarrativeLog> narrativeHistory;
        if (!classification.IsAdult && lastRecordIsAdult)
        {
            var nonAdultRecords = allNarrativeHistory.Where(l => !l.IsAdult).ToList();
            if (nonAdultRecords.Count < 5)
            {
                nonAdultRecords = await _worldState.GetNarrativeHistoryAsync(
                    new NarrativeHistoryQueryInput { SessionId = sessionId, Count = 20, ExcludeAdult = true });
            }
            narrativeHistory = nonAdultRecords.Take(5).ToList();
            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("叙事历史过滤", $"成人→非成人转换，跳过成人记录，获取{narrativeHistory.Count}条非成人历史");
        }
        else
        {
            narrativeHistory = allNarrativeHistory.Take(5).ToList();
        }

        var recentNarrative = BuildRecentNarrativeText(narrativeHistory);

        // 1.5 可行性短路：分类AI判定行动不可能时，直接返回拒绝叙事
        if (!classification.IsFeasible)
        {
            var rejectText = classification.InfeasibleReason ?? classification.Reason ?? "你无法执行这个行动。";
            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("可行性短路", $"行动不可行: {rejectText}");

            if (!input.DryRun)
            {
                session.InteractionCount++;
                await _sessionRep.AsUpdateable(session)
                    .UpdateColumns(s => new { s.InteractionCount })
                    .ExecuteCommandAsync();

                await _narrativeLogRep.AsInsertable(new GameNarrativeLog
                {
                    SessionId = sessionId,
                    InteractionIndex = session.InteractionCount,
                    PlayerInput = actionText,
                    NarrativeText = rejectText,
                    Timestamp = DateTime.Now
                }).ExecuteCommandAsync();
            }

            return new GameActionResult
            {
                Narrative = rejectText,
                IsChoicePoint = false
            };
        }

        // 2. 成人内容短路：跳过导演AI，直接走叙事AI
        if (classification.IsAdult)
        {
            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("行动分类", "成人内容 → 跳过导演AI，直接叙事");
            return await HandleAdultAction(sessionId, session, actionText, narrativeHistory, playerInventory, characterName);
        }

        var isStagnant = await DetectStagnationAsync(sessionId);
        if (isStagnant && DebugEnabled)
            AiDebugLogger.LogOrchestration("停滞检测", "剧情停滞检测触发，导演AI将主动引入推进线索");

        if (DebugEnabled)
        {
            var routeDesc = classification.IsRoutine
                ? (classification.NeedsStateChange
                    ? "常规行动(需状态变更) → 导演流程(跳过骰子)"
                    : "常规行动(无状态变更) → 导演流程(仅叙事推演)")
                : "非常规行动 → 完整导演流程";
            AiDebugLogger.LogOrchestration("行动分类", routeDesc);
        }

        // 3. 获取世界状态+副本上下文（NPC已在步骤1提前获取）
        var worldState = await _worldState.GetCurrentStateAsync(sessionId);

        // 4. 检查是否需要角色再定位
        string? repositionSnippet = null;
        if (_worldState.ShouldReposition(session.InteractionCount))
        {
            var reposition = await _worldState.GenerateRepositionSnapshotAsync(sessionId);
            repositionSnippet = reposition.StateJson;
        }

        // 5. 若需判定 → 规则引擎掷骰（在导演AI之前，让导演知道成败）
        GameDiceRollRecord? diceResult = null;
        string? judgmentOutcome = null;

        if (classification.Judgment != null && classification.Judgment.Needed &&
            !string.IsNullOrEmpty(classification.Judgment.Skill) &&
            classification.Judgment.Dc > 0)
        {
            // 查出副本世界难度修正
            var difficultyModifier = 0;
            var template = await _templateRep.GetByIdAsync(session.TemplateId);
            if (template != null)
            {
                difficultyModifier = template.DifficultyModifier;
            }

            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("技能判定", $"技能={classification.Judgment.Skill}, DC={classification.Judgment.Dc}, 优势={classification.Judgment.Advantage}, 劣势={classification.Judgment.Disadvantage}, 世界难度修正={difficultyModifier:+#;-#;0}");

            diceResult = await _judgmentService.SkillCheckAsync(
                sessionId,
                classification.Judgment.Skill,
                classification.Judgment.Dc!.Value,
                classification.Judgment.Advantage,
                classification.Judgment.Disadvantage,
                difficultyModifier,
                dryRun: input.DryRun);

            // 构建判定结果文本（注入导演AI上下文）
            var tag = diceResult.IsNatural20 ? "大成功" : diceResult.IsNatural1 ? "大失败" : diceResult.IsSuccess ? "成功" : "失败";
            var modSign = diceResult.Modifier >= 0 ? "+" : "";
            judgmentOutcome = $"{diceResult.SkillName} DC{diceResult.DC} → D20={diceResult.D20Roll}{modSign}{diceResult.Modifier}={diceResult.Total} vs 有效DC{diceResult.EffectiveDC} [{tag}]";

            if (DebugEnabled)
                AiDebugLogger.LogOrchestration("骰子结果", $"D20={diceResult.D20Roll}{modSign}{diceResult.Modifier}={diceResult.Total} vs 原始DC{diceResult.DC}+世界难度{difficultyModifier:+#;-#;0}=有效DC{diceResult.EffectiveDC} → {tag}");

            // 即时回调：在导演AI推演之前推送骰子结果，让玩家在等待期间看到判定详情
            input.OnDiceRolled?.Invoke(diceResult);
        }
        else if (classification.Judgment != null && classification.Judgment.Needed && classification.Judgment.Dc is null or <= 0)
        {
            _logger.LogWarning("分类AI输出的DC无效({Dc})，跳过本次技能检定", classification.Judgment.Dc);
        }

        // 6. 导演AI推演（已知道判定成败，可精准生成叙事方向和世界反应）
        if (DebugEnabled)
            AiDebugLogger.LogOrchestration("导演AI", "开始推演世界反应...");

        var directorInput = new DirectorInput
        {
            PlayerAction = actionText,
            ActionIntent = classification.ActionIntent,
            WorldState = await _worldState.GetCurrentStateForDirectorAsync(sessionId),
            DungeonContext = session.WorldSetting ?? "",
            NpcProfiles = BuildNpcProfilesText(npcs),
            MainQuestProgress = session.MainQuest ?? "",
            RepositionSnippet = repositionSnippet,
            PlayerInventory = playerInventory,
            IsRoutine = classification.IsRoutine,
            NeedsStateChange = classification.NeedsStateChange,
            JudgmentOutcome = judgmentOutcome,
            CharacterName = characterName,
            IsStagnant = isStagnant,
            SideQuestList = BuildSideQuestList(session.SideQuests),
            HiddenContentList = BuildHiddenContentList(session.HiddenContent)
        };

        var directorOutput = await _director.DirectAsync(directorInput, sessionId);
        if (directorOutput == null)
        {
            if (DebugEnabled)
                AiDebugLogger.LogError("Coordinator", "导演AI返回null，流程中断");
            return new GameActionResult
            {
                Narrative = "世界似乎没有对你的行动做出反应……",
                IsChoicePoint = false
            };
        }

        // 7. 物资清单（item_hints）：导演不再直接写背包。
        //    实际资产变更由物资官(道具AI)在导演之后、叙事之前依此权威蓝图记账落库（见 GameSessionHub）。
        //    此处收集蓝图条目，随结果透传给 Hub 供物资官逐条落实。
        var itemHints = directorOutput.ItemHints ?? BuildLegacyItemHints(directorOutput);
        if (DebugEnabled && itemHints is { Count: > 0 })
            AiDebugLogger.LogOrchestration("物资推荐", string.Join("; ", itemHints.Select(h => $"{h.Change}·{h.Category}:{h.Name}")));

        // 8. 应用世界状态变更（仅NeedsStateChange时生效，DryRun时跳过）
        var stateUpdate = new GameStateUpdate();
        if (classification.NeedsStateChange && directorOutput.WorldStateChanges != null)
        {
            if (!input.DryRun)
            {
                await _worldState.ApplyChangesAsync(sessionId, directorOutput.WorldStateChanges, session.InteractionCount);
            }

            // 任务进度变化时传递给Hub，供推送前端更新支线任务状态
            if (directorOutput.WorldStateChanges.QuestProgress != null)
            {
                stateUpdate.QuestProgress = directorOutput.WorldStateChanges.QuestProgress;
            }
        }

        // 8.5 时段推进（仅NeedsStateChange时生效，DryRun时跳过）
        if (classification.NeedsStateChange && directorOutput.TimeAdvance)
        {
            if (!input.DryRun)
            {
                try
                {
                    await _timeSegmentService.AdvanceTimeAsync(new SessionIdInput { SessionId = sessionId });
                    stateUpdate.TimeAdvanced = true;

                    if (DebugEnabled)
                        AiDebugLogger.LogOrchestration("时段推进", $"时段已推进, SessionId={sessionId}");

                    // 同步session对象的时段字段，保证后续代码使用最新值
                    var updatedSession = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
                    if (updatedSession != null)
                    {
                        session.CurrentDay = updatedSession.CurrentDay;
                        session.CurrentSegment = updatedSession.CurrentSegment;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "时段推进失败: SessionId={SessionId}", sessionId);
                }
            }
            else
            {
                stateUpdate.TimeAdvanced = true;
            }
        }

        // 9. 更新NPC态度（仅NeedsStateChange时生效，DryRun时跳过）
        if (classification.NeedsStateChange && directorOutput.NpcActions != null)
        {
            stateUpdate.NpcAttitudeChanges = new Dictionary<string, int>();
            foreach (var npcAction in directorOutput.NpcActions.Where(n => n.AttitudeChange != 0))
            {
                var npc = npcs.FirstOrDefault(n => n.NpcIdentifier == npcAction.NpcId);
                if (npc != null)
                {
                    if (!input.DryRun)
                    {
                        await _npcService.UpdateAttitudeAsync(sessionId, npc.Id, npcAction.AttitudeChange);
                    }
                    stateUpdate.NpcAttitudeChanges[npcAction.NpcId] = npcAction.AttitudeChange;
                }
            }
        }

        // 10. 准备叙事输入（由Hub流式推送到客户端）
        if (DebugEnabled)
            AiDebugLogger.LogOrchestration("叙事AI", "准备NarrativeInput，由Hub流式推送");

        var npcLanguageCards = await BuildNpcLanguageCardsForScene(sessionId, directorOutput);
        var sceneType = DetermineSceneType(directorOutput);
        var narrativeInput = new NarrativeInput
        {
            DirectorBlueprint = directorOutput,
            NpcLanguageCards = npcLanguageCards,
            RecentNarrative = BuildRecentNarrativeText(narrativeHistory),
            JudgmentResult = diceResult,
            SceneType = sceneType,
            WordTarget = ResolveNarrativeWordTarget(directorOutput, sceneType),
            WorldContext = BuildNarrativeWorldContext(session, worldState),
            PlayerInventory = playerInventory,
            CharacterName = characterName,
            StyleBible = BuildStyleBibleText(session),
            MotifTracker = BuildMotifTrackerText(session)
        };

        if (DebugEnabled)
        {
            AiDebugLogger.LogOrchestration("流程结束", $"SessionId={sessionId}, 互动次数={session.InteractionCount + 1}");
        }

        // 11. 更新会话计数器（DryRun时跳过）
        if (!input.DryRun)
        {
            session.InteractionCount++;
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.InteractionCount })
                .ExecuteCommandAsync();

            // 更新紧张度
            if (directorOutput.Pacing != null)
            {
                session.TensionLevel = directorOutput.Pacing.TensionLevel;
                await _sessionRep.AsUpdateable(session)
                    .UpdateColumns(s => new { s.TensionLevel })
                    .ExecuteCommandAsync();
            }
        }

        // 12. 返回结果（NarrativeInput由Hub流式推送叙事）
        return new GameActionResult
        {
            NarrativeInput = narrativeInput,
            DiceResult = diceResult,
            StateChanges = stateUpdate,
            IsChoicePoint = directorOutput.PlayerChoicePoint,
            SuggestedActions = directorOutput.SuggestedActions,
            NeedsStateChange = classification.NeedsStateChange,
            ItemHints = itemHints,
            ActionIntent = classification.ActionIntent,
            Feasibility = classification.Feasibility
        };
    }

    /// <summary>
    /// 启动副本会话
    /// </summary>
    [DisplayName("启动副本会话")]
    [HttpPost("startDungeon")]
    public async Task<DungeonStartResult> StartDungeonSessionAsync([FromBody] StartDungeonSessionInput input)
    {
        var userId = input.UserId;
        var templateId = input.TemplateId;
        try
        {
            // 1. 加载副本模板
            var template = await _templateRep.GetFirstAsync(t => t.Id == templateId);
            if (template == null)
                return new DungeonStartResult { IsSuccess = false, ErrorMessage = "副本模板不存在" };

            // ★ 检查是否有进行中或挂起的会话(Status==0或4)，支持续玩
            var existingSession = await _sessionRep.AsQueryable()
                .Where(s => s.UserId == userId && s.TemplateId == templateId && (s.Status == 0 || s.Status == 4))
                .OrderByDescending(s => s.StartTime)
                .FirstAsync();

            if (existingSession != null)
            {
                // 若为挂起状态，恢复为进行中
                if (existingSession.Status == 4)
                {
                    existingSession.Status = 0;
                    await _sessionRep.AsUpdateable(existingSession)
                        .UpdateColumns(s => new { s.Status })
                        .ExecuteCommandAsync();
                }
                return BuildResumedResult(existingSession, template);
            }

            // 检查是否为重玩
            var isReplay = await _sessionRep.AsQueryable()
                .AnyAsync(s => s.UserId == userId && s.TemplateId == templateId && s.Status != 0);

            // 2. 调用副本建筑师AI
            var architectOutput = await _architect.GenerateDungeonAsync(template, isReplay);
            if (architectOutput == null)
                return new DungeonStartResult { IsSuccess = false, ErrorMessage = "副本生成失败" };

            // 3. 创建GameDungeonSession
            var session = new GameDungeonSession
            {
                UserId = userId,
                TemplateId = templateId,
                Status = 0,
                WorldSetting = JsonConvert.SerializeObject(architectOutput.WorldSetting, _jsonSettings),
                MainQuest = JsonConvert.SerializeObject(architectOutput.MainQuest, _jsonSettings),
                SideQuests = JsonConvert.SerializeObject(architectOutput.SideQuests, _jsonSettings),
                HiddenContent = JsonConvert.SerializeObject(architectOutput.HiddenContent, _jsonSettings),
                DifficultyParams = JsonConvert.SerializeObject(architectOutput.DifficultyParams, _jsonSettings),
                StyleBibleJson = architectOutput.StyleBible != null ? JsonConvert.SerializeObject(architectOutput.StyleBible, _jsonSettings) : null,
                MotifsJson = architectOutput.Motifs != null ? JsonConvert.SerializeObject(architectOutput.Motifs, _jsonSettings) : null,
                CurrentDay = 1,
                CurrentSegment = 0,
                TensionLevel = 1,
                InteractionCount = 0,
                StartTime = DateTime.Now,
                IsReplay = isReplay
            };

            await _sessionRep.AsInsertable(session).ExecuteCommandAsync();

            // 4. 创建所有NPC档案卡
            if (architectOutput.Npcs != null)
            {
                var npcProfiles = _architect.ConvertToNpcProfiles(session.Id, architectOutput.Npcs);
                foreach (var npc in npcProfiles)
                {
                    await _npcRep.AsInsertable(npc).ExecuteCommandAsync();
                }
            }

            // 5. 初始化世界状态（使用结构化局面快照）
            var initialSnapshot = new SituationSnapshotDto
            {
                WorldSetting = architectOutput.WorldSetting != null
                    ? JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(architectOutput.WorldSetting, _jsonSettings)) ?? new()
                    : new(),
                Location = architectOutput.WorldSetting?.KeyLocations?.FirstOrDefault()?.Name ?? "",
                CurrentDay = 1,
                CurrentSegment = "上午",
                PlayerPosition = "",
                PlayerStatus = "正常",
                Environment = "",
                NpcStates = new List<NpcStateDto>(),
                ActiveConditions = new List<string>(),
                Flags = new List<string>(),
                ChangeHistory = new List<ChangeHistoryEntry>()
            };
            await _worldState.InitializeWorldStateAsync(session.Id, JsonConvert.SerializeObject(initialSnapshot, _jsonSettings));

            // 6. 生成开场叙事
            var openingInput = new NarrativeInput
            {
                DirectorBlueprint = new DirectorOutput
                {
                    NarrativeSeed = "光线一点点渗进来，温度从脚底开始回升。远处有什么东西在响——是风，还是机械，你分不清。世界正在从模糊的边缘慢慢凝聚成形。",
                    Pacing = new PacingInfo { TensionLevel = 2, Note = "开场探索氛围" }
                },
                NpcLanguageCards = new List<NpcLanguageCardDto>(),
                RecentNarrative = "",
                SceneType = "opening",
                CharacterName = input.CharacterName,
                WorldContext = BuildNarrativeWorldContext(session, null),
                StyleBible = BuildStyleBibleText(session),
                MotifTracker = BuildMotifTrackerText(session)
            };

            var openingNarrative = await _narrative.GenerateNarrativeAsync(openingInput, session.Id);

            // 记录开场叙事
            await _narrativeLogRep.AsInsertable(new GameNarrativeLog
            {
                SessionId = session.Id,
                InteractionIndex = 0,
                PlayerInput = "[副本开始]",
                NarrativeText = openingNarrative,
                Timestamp = DateTime.Now
            }).ExecuteCommandAsync();

            return BuildNewStartResult(session, template, architectOutput, openingNarrative);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "副本启动失败");
            return new DungeonStartResult { IsSuccess = false, ErrorMessage = $"副本启动异常: {ex.Message}" };
        }
    }

    /// <summary>
    /// 构建续玩结果（从已有会话恢复）
    /// </summary>
    private DungeonStartResult BuildResumedResult(GameDungeonSession session, GameDungeonTemplate template)
    {
        _logger.LogInformation("续玩副本: UserId={UserId}, SessionId={SessionId}", session.UserId, session.Id);

        // 解析世界设定
        WorldSettingData? worldSetting = null;
        if (!string.IsNullOrEmpty(session.WorldSetting))
        {
            worldSetting = JsonConvert.DeserializeObject<WorldSettingData>(session.WorldSetting, _jsonSettings);
        }

        // 解析主线任务
        MainQuestData? mainQuest = null;
        if (!string.IsNullOrEmpty(session.MainQuest))
        {
            mainQuest = JsonConvert.DeserializeObject<MainQuestData>(session.MainQuest, _jsonSettings);
        }

        // 生成回归叙事（简短提示，告知玩家回到副本世界）
        var resumeNarrative = $"你重新回到了{template.Name}的世界。眼前的一切似曾相识……";

        return new DungeonStartResult
        {
            IsSuccess = true,
            SessionId = session.Id,
            OpeningNarrative = resumeNarrative,
            IsResumed = true,
            DungeonName = template.Name,
            WorldSettingSummary = worldSetting != null
                ? $"{worldSetting.Era} | {worldSetting.Geography}"
                : "",
            WorldBackground = BuildWorldBackground(worldSetting),
            MainQuestObjective = mainQuest?.Objective ?? "",
            MainQuestNodes = mainQuest?.KeyNodes ?? new List<string>(),
            KeyLocations = worldSetting?.KeyLocations?.Select(l => $"{l.Name}: {l.Description}").ToList()
                ?? new List<string>(),
            SideQuests = BuildSideQuestBriefList(session.SideQuests)
        };
    }

    /// <summary>
    /// 构建新启动结果
    /// </summary>
    private static DungeonStartResult BuildNewStartResult(
        GameDungeonSession session,
        GameDungeonTemplate template,
        DungeonArchitectOutput architectOutput,
        string openingNarrative)
    {
        return new DungeonStartResult
        {
            IsSuccess = true,
            SessionId = session.Id,
            OpeningNarrative = openingNarrative,
            IsResumed = false,
            DungeonName = template.Name,
            WorldSettingSummary = architectOutput.WorldSetting != null
                ? $"{architectOutput.WorldSetting.Era} | {architectOutput.WorldSetting.Geography}"
                : "",
            WorldBackground = BuildWorldBackground(architectOutput.WorldSetting),
            MainQuestObjective = architectOutput.MainQuest?.Objective ?? "",
            MainQuestNodes = architectOutput.MainQuest?.KeyNodes ?? new List<string>(),
            KeyLocations = architectOutput.WorldSetting?.KeyLocations?.Select(l => $"{l.Name}: {l.Description}").ToList()
                ?? new List<string>(),
            SideQuests = architectOutput.SideQuests?
                .Where(sq => !string.Equals(sq.InitialVisibility, "hidden", StringComparison.OrdinalIgnoreCase))
                .Select(sq => new SideQuestBriefInfo
                {
                    Name = sq.Name,
                    Description = sq.Description
                }).ToList() ?? new List<SideQuestBriefInfo>()
        };
    }

    /// <summary>
    /// 从世界设定构建背景描述文本
    /// </summary>
    private static string BuildWorldBackground(WorldSettingData? setting)
    {
        if (setting == null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(setting.Era)) parts.Add($"时代: {setting.Era}");
        if (!string.IsNullOrEmpty(setting.TechnologyLevel)) parts.Add($"科技水平: {setting.TechnologyLevel}");
        if (!string.IsNullOrEmpty(setting.Culture)) parts.Add($"文化: {setting.Culture}");
        if (!string.IsNullOrEmpty(setting.Geography)) parts.Add($"地理: {setting.Geography}");
        return string.Join("\n", parts);
    }

        /// <summary>
        /// 应用缓存行动结果到数据库（预计算缓存命中时由 Hub 调用）
        /// </summary>
        public async Task ApplyCachedActionResultAsync(long sessionId, GameActionResult result, string actionText)
        {
            var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
            if (session == null) return;
    
            var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
            var directorOutput = result.NarrativeInput?.DirectorBlueprint;
    
            // 仅在分类AI判定需要状态变更时才执行持久化（与正常流程一致）
            if (result.NeedsStateChange)
            {
                // 1. 应用世界状态变更
                if (directorOutput?.WorldStateChanges != null)
                {
                    await _worldState.ApplyChangesAsync(sessionId, directorOutput.WorldStateChanges, session.InteractionCount);
                }
    
                                // 2/3. 道具增减不再于此内联应用：缓存结果提交时由物资官(道具AI)依导演蓝图 item_hints 记账落库（见 GameSessionHub 缓存路径）。
    
                // 5. 更新NPC态度
                if (directorOutput?.NpcActions != null)
                {
                    var npcs = await _npcService.GetCriticalNpcsAsync(sessionId);
                    foreach (var npcAction in directorOutput.NpcActions.Where(n => n.AttitudeChange != 0))
                    {
                        try
                        {
                            var npc = npcs.FirstOrDefault(n => n.NpcIdentifier == npcAction.NpcId);
                            if (npc != null)
                            {
                                await _npcService.UpdateAttitudeAsync(sessionId, npc.Id, npcAction.AttitudeChange);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "缓存行动NPC态度更新失败: {NpcId}", npcAction.NpcId);
                        }
                    }
                }
            }
    
            // 6. 更新交互计数和紧张度（与正常流程一致，不受NeedsStateChange限制，每次行动都必须递增）
            session.InteractionCount++;
            if (directorOutput?.Pacing != null)
            {
                session.TensionLevel = directorOutput.Pacing.TensionLevel;
            }
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.InteractionCount, s.TensionLevel })
                .ExecuteCommandAsync();
    
            // 4. 时段推进（已有 TimeAdvanced 守卫，不受 NeedsStateChange 影响）
            if (result.StateChanges?.TimeAdvanced == true)
            {
                try
                {
                    await _timeSegmentService.AdvanceTimeAsync(new SessionIdInput { SessionId = sessionId });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "缓存行动时段推进失败: SessionId={SessionId}", sessionId);
                }
            }
        }
    
    #region 私有辅助方法

    private async Task<GameActionResult> HandleAdultAction(long sessionId, GameDungeonSession session, string actionText, List<GameNarrativeLog> narrativeHistory, string playerInventory, string characterName)
    {
        var worldState = await _worldState.GetCurrentStateAsync(sessionId);

        var narrativeInput = new NarrativeInput
        {
            IsAdult = true,
            PlayerAction = actionText,
            RecentNarrative = BuildRecentNarrativeText(narrativeHistory),
            WorldContext = BuildNarrativeWorldContext(session, worldState),
            PlayerInventory = playerInventory,
            SceneType = "adult",
            CharacterName = characterName
        };

        // 更新会话计数器
        session.InteractionCount++;
        await _sessionRep.AsUpdateable(session)
            .UpdateColumns(s => new { s.InteractionCount })
            .ExecuteCommandAsync();

        return new GameActionResult
        {
            NarrativeInput = narrativeInput,
            IsChoicePoint = false
        };
    }

    /// <summary>
    /// 构建叙事AI的世界上下文摘要（精简文本，供叙事AI维持世界观一致性）
    /// </summary>
    public static string BuildNarrativeWorldContext(GameDungeonSession session, GameWorldState? worldState)
    {
        var sb = new StringBuilder();

        // 世界设定摘要
        if (!string.IsNullOrEmpty(session.WorldSetting))
        {
            try
            {
                var ws = JObject.Parse(session.WorldSetting);
                var parts = new List<string>();
                if (ws["era"]?.ToString() is string era && !string.IsNullOrEmpty(era))
                    parts.Add($"时代:{era}");
                if (ws["technology_level"]?.ToString() is string tech && !string.IsNullOrEmpty(tech))
                    parts.Add($"科技:{tech}");
                if (ws["culture"]?.ToString() is string culture && !string.IsNullOrEmpty(culture))
                    parts.Add($"文化:{culture}");
                if (ws["geography"]?.ToString() is string geo && !string.IsNullOrEmpty(geo))
                    parts.Add($"地理:{geo}");
                if (parts.Count > 0)
                    sb.AppendLine(string.Join(" | ", parts));
            }
            catch { }
        }

        // 主线目标
        if (!string.IsNullOrEmpty(session.MainQuest))
        {
            try
            {
                var mq = JObject.Parse(session.MainQuest);
                if (mq["objective"]?.ToString() is string obj && !string.IsNullOrEmpty(obj))
                    sb.AppendLine($"主线目标: {obj}");
            }
            catch { }
        }

        // 当前世界状态要点
        if (!string.IsNullOrEmpty(worldState?.StateJson))
        {
            try
            {
                var state = JObject.Parse(worldState.StateJson);
                var changes = new List<string>();
                foreach (var prop in state.Properties())
                {
                    // 跳过元数据和历史字段，只保留有叙事价值的状态
                    if (prop.Name is "world_setting" or "timeline" or "current_day" or "current_segment" or "change_history")
                        continue;
                    var val = prop.Value.ToString();
                    if (!string.IsNullOrEmpty(val) && val != "{}" && val != "[]")
                        changes.Add($"{prop.Name}: {val}");
                }
                if (changes.Count > 0)
                {
                    sb.AppendLine("世界状态: " + string.Join("; ", changes));
                }
            }
            catch { }
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildNpcProfilesText(List<GameNpcProfile> npcs)
    {
        if (npcs == null || npcs.Count == 0) return "无NPC";

        var sb = new StringBuilder();
        foreach (var npc in npcs)
        {
            sb.AppendLine($"[{npc.NpcIdentifier}] {npc.Name} - {npc.Role}");
            sb.AppendLine($"  性格: {npc.Personality}, 态度: {npc.CurrentAttitude}");
            sb.AppendLine($"  位置: {npc.Location}, 存活: {npc.IsAlive}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// 构建支线任务清单文本（供导演AI标记完成时精确匹配）
    /// </summary>
    private static string BuildSideQuestList(string? sideQuestsJson)
    {
        if (string.IsNullOrEmpty(sideQuestsJson)) return "";
        try
        {
            var quests = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SideQuestData>>(sideQuestsJson);
            if (quests == null || quests.Count == 0) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < quests.Count; i++)
            {
                var sq = quests[i];
                var visibility = string.Equals(sq.InitialVisibility, "hidden", StringComparison.OrdinalIgnoreCase) ? "隐藏" : "可见";
                sb.AppendLine($"{i + 1}. {sq.Name} - {sq.Description} [{visibility}] 触发: {sq.Trigger}");
            }
            return sb.ToString().TrimEnd();
        }
        catch { return ""; }
    }

    /// <summary>
    /// 构建隐藏内容清单文本（供导演AI标记发现时精确匹配）
    /// </summary>
    private static string BuildHiddenContentList(string? hiddenContentJson)
    {
        if (string.IsNullOrEmpty(hiddenContentJson)) return "";
        try
        {
            var contents = Newtonsoft.Json.JsonConvert.DeserializeObject<List<HiddenContentData>>(hiddenContentJson);
            if (contents == null || contents.Count == 0) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < contents.Count; i++)
            {
                var hc = contents[i];
                sb.AppendLine($"{i + 1}. {hc.Content} [隐藏] 触发: {hc.TriggerCondition}");
            }
            return sb.ToString().TrimEnd();
        }
        catch { return ""; }
    }

    /// <summary>
    /// 从 session.SideQuests JSON解析支线任务简要列表（供前端展示，仅立即可见的支线）
    /// </summary>
    private static List<SideQuestBriefInfo> BuildSideQuestBriefList(string? sideQuestsJson)
    {
        if (string.IsNullOrEmpty(sideQuestsJson)) return new List<SideQuestBriefInfo>();
        try
        {
            var quests = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SideQuestData>>(sideQuestsJson);
            return quests?
                .Where(sq => !string.Equals(sq.InitialVisibility, "hidden", StringComparison.OrdinalIgnoreCase))
                .Select(sq => new SideQuestBriefInfo
                {
                    Name = sq.Name,
                    Description = sq.Description
                }).ToList() ?? new List<SideQuestBriefInfo>();
        }
        catch { return new List<SideQuestBriefInfo>(); }
    }

    private static string BuildRecentNarrativeText(List<GameNarrativeLog> logs)
    {
        if (logs == null || logs.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var log in logs.OrderBy(l => l.InteractionIndex))
        {
            if (!string.IsNullOrEmpty(log.PlayerInput) && log.PlayerInput != "[副本开始]")
                sb.AppendLine($"[玩家] {log.PlayerInput}");
            if (!string.IsNullOrEmpty(log.NarrativeText))
                sb.AppendLine($"[叙事] {log.NarrativeText}");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// 构建文风圣经文本（从session的StyleBibleJson解析并渲染为叙事AI可消费的文本）
    /// </summary>
    private static string BuildStyleBibleText(GameDungeonSession session)
    {
        if (string.IsNullOrEmpty(session.StyleBibleJson)) return "";
        try
        {
            var sb = JsonConvert.DeserializeObject<StyleBibleData>(session.StyleBibleJson, _jsonSettings);
            if (sb == null) return "";

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(sb.Tone)) parts.Add($"语调: {sb.Tone}");
            if (!string.IsNullOrEmpty(sb.SentenceRhythm)) parts.Add($"句式: {sb.SentenceRhythm}");
            if (sb.SensoryPalette is { Count: > 0 }) parts.Add($"感官调色板: {string.Join("/", sb.SensoryPalette)}");
            if (sb.ForbiddenCliches is { Count: > 0 }) parts.Add($"禁用陈词: {string.Join("、", sb.ForbiddenCliches)}");

            return parts.Count > 0 ? string.Join("\n", parts) : "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// 构建意象追踪文本（从session的MotifsJson解析并渲染为叙事AI可消费的文本）
    /// </summary>
    private static string BuildMotifTrackerText(GameDungeonSession session)
    {
        if (string.IsNullOrEmpty(session.MotifsJson)) return "";
        try
        {
            var motifs = JsonConvert.DeserializeObject<List<MotifData>>(session.MotifsJson, _jsonSettings);
            if (motifs == null || motifs.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine("贯穿意象（鼓励在叙事中使用，每次赋予新含义）:");
            foreach (var m in motifs)
            {
                sb.AppendLine($"  - 「{m.Name}」初始: {m.InitialState}");
                if (!string.IsNullOrEmpty(m.EvolutionHint))
                    sb.AppendLine($"    进化方向: {m.EvolutionHint}");
            }
            return sb.ToString().TrimEnd();
        }
        catch { return ""; }
    }

    /// <summary>
    /// 构建玩家背包摘要文本（供导演AI上下文使用）
    /// </summary>
    private async Task<string> BuildPlayerInventoryText(long sessionId)
    {
        try
        {
            var backpack = await _inventoryService.GetBackpackAsync(new SessionIdInput { SessionId = sessionId });
            if (backpack.Items.Count == 0)
                return "背包空无";

            var sb = new StringBuilder();
            sb.AppendLine($"负重: {backpack.CurrentWeight}/{backpack.MaxWeight} ({backpack.WeightPercent}%){(backpack.IsEncumbered ? " [负重惩罚]" : "")}{(backpack.IsOverloaded ? " [超重!]" : "")}");

            // 装备栏
            if (backpack.EquippedWeapon != null)
            {
                var w = backpack.EquippedWeapon;
                var uses = w.IsUnlimited ? "∞" : $"{w.CurrentUses}/{w.MaxUses}";
                sb.AppendLine($"[装备-武器] {w.ItemName} ({w.LinkedAttribute}+{w.AttributeBonus}, 余量{uses})");
            }
            else
                sb.AppendLine("[装备-武器] 无");

            if (backpack.EquippedArmor != null)
            {
                var a = backpack.EquippedArmor;
                var uses = a.IsUnlimited ? "∞" : $"{a.CurrentUses}/{a.MaxUses}";
                sb.AppendLine($"[装备-防具] {a.ItemName} ({a.LinkedAttribute}+{a.AttributeBonus}, 余量{uses})");
            }
            else
                sb.AppendLine("[装备-防具] 无");

            // 其他道具
            var otherItems = backpack.Items.Where(i => !i.IsEquipped).ToList();
            if (otherItems.Count > 0)
            {
                sb.Append("背包: ");
                sb.AppendLine(string.Join(", ", otherItems.Select(i =>
                    i.IsKeyItem ? $"{i.ItemName}[关键]" : $"{i.ItemName}x{i.Quantity}")));
            }

            return sb.ToString().TrimEnd();
        }
        catch
        {
            return "背包状态未知";
        }
    }

    /// <summary>
    /// 构建已知情报/无形资产清单文本（供分类AI做可行性判定的“可用资产清单”一部分）。
    /// 空账本时返回空串。
    /// </summary>
    private async Task<string> BuildKnownAssetsText(long sessionId)
    {
        try
        {
            var assets = await _knownAssetService.ListValidAsync(new SessionIdInput { SessionId = sessionId });
            if (assets == null || assets.Count == 0)
                return "";

            return string.Join("\n", assets.Select(a =>
            {
                var content = string.IsNullOrWhiteSpace(a.Content) ? "" : $"：{a.Content}";
                return $"- [{a.AssetType}] {a.Name}{content}";
            }));
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// 兼容存量导演输出：当导演未给 item_hints 却仍输出了旧版 acquired_items/consumed_items 时，
    /// 转换为非权威物资推荐线索，供物资官参考（不落库）。
    /// </summary>
    private static List<ItemHintInfo>? BuildLegacyItemHints(DirectorOutput directorOutput)
    {
        var hints = new List<ItemHintInfo>();
        if (directorOutput.AcquiredItems is { Count: > 0 })
        {
            hints.AddRange(directorOutput.AcquiredItems.Select(a => new ItemHintInfo
            {
                Name = a.ItemName,
                Category = "物品",
                Change = "获得",
                Note = a.Description
            }));
        }
        if (directorOutput.ConsumedItems is { Count: > 0 })
        {
            hints.AddRange(directorOutput.ConsumedItems.Select(c => new ItemHintInfo
            {
                Name = c.ItemName,
                Category = "物品",
                Change = "消耗",
                Note = c.Reason
            }));
        }
        return hints.Count > 0 ? hints : null;
    }

    private async Task<List<NpcLanguageCardDto>> BuildNpcLanguageCardsForScene(long sessionId, DirectorOutput directorOutput)
    {
        var cards = new List<NpcLanguageCardDto>();

        if (directorOutput.NpcActions == null || directorOutput.NpcActions.Count == 0)
            return cards;

        var npcIds = directorOutput.NpcActions.Select(n => n.NpcId).ToArray();
        var npcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == sessionId && npcIds.Contains(n.NpcIdentifier))
            .ToListAsync();

        foreach (var npc in npcs)
        {
            cards.Add(new NpcLanguageCardDto
            {
                NpcName = npc.Name,
                LanguageStyle = npc.LanguageStyle ?? "",
                Catchphrase = npc.Catchphrase ?? "",
                CurrentAttitude = npc.CurrentAttitude
            });
        }

        return cards;
    }

    // 叙事目标字数的安全区间：防止导演给出离谱值导致注水或过短
    // 上限提高到3000以支持章节档（chapter）的整章叙事
    private const int MinNarrativeWordTarget = 80;
    private const int MaxNarrativeWordTarget = 3000;

    /// <summary>
    /// 解析本轮叙事目标字数：
    /// 优先按导演给出的 beat_scale 档位确定字数区间（micro/normal/chapter），
    /// 导演给出的具体 narrative_word_target 在档位区间内生效、未给则取档位中值；
    /// 导演未输出 beat_scale 时回退到场景类型默认值以保证向后兼容。
    /// </summary>
    private static int ResolveNarrativeWordTarget(DirectorOutput director, string sceneType)
    {
        var scale = (director.BeatScale ?? "").Trim().ToLowerInvariant();
        var target = director.NarrativeWordTarget;

        // 按节拍档位确定字数区间
        var (lo, hi) = scale switch
        {
            "chapter" => (1800, 3000),
            "normal" => (600, 1200),
            "micro" => (200, 600),
            _ => (0, 0) // 未分档：回退到旧的场景默认逻辑
        };

        if (lo == 0 && hi == 0)
        {
            if (target <= 0)
            {
                // 导演未输出目标字数时，按场景类型取一个居中的默认值
                target = sceneType switch
                {
                    "daily" => 150,
                    "dialogue" => 250,
                    "action" => 200,
                    "critical" => 500,
                    "opening" or "exploration" => 250,
                    _ => 250
                };
            }
            return Math.Clamp(target, MinNarrativeWordTarget, MaxNarrativeWordTarget);
        }

        // 有分档：导演给的具体值优先，clamp 到档位区间；未给则取档位中值
        if (target <= 0)
            target = (lo + hi) / 2;
        return Math.Clamp(target, lo, hi);
    }

    private static string DetermineSceneType(DirectorOutput director)
    {
        if (director.Pacing == null) return "daily";

        var tension = director.Pacing.TensionLevel;

        // 根据紧张度和叙事内容推断场景类型
        // 高紧张 + 无NPC对话 = 战斗场景
        if (tension >= 8) return "critical";
        if (tension >= 6) return "action";

        // 有NPC对话行为 = 对话场景
        if (director.NpcActions is { Count: > 0 } &&
            director.NpcActions.Any(n => n.DialogueDirection != null && !string.IsNullOrEmpty(n.DialogueDirection.Surface)))
            return "dialogue";

        // 低紧张 + 有narrative_hooks = 探索场景
        if (tension <= 3 && director.NarrativeHooks is { Count: > 0 })
            return "exploration";

        // 低紧张 + 无NPC = 日常
        if (tension <= 3) return "daily";

        // 中等紧张 = 对话
        return "dialogue";
    }

    private async Task<ValidationContext> BuildValidationContext(long sessionId, int maxWordCount)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        var allNpcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == sessionId)
            .ToListAsync();

        // 获取隐藏内容关键词
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        var hiddenKeywords = new List<string>();
        if (!string.IsNullOrEmpty(session?.HiddenContent))
        {
            try
            {
                var hiddenItems = JsonConvert.DeserializeObject<List<HiddenContentData>>(session.HiddenContent, _jsonSettings);
                if (hiddenItems != null)
                {
                    hiddenKeywords = hiddenItems.Select(h => h.Content).Where(c => !string.IsNullOrEmpty(c)).ToList();
                }
            }
            catch { }
        }

        return new ValidationContext
        {
            Character = character,
            AliveNpcs = allNpcs.Where(n => n.IsAlive).Select(n => n.Name).ToList(),
            DeadNpcs = allNpcs.Where(n => !n.IsAlive).Select(n => n.Name).ToList(),
            HiddenContent = hiddenKeywords,
            MaxWordCount = maxWordCount
        };
    }

    /// <summary>
    /// 检测剧情是否停滞（连续N轮无实质性世界状态推进）
    /// </summary>
    private async Task<bool> DetectStagnationAsync(long sessionId)
    {
        var state = await _worldState.GetCurrentStateAsync(sessionId);
        if (state?.StateJson == null) return false;

        try
        {
            var snapshot = JsonConvert.DeserializeObject<SituationSnapshotDto>(state.StateJson, _jsonSettings);
            if (snapshot?.ChangeHistory == null || snapshot.ChangeHistory.Count < 3) return false;

            // 取最近3轮的历史
            var recentHistory = snapshot.ChangeHistory
                .OrderByDescending(h => h.Round)
                .Take(3)
                .ToList();

            // 判断标准：最近3轮的summary都很短且不包含关键推进词汇
            var progressKeywords = new[] { "发现", "获得", "进入", "战斗", "逃离", "解锁", "触发", "对话", "交易", "改变", "打开", "破解", "移动", "抵达", "击败", "说服", "线索", "任务" };
            var stagnantCount = recentHistory.Count(h =>
                string.IsNullOrEmpty(h.Summary) ||
                h.Summary.Length < 15 ||
                !progressKeywords.Any(k => h.Summary.Contains(k)));

            return stagnantCount >= 3;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// 处理玩家行动输入
/// </summary>
public class ProcessActionInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>行动文本</summary>
    public string ActionText { get; set; } = "";
    /// <summary>成人模式开关（前端玩家手动切换，开启后跳过分类AI和导演AI，直接走成人叙事）</summary>
    public bool IsAdultMode { get; set; }
    /// <summary>干跑模式（预计算用，跳过所有DB写入但完整执行AI管线）</summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// 骰子掷出后的即时回调（用于在导演AI推演期间提前推送骰子结果给前端展示）
    /// </summary>
    public Action<GameDiceRollRecord>? OnDiceRolled { get; set; }
}

/// <summary>
/// 启动副本会话输入
/// </summary>
public class StartDungeonSessionInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>副本模板ID</summary>
    public long TemplateId { get; set; }
    /// <summary>角色名称</summary>
    public string CharacterName { get; set; } = "";
}
