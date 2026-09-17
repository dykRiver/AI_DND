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
    private readonly IServiceScopeFactory _scopeFactory;

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
        AiModelFactory modelFactory,
        IServiceScopeFactory scopeFactory)
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
        _scopeFactory = scopeFactory;
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

            var rejectResult = new GameActionResult
            {
                Narrative = rejectText,
                IsChoicePoint = false,
                Feasibility = classification.Feasibility
            };

            // 场景10补齐：不可行短路也产出2个补救选项（复用书记官轻量模式，以拒绝叙事为[本轮既定事实]）。
            // 仅真实行动(!DryRun)时生成；预计算DryRun只关心可行性判定，无需补救选项。
            // ScribeTask 由 Hub 后台记账链 await 后回填 SuggestedActions 并启动预计算、推送选项。
            if (!input.DryRun)
            {
                var rejectScribeInput = await BuildSuggestionsOnlyInputAsync(sessionId, actionText, rejectText, characterName);
                rejectResult.ScribeTask = RunScribeTaskAsync(sessionId, rejectResult, rejectScribeInput, session.InteractionCount, false, npcs);
            }

            return rejectResult;
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
                ? "常规行动 → 导演流程(跳过骰子)"
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
            JudgmentOutcome = judgmentOutcome,
            CharacterName = characterName,
            IsStagnant = isStagnant,
            SideQuestList = BuildSideQuestList(session.SideQuests),
            HiddenContentList = BuildHiddenContentList(session.HiddenContent),
            // 玩家点选粗粒度推进选项时强制导演走章节档（一次性推演整段剧情并产出大篇幅叙事）
            IsAdvanceAction = input.ActionScale == ActionScales.Advance
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

        // 7. 状态记账（world_state_changes/item_hints/npc_attitude_changes/suggested_actions）
        //    已拆给书记官后台Task（见步骤13），与叙事流式并行；前台仅保留时段推进等毫秒级操作
        var stateUpdate = new GameStateUpdate();

        // 8. 时段推进（前台导演输出time_advance即生效，导演提示词自身约束简单观察/简短对话不推进；DryRun时跳过）
        if (directorOutput.TimeAdvance)
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

        // 9. NPC态度变更已拆给书记官（npc_attitude_changes），由书记官Task落库并回填StateChanges

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

        // 11. 更新会话计数器（DryRun时跳过）；书记官落库沿用递增前的轮次（与旧串行流程一致）
        var applyRound = session.InteractionCount;
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

        // 12. 构建返回结果（NarrativeInput由Hub流式推送叙事；ItemHints/SuggestedActions/ScribeOutput由书记官Task回填）
        var result = new GameActionResult
        {
            NarrativeInput = narrativeInput,
            DiceResult = diceResult,
            StateChanges = stateUpdate,
            IsChoicePoint = directorOutput.PlayerChoicePoint,
            ActionIntent = classification.ActionIntent,
            Feasibility = classification.Feasibility
        };

        // 13. 书记官后台Task（与叙事流式并行）：始终走完整记账模式，接收完整上下文并产出全部结构化字段。
        //     若本轮确实无状态变化，书记官自然输出空 world_state_changes（仅含 summary），不会造成误记账；
        //     但若存在信息获取类行动（查看新短信/阅读新文件等），书记官可正确记录知识状态变更，
        //     避免叙事层与世界状态层脱节。
        var scribeInput = new ScribeInput
        {
            PlayerAction = actionText,
            ActionIntent = classification.ActionIntent,
            JudgmentOutcome = judgmentOutcome,
            DirectorFacts = BuildDirectorFacts(directorOutput),
            WorldState = directorInput.WorldState,
            NpcProfiles = npcProfiles,
            PlayerInventory = playerInventory,
            MainQuestProgress = session.MainQuest ?? "",
            SideQuestList = directorInput.SideQuestList,
            HiddenContentList = directorInput.HiddenContentList,
            CharacterName = characterName,
            SuggestionsOnly = false,
            // 选项节奏档位：抉择点/章节档/高紧张时为关键时刻（细粒度选项），否则输出粗粒度剧情推进型选项
            IsKeyMoment = directorOutput.PlayerChoicePoint
                || directorOutput.BeatScale == "chapter"
                || (directorOutput.Pacing?.TensionLevel ?? 0) >= 8
        };
        result.ScribeTask = RunScribeTaskAsync(sessionId, result, scribeInput, applyRound, input.DryRun, npcs);

        return result;
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

        // 方案A：恢复副本时不推送硬编码旁白，直接续上离开前最后一轮叙事（历史叙事由前端从CheckActiveSession HTTP接口预先恢复）。
        // Hub 在 IsResumed=true 时会跳过 OpeningNarrative 推送，此处保留空字符串以避免语义混乱。
        var resumeNarrative = string.Empty;

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
    
            // 书记官始终走完整记账模式，只要输出了 WorldStateChanges 就落库。
            // 预计算DryRun时书记官已回填ScribeOutput但未落库，此处补落。
            {
                var scribeOutput = result.ScribeOutput;

                // 1. 应用世界状态变更（书记官输出）
                if (scribeOutput?.WorldStateChanges != null)
                {
                    await _worldState.ApplyChangesAsync(sessionId, scribeOutput.WorldStateChanges, session.InteractionCount);
                }
    
                                // 2/3. 道具增减不再于此内联应用：缓存结果提交时由物资官(道具AI)依书记官 item_hints 记账落库（见 GameSessionHub 缓存路径）。
    
                // 5. 更新NPC态度（书记官输出npc_attitude_changes）
                if (scribeOutput?.NpcAttitudeChanges is { Count: > 0 })
                {
                    var npcs = await _npcService.GetCriticalNpcsAsync(sessionId);
                    foreach (var change in scribeOutput.NpcAttitudeChanges.Where(c => c.Change != 0))
                    {
                        try
                        {
                            var npc = npcs.FirstOrDefault(n => n.NpcIdentifier == change.NpcId);
                            if (npc != null)
                            {
                                await _npcService.UpdateAttitudeAsync(sessionId, npc.Id, change.Change);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "缓存行动NPC态度更新失败: {NpcId}", change.NpcId);
                        }
                    }
                }
            }
    
            // 6. 更新交互计数和紧张度（与正常流程一致，每次行动都必须递增）
            session.InteractionCount++;
            if (directorOutput?.Pacing != null)
            {
                session.TensionLevel = directorOutput.Pacing.TensionLevel;
            }
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.InteractionCount, s.TensionLevel })
                .ExecuteCommandAsync();
    
            // 4. 时段推进（已有 TimeAdvanced 守卫）
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
    /// 序列化前台导演输出为书记官的既定事实文本（书记官只记录不改写）
    /// </summary>
    private static string BuildDirectorFacts(DirectorOutput directorOutput)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"场景速写: {directorOutput.NarrativeSeed}");

        if (directorOutput.Beats is { Count: > 0 })
        {
            sb.AppendLine("章节分镜:");
            foreach (var beat in directorOutput.Beats)
                sb.AppendLine($"- [{beat.BeatType}] {beat.Seed}");
        }

        if (directorOutput.NpcActions is { Count: > 0 })
        {
            sb.AppendLine("NPC行为:");
            foreach (var npcAction in directorOutput.NpcActions)
            {
                var surface = npcAction.DialogueDirection?.Surface;
                sb.AppendLine(string.IsNullOrEmpty(surface)
                    ? $"- {npcAction.NpcId}: {npcAction.Action}"
                    : $"- {npcAction.NpcId}: {npcAction.Action}｜台词大意: {surface}");
            }
        }

        if (directorOutput.NarrativeHooks is { Count: > 0 })
            sb.AppendLine($"引导线索: {string.Join("；", directorOutput.NarrativeHooks)}");

        if (directorOutput.Pacing != null)
            sb.AppendLine($"紧张度: {directorOutput.Pacing.TensionLevel}/10 {directorOutput.Pacing.Note}");

        sb.AppendLine($"本轮时段推进: {(directorOutput.TimeAdvance ? "是" : "否")}");
        return sb.ToString();
    }

    /// <summary>
    /// 书记官后台记账Task：独立DI scope执行（防调用方scope提前销毁），
    /// 完成后回填 result 的 ScribeOutput/ItemHints/SuggestedActions/StateChanges；内部自捕异常永不抛。
    /// dryRun=true 时不落库，仅回填字段（供预计算缓存后回放时再落库）。
    /// </summary>
    private Task RunScribeTaskAsync(long sessionId, GameActionResult result, ScribeInput scribeInput, int applyRound, bool dryRun, List<GameNpcProfile> npcs)
    {
        return Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scribe = scope.ServiceProvider.GetRequiredService<ScribeAiService>();
                var output = await scribe.ScribeAsync(scribeInput, sessionId);
                if (output == null)
                    return; // 重试后仍失败：本轮状态不变，叙事不中断（ScribeAiService已记error日志）

                result.ScribeOutput = output;
                result.ItemHints = output.ItemHints;
                result.SuggestedActions = output.SuggestedActions;

                // 选项粒度统一打标：同一节奏档下书记官产出的两个选项必然同粒度，
                // 由代码层依 IsKeyMoment 回填（不让AI输出，避免幻觉）。
                // 粗粒度(advance)选项被点选后，导演将收到[推进型行动]标记并强制走章节档大幅推演。
                if (result.SuggestedActions is { Count: > 0 })
                {
                    var scale = scribeInput.IsKeyMoment ? ActionScales.Detail : ActionScales.Advance;
                    foreach (var sa in result.SuggestedActions)
                        sa.Scale = scale;
                }

                if (DebugEnabled && output.ItemHints is { Count: > 0 })
                    AiDebugLogger.LogOrchestration("物资推荐", string.Join("; ", output.ItemHints.Select(h => $"{h.Change}·{h.Category}:{h.Name}")));

                // 应用世界状态变更（沿用递增前的轮次，与旧串行流程一致）
                // 轻量模式(SuggestionsOnly)不落库：纯叙事轮只取建议选项，即便模型幻觉产出状态也强制忽略
                if (output.WorldStateChanges != null && !scribeInput.SuggestionsOnly)
                {
                    if (!dryRun)
                    {
                        var worldState = scope.ServiceProvider.GetRequiredService<WorldStateService>();
                        await worldState.ApplyChangesAsync(sessionId, output.WorldStateChanges, applyRound);
                    }

                    // 任务进度变化时回填，供Hub推送前端更新支线任务状态
                    if (output.WorldStateChanges.QuestProgress != null && result.StateChanges != null)
                    {
                        result.StateChanges.QuestProgress = output.WorldStateChanges.QuestProgress;
                    }
                }

                // 更新NPC态度（书记官依既定事实判定变化量）；轻量模式不落库
                if (output.NpcAttitudeChanges is { Count: > 0 } && !scribeInput.SuggestionsOnly && result.StateChanges != null)
                {
                    var npcService = scope.ServiceProvider.GetRequiredService<NpcService>();
                    result.StateChanges.NpcAttitudeChanges = new Dictionary<string, int>();
                    foreach (var change in output.NpcAttitudeChanges.Where(c => c.Change != 0))
                    {
                        var npc = npcs.FirstOrDefault(n => n.NpcIdentifier == change.NpcId);
                        if (npc == null) continue;
                        if (!dryRun)
                        {
                            await npcService.UpdateAttitudeAsync(sessionId, npc.Id, change.Change);
                        }
                        result.StateChanges.NpcAttitudeChanges[change.NpcId] = change.Change;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "书记官记账Task异常，本轮状态不变: SessionId={SessionId}", sessionId);
            }
        });
    }

    /// <summary>
    /// 装配“仅产出建议选项”的书记官轻量输入（SuggestionsOnly=true）。
    /// 用于开场类场景（首次进入/重新开始/同题异卷重玩）与不可行短路补救：
    /// 以给定文本（开场叙事或拒绝叙事）作为[本轮既定事实]，复用 scribe_suggestions_system 提示词产出2个引导选项。
    /// </summary>
    private async Task<ScribeInput> BuildSuggestionsOnlyInputAsync(long sessionId, string playerAction, string directorFacts, string characterName)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        var worldState = await _worldState.GetCurrentStateForDirectorAsync(sessionId);
        var npcs = await _npcService.GetCriticalNpcsAsync(sessionId);
        return new ScribeInput
        {
            PlayerAction = playerAction,
            ActionIntent = null,
            JudgmentOutcome = null,
            DirectorFacts = directorFacts,
            WorldState = worldState,
            NpcProfiles = BuildNpcProfilesText(npcs),
            PlayerInventory = await BuildPlayerInventoryText(sessionId),
            // 开场/补救场景保留主线目标以引导选项朝主线推进（新手引导）；不暴露支线/隐藏内容避免剧透
            MainQuestProgress = session?.MainQuest ?? "",
            SideQuestList = "",
            HiddenContentList = "",
            CharacterName = characterName,
            SuggestionsOnly = true
        };
    }

    /// <summary>
    /// 生成“仅建议选项”（轻量模式，不记账不落库）。供 Hub 在开场叙事推送后调用，
    /// 为首次进入/重新开始/同题异卷重玩补齐行动选项（新手引导 + UX 一致性）。
    /// 失败时返回 null，调用方降级为无选项（不阻断副本启动）。
    /// </summary>
    public async Task<List<SuggestedActionInfo>?> GenerateSuggestionsOnlyAsync(long sessionId, string playerAction, string directorFacts, string characterName)
    {
        try
        {
            var scribeInput = await BuildSuggestionsOnlyInputAsync(sessionId, playerAction, directorFacts, characterName);
            using var scope = _scopeFactory.CreateScope();
            var scribe = scope.ServiceProvider.GetRequiredService<ScribeAiService>();
            var output = await scribe.ScribeAsync(scribeInput, sessionId);
            return output?.SuggestedActions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "轻量建议选项生成失败(非致命): SessionId={SessionId}", sessionId);
            return null;
        }
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
    /// 本次行动的粒度（来自被点选选项的 SuggestedActionInfo.Scale）：
    /// ActionScales.Advance 时向导演注入[推进型行动]标记，强制章节档大幅推演；
    /// 玩家自由输入的行动不带此标记（默认 detail）。
    /// </summary>
    public string ActionScale { get; set; } = ActionScales.Detail;

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
