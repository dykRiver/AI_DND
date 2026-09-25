using DHY.Game.AI.Dtos;
using DHY.Game.AI.Services;
using DHY.Game.Core.Dtos;
using DHY.Game.Core.Entities;
using DHY.Game.Core.Services;
using DHY.Game.Hub.Dtos;
using DHY.Game.Hub.Services;
using Furion.InstantMessaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DHY.Game.Hub.Hubs;

/// <summary>
/// 游戏会话Hub - SignalR实时通信入口
/// </summary>
[MapHub("/hubs/gameSession")]
public class GameSessionHub : Hub<IGameSessionHub>
{
    private readonly AiCoordinatorService _aiCoordinator;
    private readonly GameSessionManager _sessionManager;
    private readonly HubBroadcastService _broadcast;
    private readonly TimeSegmentService _timeSegmentService;
    private readonly ScoringService _scoringService;
    private readonly SettlementNarrativeService _settlementNarrative;
    private readonly MetaProgressionService _metaProgression;
    private readonly ActionPrecomputeService _precomputeService;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameNarrativeLog> _narrativeLogRep;
    private readonly SqlSugarRepository<GamePlayerMeta> _metaRep;
    private readonly SkillService _skillService;
    private readonly ILogger<GameSessionHub> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public GameSessionHub(
        AiCoordinatorService aiCoordinator,
        GameSessionManager sessionManager,
        HubBroadcastService broadcast,
        TimeSegmentService timeSegmentService,
        ScoringService scoringService,
        SettlementNarrativeService settlementNarrative,
        MetaProgressionService metaProgression,
        ActionPrecomputeService precomputeService,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameNarrativeLog> narrativeLogRep,
        SqlSugarRepository<GamePlayerMeta> metaRep,
        SkillService skillService,
        ILogger<GameSessionHub> logger,
        IServiceScopeFactory scopeFactory)
    {
        _aiCoordinator = aiCoordinator;
        _sessionManager = sessionManager;
        _broadcast = broadcast;
        _timeSegmentService = timeSegmentService;
        _scoringService = scoringService;
        _settlementNarrative = settlementNarrative;
        _metaProgression = metaProgression;
        _precomputeService = precomputeService;
        _sessionRep = sessionRep;
        _characterRep = characterRep;
        _narrativeLogRep = narrativeLogRep;
        _metaRep = metaRep;
        _skillService = skillService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    #region 生命周期

    /// <summary>
    /// 连接时解析JWT获取UserId并注册
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var token = httpContext?.Request.Query["access_token"].ToString();

        if (!string.IsNullOrEmpty(token))
        {
            var claims = JWTEncryption.ReadJwtToken(token)?.Claims;
            var userIdStr = claims?.FirstOrDefault(u => u.Type == ClaimConst.UserId)?.Value;

            if (long.TryParse(userIdStr, out var userId))
            {
                _sessionManager.RegisterConnection(Context.ConnectionId, userId);
                _logger.LogInformation("游戏Hub连接: UserId={UserId}, ConnectionId={ConnectionId}", userId, Context.ConnectionId);

                // 检查是否有活跃会话(支持断线重连)
                var activeSession = _sessionManager.GetActiveSession(userId);
                if (activeSession.HasValue)
                {
                    await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
                    {
                        Type = "info",
                        Message = "已恢复活跃会话连接",
                        Timestamp = DateTime.Now
                    });
                }
            }
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 断开时从管理器移除(不中断游戏会话)
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _sessionManager.GetUserId(Context.ConnectionId);
        if (userId.HasValue)
        {
            _logger.LogInformation("游戏Hub断开: UserId={UserId}, ConnectionId={ConnectionId}", userId.Value, Context.ConnectionId);
        }

        // 仅移除连接映射，不移除会话映射(支持重连)
        _sessionManager.RemoveConnection(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region 客户端调用方法

    /// <summary>
    /// 选择副本并开始游戏（异步返回，后台生成完成后通过SignalR推送DungeonReady）
    /// </summary>
    public async Task SelectDungeon(SelectDungeonInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        var connectionId = Context.ConnectionId;

        // 1. 立即推送生成进度
        await Clients.Caller.DungeonGenerating(new GeneratingProgressDto
        {
            Phase = "世界生成中",
            ProgressPercent = 0,
            Message = "正在构建副本世界..."
        });

        // 2. 立即返回，后台异步处理（fire-and-forget）
        _ = ProcessSelectDungeonAsync(userId.Value, connectionId, input);
    }

    /// <summary>
    /// 后台处理副本生成，完成后通过DungeonReady推送给客户端
    /// </summary>
    private async Task ProcessSelectDungeonAsync(long userId, string connectionId, SelectDungeonInput input)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiCoordinator = scope.ServiceProvider.GetRequiredService<AiCoordinatorService>();
        var sessionRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameDungeonSession>>();
        var characterRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameCharacter>>();
        var broadcast = scope.ServiceProvider.GetRequiredService<HubBroadcastService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameSessionHub, IGameSessionHub>>();
        var metaProgression = scope.ServiceProvider.GetRequiredService<MetaProgressionService>();
        var skillService = scope.ServiceProvider.GetRequiredService<SkillService>();

        try
        {
            // 3. 调用AI协调器启动副本
            var result = await aiCoordinator.StartDungeonSessionAsync(new StartDungeonSessionInput { UserId = userId, TemplateId = input.TemplateId, CharacterName = input.CharacterName });

            if (!result.IsSuccess)
            {
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error",
                    Message = result.ErrorMessage ?? "副本启动失败",
                    Timestamp = DateTime.Now
                });
                return;
            }

            GameCharacter character;

            if (result.IsResumed)
            {
                // ★ 续玩模式：加载已有角色
                character = await characterRep.GetFirstAsync(c => c.SessionId == result.SessionId)
                    ?? throw new InvalidOperationException("续玩会话但角色不存在");
            }
            else
            {
                // 新建角色
                character = new GameCharacter
                {
                    UserId = userId,
                    SessionId = result.SessionId,
                    Name = input.CharacterName,
                    Strength = input.Strength,
                    Dexterity = input.Dexterity,
                    Constitution = input.Constitution,
                    Intelligence = input.Intelligence,
                    Wisdom = input.Wisdom,
                    Charisma = input.Charisma,
                    CurrentHp = 10 + (input.Constitution - 10) / 2,
                    MaxHp = 10 + (input.Constitution - 10) / 2,
                    Level = 1,
                    IsInCombat = false,
                    IsFatigued = false
                };
                await characterRep.AsInsertable(character).ExecuteCommandAsync();
            }

            // 3.1 确保技能记录就绪（从Meta层拉取快照，兼容续玩历史角色无技能的场景）
            try
            {
                var meta = await metaProgression.GetMetaAsync(new UserIdInput { UserId = userId });
                await skillService.InitializeCharacterSkillsFromMetaAsync(meta.Id, character.Id);
            }
            catch (Exception skillEx)
            {
                // 技能初始化失败不阻断副本启动，仅记录警告
                _logger.LogWarning(skillEx, "副本技能快照初始化失败(非致命): UserId={UserId}, CharacterId={CharacterId}", userId, character.Id);
            }

            // 4. 注册活跃会话
            _sessionManager.SetActiveSession(userId, result.SessionId);

            // 5. 流式推送开场叙事（仅限新建副本）
            //    恢复副本时跳过旁白推送，直接续上离开前最后一轮叙事（方案A）：
            //    历史叙事由前端 Lobby.resumeDungeon 从 checkActiveSession HTTP 接口预先恢复；
            //    Hub 仅负责在 DungeonReady 后补推离开前的建议行动选项。
            if (!result.IsResumed)
            {
                // 新建副本：先推送场景转换分割线
                await hubContext.Clients.Client(connectionId).ReceiveNarrative(new NarrativeChunkDto
                {
                    Text = "",
                    ChunkType = "scene_transition",
                    IsLast = true,
                    Timestamp = DateTime.Now
                });
                await broadcast.StreamNarrativeAsync(userId, result.OpeningNarrative, "narrative");
            }

            // 6. 查询session构建游戏状态
            var session = await sessionRep.GetFirstAsync(s => s.Id == result.SessionId);
            var gameState = BuildGameState(
                character,
                session?.CurrentDay ?? 1,
                session?.CurrentSegment ?? 0,
                session?.TensionLevel ?? 1);

            // ★ 7. 推送副本就绪通知（客户端收到后进入游戏）
            await hubContext.Clients.Client(connectionId).DungeonReady(new DungeonReadyDto
            {
                SessionId = result.SessionId,
                DungeonName = result.DungeonName,
                WorldInfo = new WorldInfoDto
                {
                    DungeonName = result.DungeonName,
                    WorldBackground = result.WorldBackground,
                    MainQuestObjective = result.MainQuestObjective,
                    MainQuestNodes = result.MainQuestNodes,
                    KeyLocations = result.KeyLocations,
                    SideQuests = result.SideQuests?.Select(sq => new SideQuestInfoDto
                    {
                        Name = sq.Name,
                        Description = sq.Description,
                        IsCompleted = false
                    }).ToList() ?? new List<SideQuestInfoDto>()
                },
                GameState = gameState,
                // 同步玩家当前目标（断线恢复时前端渲染目标chip与清空按钮；新建副本时为空）
                CurrentPlayerGoal = session?.CurrentPlayerGoal ?? ""
            });

            // ★ 8. 恢复副本：推送离开前最后一轮的建议行动选项（方案A：续上离开前一刻）
            //    仅以文本推送（IsComputing=false、IsFeasible默认true），不重跑预计算（省token）：
            //    玩家点选后服务端 ProcessSelectCachedActionAsync 自动分流：
            //      内存缓存命中（1天TTL）→秒响应；未命中→以 ActionText 走常规全链路。
            if (result.IsResumed && !string.IsNullOrEmpty(session?.LastSuggestedActions))
            {
                try
                {
                    var restoredOptions = JsonConvert.DeserializeObject<List<SuggestedActionInfo>>(session!.LastSuggestedActions!);
                    if (restoredOptions != null && restoredOptions.Count >= 2)
                    {
                        var optionDtos = restoredOptions.Take(2).Select((sa, i) => new SuggestedActionOptionDto
                        {
                            Index = i,
                            ActionText = sa.ActionText,
                            Hint = sa.Hint,
                            // 恢复路径不预置可行性：前端默认可点，点选后服务端根据实际缓存状态分流
                            IsFeasible = true
                        }).ToList();

                        // 埋点①重置：以当前时刻作为选项推送时刻（供后续思考间隔统计）
                        _precomputeService.MarkOptionsShown(result.SessionId);

                        await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                        {
                            Options = optionDtos,
                            IsComputing = false
                        });
                    }
                }
                catch (Exception restoreEx)
                {
                    _logger.LogWarning(restoreEx, "恢复副本时反序列化 LastSuggestedActions 失败(非致命): SessionId={SessionId}", result.SessionId);
                }
            }

            // ★ 9. 新建副本（首次进入/同题异卷重玩）：生成开场行动选项（方案C-1，复用书记官轻量模式）
            //    开场叙事作为[本轮既定事实]，书记官 SuggestionsOnly 产出2个引导选项（新手引导 + UX 一致性）。
            //    与常规轮一致：持久化 LastSuggestedActions + 启动预计算 + 推送选项（预计算未完成时先显示加载态）。
            if (!result.IsResumed && character != null)
            {
                try
                {
                    var openingOptions = await aiCoordinator.GenerateSuggestionsOnlyAsync(
                        result.SessionId, "[副本开场]", result.OpeningNarrative, input.CharacterName);
                    if (openingOptions != null && openingOptions.Count >= 2)
                    {
                        var optionsToCompute = openingOptions.Take(2).ToList();

                        // 持久化选项文本（挂起/断线恢复时可继续显示按钮）
                        if (session != null)
                        {
                            session.LastSuggestedActions = JsonConvert.SerializeObject(optionsToCompute);
                            await sessionRep.AsUpdateable(session)
                                .UpdateColumns(s => new { s.LastSuggestedActions })
                                .ExecuteCommandAsync();
                        }

                        // 启动预计算（玩家点选时秒响应；与常规轮 ledgerTask 内的预计算一致）
                        var precomputeTask = _precomputeService.PrecomputeAsync(result.SessionId, optionsToCompute, null, false, input.IsVipMode);
                        var optionDtos = optionsToCompute.Select((sa, i) => new SuggestedActionOptionDto
                        {
                            Index = i,
                            ActionText = sa.ActionText,
                            Hint = sa.Hint
                        }).ToList();

                        var alreadyReady = precomputeTask.IsCompleted;
                        _precomputeService.MarkOptionsShown(result.SessionId);
                        await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                        {
                            Options = alreadyReady ? ApplyFeasibility(result.SessionId, optionDtos) : optionDtos,
                            IsComputing = !alreadyReady
                        });

                        // 预计算尚未完成时，等其结束后再通知前端 IsComputing=false（并回填可行性）
                        if (!alreadyReady)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await precomputeTask;
                                    await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                                    {
                                        Options = ApplyFeasibility(result.SessionId, optionDtos),
                                        IsComputing = false
                                    });
                                }
                                catch { /* 预计算失败，按钮保持禁用，玩家可手动输入 */ }
                            });
                        }
                    }
                }
                catch (Exception openingEx)
                {
                    _logger.LogWarning(openingEx, "开场选项生成失败(非致命): SessionId={SessionId}", result.SessionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SelectDungeon后台处理失败: UserId={UserId}", userId);
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "error",
                Message = "副本启动异常，请稍后重试",
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 玩家行动（异步返回，后台处理完成后通过SignalR推送结果）
    /// </summary>
    public async Task PlayerAction(PlayerActionInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        var connectionId = Context.ConnectionId;

        // 1. 立即推送推演进度
        await Clients.Caller.DungeonGenerating(new GeneratingProgressDto
        {
            Phase = "世界推演中",
            ProgressPercent = 50,
            Message = "正在处理你的行动..."
        });

        // 2. 禁用输入
        await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
        {
            Type = "input_control",
            Message = "disable",
            Timestamp = DateTime.Now
        });

        // 3. 立即返回，后台异步处理（fire-and-forget）
        _ = ProcessPlayerActionAsync(userId.Value, connectionId, input);
    }

    /// <summary>
    /// 设置玩家当前目标（自由输入框新语义：目标声明，非本轮行动）。
    /// 仅写库 + 推送一条轻量反馈消息，不触发任何AI调用、不刷新当前选项、不失效预计算缓存。
    /// 下一轮书记官构造ScribeInput时读取该字段，让下一批suggested_actions围绕目标生成。
    /// </summary>
    public async Task SetPlayerGoal(SetPlayerGoalInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        // 服务端兼底：目标文本截断至200字（前端已限100字）
        var goalText = (input.GoalText ?? string.Empty).Trim();
        if (goalText.Length > 200)
            goalText = goalText.Substring(0, 200);

        try
        {
            var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId && s.UserId == userId.Value);
            if (session == null)
            {
                await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error",
                    Message = "会话不存在或无权访问",
                    Timestamp = DateTime.Now
                });
                return;
            }

            session.CurrentPlayerGoal = goalText;
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.CurrentPlayerGoal })
                .ExecuteCommandAsync();

            // 轻量反馈：仅在目标非空时推送确认消息（空目标视为静默覆盖，不弹提示）
            if (!string.IsNullOrEmpty(goalText))
            {
                await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "info",
                    Message = "已记下你的想法，接下来的选项会围绕它展开",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置玩家目标失败: SessionId={SessionId}", input.SessionId);
            await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "error",
                Message = "目标记录失败，请稍后重试",
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 后台处理玩家行动，完成后通过SignalR推送结果给客户端
    /// </summary>
    private async Task ProcessPlayerActionAsync(long userId, string connectionId, PlayerActionInput input)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiCoordinator = scope.ServiceProvider.GetRequiredService<AiCoordinatorService>();
        var narrativeAi = scope.ServiceProvider.GetRequiredService<NarrativeAiService>();
        var sessionRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameDungeonSession>>();
        var characterRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameCharacter>>();
        var narrativeLogRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameNarrativeLog>>();
        var broadcast = scope.ServiceProvider.GetRequiredService<HubBroadcastService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameSessionHub, IGameSessionHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameSessionHub>>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();
        var quartermaster = scope.ServiceProvider.GetRequiredService<QuartermasterAiService>();
        var knownAssetService = scope.ServiceProvider.GetRequiredService<KnownAssetService>();

        // 后台记账链句柄（物资官记账→推送→启动预计算），与叙事流式并行执行；finally中兜底等待，防止scope提前销毁
        Task<Task?>? ledgerTask = null;

        try
        {
            // 0. 清除预计算缓存（玩家发起新行动，旧缓存失效）
            _precomputeService.InvalidateCache(input.SessionId);

            // 0.1 背包重量校验：>=100%则阻断行动，要求整理背包
            var weightCheck = await inventoryService.CheckWeightAsync(input.SessionId);
            if (weightCheck.IsBlocked)
            {
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error",
                    Message = $"背包超重({weightCheck.CurrentWeight}/{weightCheck.MaxWeight})，请先整理背包丢弃道具后再行动！",
                    Timestamp = DateTime.Now
                });
                // 恢复输入
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "input_control",
                    Message = "enable",
                    Timestamp = DateTime.Now
                });
                return;
            }

            // 1. 调用AI协调器处理行动
            var processInput = new ProcessActionInput
            {
                SessionId = input.SessionId,
                ActionText = input.ActionText,
                IsAdultMode = input.IsAdultMode,
                // 手动世界难度覆盖值透传（开启时替换模板难度，关闭时为 null 回退模板）
                WorldDifficultyOverride = input.WorldDifficultyOverride,
                // 粒度透传：仅粗粒度选项回退路径会为 advance，玩家自由输入永远是 detail
                ActionScale = input.ActionScale,
                // 骰子掷出后立即推送结果给前端，让玩家在导演AI推演期间看到判定详情
                OnDiceRolled = dice =>
                {
                    hubContext.Clients.Client(connectionId).ReceiveDiceResult(new DiceResultDto
                    {
                        SkillName = dice.SkillName ?? "",
                        D20Roll = dice.D20Roll,
                        Modifier = dice.Modifier,
                        Total = dice.Total,
                        DC = dice.DC,
                        WorldDifficultyModifier = dice.WorldDifficultyModifier,
                        EffectiveDC = dice.EffectiveDC,
                        IsSuccess = dice.IsSuccess,
                        IsNatural20 = dice.IsNatural20,
                        IsNatural1 = dice.IsNatural1,
                        NarrativeHint = dice.NarrativeSummary
                    }).GetAwaiter().GetResult();
                }
            };
            var result = await aiCoordinator.ProcessPlayerActionAsync(processInput);

            // 2. 若有骰子判定 → 推送
            if (result.DiceResult != null)
            {
                await hubContext.Clients.Client(connectionId).ReceiveDiceResult(new DiceResultDto
                {
                    SkillName = result.DiceResult.SkillName ?? "",
                    D20Roll = result.DiceResult.D20Roll,
                    Modifier = result.DiceResult.Modifier,
                    Total = result.DiceResult.Total,
                    DC = result.DiceResult.DC,
                    WorldDifficultyModifier = result.DiceResult.WorldDifficultyModifier,
                    EffectiveDC = result.DiceResult.EffectiveDC,
                    IsSuccess = result.DiceResult.IsSuccess,
                    IsNatural20 = result.DiceResult.IsNatural20,
                    IsNatural1 = result.DiceResult.IsNatural1,
                    NarrativeHint = result.DiceResult.NarrativeSummary
                });
            }

            // 2.5 后台记账链：书记官记账 → 物资官记账 → 背包/情报推送 → 启动下一轮预计算，整链与步骤3叙事流式并行。
            //     叙事的PlayerInventory在协调器内记账前已构建，对记账结果无依赖，可安全并行。
            //     链首先 await 书记官Task（ItemHints/SuggestedActions/StateChanges由其回填）；
            //     门控：书记官是资产变更的唯一来源，无 item_hints（纯对话/观察轮）即跳过物资官记账。
            var session = await sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
            var character = await characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
            ledgerTask = Task.Run(async () =>
            {
                try
                {
                    // 书记官记账（与叙事并行）：落库world_state_changes/NPC态度，回填ItemHints/SuggestedActions
                    if (result.ScribeTask != null)
                    {
                        await result.ScribeTask;
                    }

                    LedgerDelta? ledgerDelta = null;
                    if (result.ItemHints is { Count: > 0 })
                    {
                        try
                        {
                            ledgerDelta = await quartermaster.RecordFromBlueprintAsync(
                                input.SessionId,
                                result.ActionIntent ?? input.ActionText,
                                BuildItemHintsText(result.ItemHints),
                                await BuildLedgerText(inventoryService, knownAssetService, input.SessionId),
                                session?.InteractionCount ?? 0,
                                result.ItemHints);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "道具AI记账失败: SessionId={SessionId}", input.SessionId);
                        }
                    }

                    // 记账产生物理道具变更 → 推送背包更新（在叙事流式期间到达前端）
                    var hasPhysicalChange = ledgerDelta != null &&
                        ((ledgerDelta.AcquiredItems is { Count: > 0 }) ||
                         (ledgerDelta.ConsumedItems is { Count: > 0 }) ||
                         (ledgerDelta.LostItems is { Count: > 0 }));
                    if (hasPhysicalChange && character != null)
                    {
                        var backpack = await inventoryService.GetBackpackAsync(new SessionIdInput { SessionId = input.SessionId });
                        await hubContext.Clients.Client(connectionId).UpdateInventory(new InventoryUpdateDto
                        {
                            Items = backpack.Items.Select(i => new InventoryItemDto
                            {
                                Id = i.Id,
                                ItemName = i.ItemName,
                                ItemType = i.ItemType,
                                Description = i.Description,
                                Weight = i.Weight,
                                AttributeBonus = i.AttributeBonus,
                                LinkedAttribute = i.LinkedAttribute,
                                MaxUses = i.MaxUses,
                                CurrentUses = i.CurrentUses,
                                IsUnlimited = i.IsUnlimited,
                                IsEquipped = i.IsEquipped,
                                IsKeyItem = i.IsKeyItem,
                                Quantity = i.Quantity
                            }).ToList(),
                            CurrentWeight = backpack.CurrentWeight,
                            MaxWeight = backpack.MaxWeight,
                            WeightPercent = backpack.WeightPercent,
                            IsOverloaded = backpack.IsOverloaded,
                            IsEncumbered = backpack.IsEncumbered,
                            EquippedWeaponId = character.EquippedWeaponId,
                            EquippedArmorId = character.EquippedArmorId
                        });
                    }

                    // 记账产生无形情报变更 → 推送已知情报更新
                    var hasInfoChange = ledgerDelta != null &&
                        ((ledgerDelta.AcquiredInfo is { Count: > 0 }) || (ledgerDelta.InvalidatedInfo is { Count: > 0 }));
                    if (hasInfoChange)
                    {
                        await PushKnownAssetsAsync(hubContext, knownAssetService, connectionId, input.SessionId);
                    }

                    // 记账完成后启动下一轮预计算（保证其分类AI读到本轮最新账本）
                    if (result.SuggestedActions != null && result.SuggestedActions.Count >= 2)
                    {
                        var optionsToCompute = result.SuggestedActions.Take(2).ToList();
                        var isAdultRound = result.NarrativeInput?.IsAdult ?? false;

                        // 持久化选项文本到 session.LastSuggestedActions：
                        // 副本挂起/断线恢复时前端可继续显示按钮；缓存命中走秒响应，未命中以文本走常规全链路。
                        // 成人轮跳过持久化，避免断线恢复时显示成人选项文本（保留原门控语义）。
                        if (session != null && !isAdultRound)
                        {
                            try
                            {
                                session.LastSuggestedActions = JsonConvert.SerializeObject(optionsToCompute);
                                await sessionRep.AsUpdateable(session)
                                    .UpdateColumns(s => new { s.LastSuggestedActions })
                                    .ExecuteCommandAsync();
                            }
                            catch (Exception persistEx)
                            {
                                logger.LogWarning(persistEx, "持久化 LastSuggestedActions 失败(非致命): SessionId={SessionId}", input.SessionId);
                            }
                        }

                        // 预计算继承本轮会话级成人模式：成人轮也用成人模型预掷，从下一次预计算起全链路切换（对齐世界难度override语义）
                        return (Task?)Task.Run(() => _precomputeService.PrecomputeAsync(input.SessionId, optionsToCompute, input.WorldDifficultyOverride, input.IsAdultMode, input.IsVipMode));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "后台记账链执行失败: SessionId={SessionId}", input.SessionId);
                }
                return null;
            });

            // 3. 流式推送叙事（真流式：AI生成token实时推送到客户端，与后台记账链并行）
            string narrativeText = result.Narrative;
            if (result.NarrativeInput != null && session != null)
            {
                var chunkType = result.DiceResult != null ? "action_result" : "narrative";
                narrativeText = await broadcast.StreamNarrativeLiveAsync(
                    userId, narrativeAi, result.NarrativeInput, input.SessionId, chunkType);

                // 记录叙事日志
                await narrativeLogRep.AsInsertable(new GameNarrativeLog
                {
                    SessionId = input.SessionId,
                    InteractionIndex = session.InteractionCount,
                    PlayerInput = input.ActionText,
                    NarrativeText = narrativeText,
                    Timestamp = DateTime.Now,
                    IsAdult = result.NarrativeInput.IsAdult
                }).ExecuteCommandAsync();
            }
            else if (!string.IsNullOrEmpty(result.Narrative))
            {
                // 不可行短路：拒绝叙事已在协调器内记录日志，此处仅推送文本
                // （与缓存路径 ProcessSelectCachedActionAsync 对齐，修复此前 PlayerAction 路径短路无反馈的缺陷）
                narrativeText = result.Narrative;
                await broadcast.StreamNarrativeAsync(userId, narrativeText, "narrative");
            }

            // 3.9 等待后台记账链收尾（通常已在叙事流式期间跑完），取回预计算任务句柄
            Task? precomputeTask = await ledgerTask;
            ledgerTask = null;

            // 4. 推送游戏状态更新
            if (session != null && character != null)
            {
                await hubContext.Clients.Client(connectionId).UpdateGameState(
                    BuildGameState(character, session.CurrentDay, session.CurrentSegment, session.TensionLevel));
            }

            // 5. 若有时段变化 → 推送
            if (result.StateChanges?.TimeAdvanced == true && session != null)
            {
                await hubContext.Clients.Client(connectionId).SendTimeTransition(new TimeTransitionDto
                {
                    Narrative = GetTimeTransitionNarrative(session.CurrentSegment),
                    NewSegment = GetSegmentName(session.CurrentSegment),
                    NewDay = session.CurrentDay
                });
            }

            // 7. 任务进度变化时推送支线任务状态更新
            if (result.StateChanges?.QuestProgress != null)
            {
                var unlockedQuestInfos = new List<SideQuestInfoDto>();
                var unlockedNames = result.StateChanges.QuestProgress.UnlockedSideQuests ?? new List<string>();
                if (unlockedNames.Count > 0 && session?.SideQuests != null)
                {
                    try
                    {
                        var allQuests = JsonConvert.DeserializeObject<List<SideQuestData>>(session.SideQuests);
                        if (allQuests != null)
                        {
                            var unlockedSet = new HashSet<string>(unlockedNames);
                            unlockedQuestInfos = allQuests
                                .Where(sq => unlockedSet.Contains(sq.Name))
                                .Select(sq => new SideQuestInfoDto
                                {
                                    Name = sq.Name,
                                    Description = sq.Description,
                                    IsCompleted = (result.StateChanges.QuestProgress.CompletedSideQuests ?? new List<string>()).Contains(sq.Name)
                                }).ToList();
                        }
                    }
                    catch { /* 解析失败时推送空解锁列表 */ }
                }

                await hubContext.Clients.Client(connectionId).UpdateSideQuests(new SideQuestUpdateDto
                {
                    CompletedSideQuests = result.StateChanges.QuestProgress.CompletedSideQuests ?? new List<string>(),
                    UnlockedSideQuests = unlockedQuestInfos
                });
            }

            // 8. 恢复输入
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "input_control",
                Message = "enable",
                Timestamp = DateTime.Now
            });

            // 9. 推送建议行动选项（预计算已在步骤2.5记账链中与叙事流式并行启动）
            if (precomputeTask != null && result.SuggestedActions != null)
            {
                var optionDtos = result.SuggestedActions.Take(2).Select((sa, i) => new SuggestedActionOptionDto
                {
                    Index = i,
                    ActionText = sa.ActionText,
                    Hint = sa.Hint
                }).ToList();

                // 若预计算已在阅读期间跑完，则按钮直接可点；否则先显示加载态
                var alreadyReady = precomputeTask.IsCompleted;
                _precomputeService.MarkOptionsShown(input.SessionId); // 埋点①：记录选项推送时刻
                await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                {
                    Options = alreadyReady ? ApplyFeasibility(input.SessionId, optionDtos) : optionDtos,
                    IsComputing = !alreadyReady
                });

                // 预计算尚未完成时，等其结束后再通知前端 IsComputing=false（并回填可行性）
                if (!alreadyReady)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await precomputeTask;
                            await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                            {
                                Options = ApplyFeasibility(input.SessionId, optionDtos),
                                IsComputing = false
                            });
                        }
                        catch { /* 预计算失败，按钮保持禁用，玩家可手动输入 */ }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PlayerAction后台处理失败: SessionId={SessionId}", input.SessionId);
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "error",
                Message = "行动处理异常，请稍后重试",
                Timestamp = DateTime.Now
            });
            // 恢复输入
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "input_control",
                Message = "enable",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            // 叙事异常中断时，确保后台记账链在scope销毁前跑完（链内已自行捕获异常）
            if (ledgerTask != null)
            {
                try { await ledgerTask; } catch { /* 已在链内记录 */ }
            }
        }
    }

    /// <summary>
    /// 选择缓存行动选项（从预计算结果中快速获取结果）
    /// </summary>
    public async Task SelectCachedAction(SelectCachedActionInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        var connectionId = Context.ConnectionId;

        // 1. 立即推送推演进度
        await Clients.Caller.DungeonGenerating(new GeneratingProgressDto
        {
            Phase = "世界推演中",
            ProgressPercent = 50,
            Message = "正在处理你的行动..."
        });

        // 2. 禁用输入
        await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
        {
            Type = "input_control",
            Message = "disable",
            Timestamp = DateTime.Now
        });

        // 3. fire-and-forget
        _ = ProcessSelectCachedActionAsync(userId.Value, connectionId, input);
    }

    /// <summary>
    /// 根据预计算缓存回填各建议选项的可行性（不可行选项前端将置灰不可点击）
    /// </summary>
    private List<SuggestedActionOptionDto> ApplyFeasibility(long sessionId, List<SuggestedActionOptionDto> options)
    {
        var cached = _precomputeService.GetCachedOptions(sessionId);
        if (cached == null) return options;
        foreach (var o in options)
        {
            var co = cached.Options.ElementAtOrDefault(o.Index);
            if (co != null) o.IsFeasible = co.IsFeasible;
        }
        return options;
    }

    /// <summary>
    /// 将导演蓝图物资清单(item_hints)拼为文本，供道具AI依蓝图记账（权威事实基准）。
    /// </summary>
    private static string BuildItemHintsText(List<ItemHintInfo>? hints)
    {
        if (hints == null || hints.Count == 0)
            return "";
        return string.Join("\n", hints.Select(h =>
        {
            var note = string.IsNullOrWhiteSpace(h.Note) ? "" : $"（{h.Note}）";
            var key = h.IsKey ? "[关键]" : "";
            return $"- [{h.Change}·{h.Category}]{key} {h.Name}{note}";
        }));
    }

    /// <summary>
    /// 构建当前账本文本（背包物理道具 + 有效已知情报），供道具AI记账时避免重复登记。
    /// </summary>
    private static async Task<string> BuildLedgerText(InventoryService inventoryService, KnownAssetService knownAssetService, long sessionId)
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            var backpack = await inventoryService.GetBackpackAsync(new SessionIdInput { SessionId = sessionId });
            if (backpack.Items.Count > 0)
            {
                sb.AppendLine("【背包物理道具】");
                foreach (var i in backpack.Items)
                {
                    var key = i.IsKeyItem ? "[关键]" : "";
                    sb.AppendLine($"- {i.ItemName}{key} x{i.Quantity}（{i.ItemType}）");
                }
            }
        }
        catch { /* 背包读取失败时留空，道具AI以导演蓝图为准 */ }

        try
        {
            var assets = await knownAssetService.ListValidAsync(new SessionIdInput { SessionId = sessionId });
            if (assets is { Count: > 0 })
            {
                sb.AppendLine("【已知情报/线索】");
                foreach (var a in assets)
                {
                    var content = string.IsNullOrWhiteSpace(a.Content) ? "" : $"：{a.Content}";
                    sb.AppendLine($"- [{a.AssetType}] {a.Name}{content}");
                }
            }
        }
        catch { /* 情报读取失败时留空 */ }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 拉取会话的有效已知情报并推送给客户端。
    /// </summary>
    private static async Task PushKnownAssetsAsync(
        IHubContext<GameSessionHub, IGameSessionHub> hubContext,
        KnownAssetService knownAssetService,
        string connectionId,
        long sessionId)
    {
        var assets = await knownAssetService.ListValidAsync(new SessionIdInput { SessionId = sessionId });
        await hubContext.Clients.Client(connectionId).UpdateKnownAssets(new KnownAssetsUpdateDto
        {
            Assets = assets.Select(a => new KnownAssetDto
            {
                Id = a.Id,
                AssetType = a.AssetType,
                Name = a.Name,
                Content = a.Content,
                Source = a.Source,
                AcquiredRound = a.AcquiredRound
            }).ToList()
        });
    }

    /// <summary>
    /// 从已持久化的 session.LastSuggestedActions 按索引反查选项粒度。
    /// 用于内存预计算缓存已丢（服务重启/挂起恢复）但玩家仍点选选项的回退场景：
    /// 粗粒度推进选项不能因缓存丢失而退化成普通行动。任何异常或缺失均回退 detail。
    /// </summary>
    private static async Task<string> ResolveScaleFromSessionAsync(
        SqlSugarRepository<GameDungeonSession> sessionRep, long sessionId, int optionIndex, ILogger logger)
    {
        try
        {
            var session = await sessionRep.GetFirstAsync(s => s.Id == sessionId);
            if (string.IsNullOrEmpty(session?.LastSuggestedActions))
                return ActionScales.Detail;

            var options = JsonConvert.DeserializeObject<List<SuggestedActionInfo>>(session.LastSuggestedActions!);
            if (options == null || optionIndex < 0 || optionIndex >= options.Count)
                return ActionScales.Detail;

            return string.IsNullOrWhiteSpace(options[optionIndex].Scale)
                ? ActionScales.Detail
                : options[optionIndex].Scale;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "反查选项粒度失败(非致命，按detail处理): SessionId={SessionId}, Index={Index}", sessionId, optionIndex);
            return ActionScales.Detail;
        }
    }

    /// <summary>
    /// 后台处理缓存行动选择，应用缓存结果并持久化
    /// </summary>
    private async Task ProcessSelectCachedActionAsync(long userId, string connectionId, SelectCachedActionInput input)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiCoordinator = scope.ServiceProvider.GetRequiredService<AiCoordinatorService>();
        var narrativeAi = scope.ServiceProvider.GetRequiredService<NarrativeAiService>();
        var sessionRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameDungeonSession>>();
        var characterRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameCharacter>>();
        var narrativeLogRep = scope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameNarrativeLog>>();
        var broadcast = scope.ServiceProvider.GetRequiredService<HubBroadcastService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameSessionHub, IGameSessionHub>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GameSessionHub>>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();
        var quartermaster = scope.ServiceProvider.GetRequiredService<QuartermasterAiService>();
        var knownAssetService = scope.ServiceProvider.GetRequiredService<KnownAssetService>();

        // 后台记账链句柄（物资官记账→推送→启动预计算），与叙事回放并行执行；finally中兜底等待，防止scope提前销毁
        Task<Task?>? ledgerTask = null;

        try
        {
            // 0. 背包重量校验：与常规行动路径一致，>=100%则阻断行动
            var weightCheck = await inventoryService.CheckWeightAsync(input.SessionId);
            if (weightCheck.IsBlocked)
            {
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error",
                    Message = $"背包超重({weightCheck.CurrentWeight}/{weightCheck.MaxWeight})，请先整理背包丢弃道具后再行动！",
                    Timestamp = DateTime.Now
                });
                // 恢复输入
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "input_control",
                    Message = "enable",
                    Timestamp = DateTime.Now
                });
                return;
            }

            // 1. 从缓存获取预计算结果
            // 埋点①：玩家思考间隔 = 点击时刻 - 选项推送时刻（须在 InvalidateCache 前读取）
            var optionsShownAt = _precomputeService.GetOptionsShownAt(input.SessionId);
            if (optionsShownAt != null)
                logger.LogInformation("玩家思考间隔: SessionId={SessionId}, Index={Index}, 间隔ms={Interval}",
                    input.SessionId, input.OptionIndex, (long)(DateTime.Now - optionsShownAt.Value).TotalMilliseconds);

            var cached = _precomputeService.GetCachedResult(input.SessionId, input.OptionIndex);
            if (cached?.Result == null)
            {
                // 缓存未命中或已过期：把选项文本当作玩家输入，走常规全链路演算
                var fallbackText = !string.IsNullOrWhiteSpace(input.ActionText)
                    ? input.ActionText
                    : cached?.ActionText;
                logger.LogWarning("缓存未命中: SessionId={SessionId}, Index={Index}, 以选项文本「{ActionText}」回退常规流程",
                    input.SessionId, input.OptionIndex, fallbackText);

                // 清除缓存
                _precomputeService.InvalidateCache(input.SessionId);

                // 选项文本也不可得（旧客户端未传且缓存全丢）：提示玩家重新输入，不用占位行动替代
                if (string.IsNullOrWhiteSpace(fallbackText))
                {
                    await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                    {
                        Type = "warning",
                        Message = "该行动选项已过期，请直接输入你的行动。",
                        Timestamp = DateTime.Now
                    });
                    await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                    {
                        Type = "input_control",
                        Message = "enable",
                        Timestamp = DateTime.Now
                    });
                    return;
                }

                var fallbackInput = new PlayerActionInput
                {
                    SessionId = input.SessionId,
                    ActionText = fallbackText,
                    IsAdultMode = input.IsAdultMode,
                    // 手动世界难度覆盖值不能丢：回退常规全链路时仍需带上玩家设定的难度修正
                    WorldDifficultyOverride = input.WorldDifficultyOverride,
                    // 粒度不能丢：粗粒度推进选项回退常规流程时仍需让导演走章节档。
                    // 缓存尚在时直取；缓存已丢（如服务重启）则从已持久化的 LastSuggestedActions 按索引反查。
                    ActionScale = cached?.Scale
                        ?? await ResolveScaleFromSessionAsync(sessionRep, input.SessionId, input.OptionIndex, logger)
                };
                await ProcessPlayerActionAsync(userId, connectionId, fallbackInput);
                return;
            }

            var result = cached.Result;
            var actionText = cached.ActionText;

            // 2. 推送骰子结果（如有）
            if (result.DiceResult != null)
            {
                await hubContext.Clients.Client(connectionId).ReceiveDiceResult(new DiceResultDto
                {
                    SkillName = result.DiceResult.SkillName ?? "",
                    D20Roll = result.DiceResult.D20Roll,
                    Modifier = result.DiceResult.Modifier,
                    Total = result.DiceResult.Total,
                    DC = result.DiceResult.DC,
                    WorldDifficultyModifier = result.DiceResult.WorldDifficultyModifier,
                    EffectiveDC = result.DiceResult.EffectiveDC,
                    IsSuccess = result.DiceResult.IsSuccess,
                    IsNatural20 = result.DiceResult.IsNatural20,
                    IsNatural1 = result.DiceResult.IsNatural1,
                    NarrativeHint = result.DiceResult.NarrativeSummary
                });
            }

            // 3. 清除当前缓存（越早越好，防重入）。落库/记账/选项构建见下（L1改造后移到 await 书记官之后）。
            _precomputeService.InvalidateCache(input.SessionId);

            // 3.5 后台链：await书记官回填 → 落库 → 物资官记账 → 背包/情报推送 → 启动下一轮预计算。
            //     【L1改造】预计算只跑到导演层、未 await 书记官，故链首 await result.ScribeTask 回填
            //     ScribeOutput/ItemHints/SuggestedActions/QuestProgress，再落库（ApplyCached 依赖 ScribeOutput）。
            //     书记官自预计算导演完成即 fire-and-forget 启动，点选时大概率已完成，await 基本不阻塞；整链与步骤4叙事并行。
            ledgerTask = Task.Run(async () =>
            {
                try
                {
                    if (result.ScribeTask != null)
                        await result.ScribeTask;

                    await aiCoordinator.ApplyCachedActionResultAsync(input.SessionId, result, actionText);

                    var sessionL = await sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
                    var characterL = await characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);

                    LedgerDelta? ledgerDelta = null;
                    if (result.ItemHints is { Count: > 0 })
                    {
                        try
                        {
                            ledgerDelta = await quartermaster.RecordFromBlueprintAsync(
                                input.SessionId,
                                result.ActionIntent ?? actionText,
                                BuildItemHintsText(result.ItemHints),
                                await BuildLedgerText(inventoryService, knownAssetService, input.SessionId),
                                sessionL?.InteractionCount ?? 0,
                                result.ItemHints);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "缓存行动道具AI记账失败: SessionId={SessionId}", input.SessionId);
                        }
                    }

                    // 记账产生物理道具变更 → 推送背包更新（在叙事回放期间到达前端）
                    var hasPhysicalChange = ledgerDelta != null &&
                        ((ledgerDelta.AcquiredItems is { Count: > 0 }) ||
                         (ledgerDelta.ConsumedItems is { Count: > 0 }) ||
                         (ledgerDelta.LostItems is { Count: > 0 }));
                    if (hasPhysicalChange && characterL != null)
                    {
                        var backpack = await inventoryService.GetBackpackAsync(new SessionIdInput { SessionId = input.SessionId });
                        await hubContext.Clients.Client(connectionId).UpdateInventory(new InventoryUpdateDto
                        {
                            Items = backpack.Items.Select(i => new InventoryItemDto
                            {
                                Id = i.Id, ItemName = i.ItemName, ItemType = i.ItemType,
                                Description = i.Description, Weight = i.Weight,
                                AttributeBonus = i.AttributeBonus, LinkedAttribute = i.LinkedAttribute,
                                MaxUses = i.MaxUses, CurrentUses = i.CurrentUses,
                                IsUnlimited = i.IsUnlimited, IsEquipped = i.IsEquipped,
                                IsKeyItem = i.IsKeyItem, Quantity = i.Quantity
                            }).ToList(),
                            CurrentWeight = backpack.CurrentWeight,
                            MaxWeight = backpack.MaxWeight,
                            WeightPercent = backpack.WeightPercent,
                            IsOverloaded = backpack.IsOverloaded,
                            IsEncumbered = backpack.IsEncumbered,
                            EquippedWeaponId = characterL.EquippedWeaponId,
                            EquippedArmorId = characterL.EquippedArmorId
                        });
                    }

                    // 记账产生无形情报变更 → 推送已知情报更新
                    var hasInfoChange = ledgerDelta != null &&
                        ((ledgerDelta.AcquiredInfo is { Count: > 0 }) || (ledgerDelta.InvalidatedInfo is { Count: > 0 }));
                    if (hasInfoChange)
                    {
                        await PushKnownAssetsAsync(hubContext, knownAssetService, connectionId, input.SessionId);
                    }

                    // 记账完成后启动下一轮预计算（保证其分类AI读到本轮最新账本）
                    if (result.SuggestedActions != null && result.SuggestedActions.Count >= 2)
                    {
                        var nextOptions = result.SuggestedActions!.Take(2).ToList();
                        var isAdultRound = result.NarrativeInput?.IsAdult ?? false;

                        // 持久化选项文本到 session.LastSuggestedActions（与 PlayerAction 路径一致）：
                        // 副本挂起/断线恢复时前端可继续显示按钮；缓存命中走秒响应，未命中以文本走常规全链路。
                        // 成人轮跳过持久化，避免断线恢复时显示成人选项文本（保留原门控语义）。
                        if (sessionL != null && !isAdultRound)
                        {
                            try
                            {
                                sessionL.LastSuggestedActions = JsonConvert.SerializeObject(nextOptions);
                                await sessionRep.AsUpdateable(sessionL)
                                    .UpdateColumns(s => new { s.LastSuggestedActions })
                                    .ExecuteCommandAsync();
                            }
                            catch (Exception persistEx)
                            {
                                logger.LogWarning(persistEx, "持久化 LastSuggestedActions 失败(非致命、缓存路径): SessionId={SessionId}", input.SessionId);
                            }
                        }

                        // 预计算继承本轮会话级成人模式：成人轮也用成人模型预掷，从下一次预计算起全链路切换（对齐世界难度override语义）
                        return (Task?)Task.Run(() => _precomputeService.PrecomputeAsync(input.SessionId, nextOptions, input.WorldDifficultyOverride, input.IsAdultMode, input.IsVipMode));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "缓存路径后台记账链执行失败: SessionId={SessionId}", input.SessionId);
                }
                return null;
            });

            // 4. 【L1改造】叙事实时流式生成（原为回放预生成文本），与后台链并行。
            //    章节档与非章节档统一走 StreamNarrativeLiveAsync（内部对章节档走分镜流式续写）。
            //    不可行短路（NarrativeInput=null）时回放拒绝文案，避免界面静默。
            //    【VIP模式】若预计算已预生成叙事文本（cached.NarrativeText 非空）→ 直接文本回放，首字延迟≈0；
            //    未预生成（非VIP、或VIP预生成未就绪/失败）→ 自动降级为实时流式，保证不卡住。
            var chunkType = result.DiceResult != null ? "action_result" : "narrative";
            string narrativeText;
            if (!string.IsNullOrEmpty(cached.NarrativeText))
            {
                narrativeText = cached.NarrativeText;
                await broadcast.StreamNarrativeAsync(userId, narrativeText, chunkType);
            }
            else if (result.NarrativeInput != null)
            {
                narrativeText = await broadcast.StreamNarrativeLiveAsync(
                    userId, narrativeAi, result.NarrativeInput, input.SessionId, chunkType);
            }
            else if (!string.IsNullOrEmpty(result.Narrative))
            {
                narrativeText = result.Narrative;
                await broadcast.StreamNarrativeAsync(userId, narrativeText, chunkType);
            }
            else
            {
                narrativeText = "";
                logger.LogWarning("缓存回放无叙事内容: SessionId={SessionId}, Index={Index}", input.SessionId, input.OptionIndex);
            }

            // 4.9 等待后台链收尾（含 await书记官+落库+记账），取回预计算任务句柄；
            //     须在恢复输入之前完成，避免玩家新行动与本轮记账竞争
            Task? precomputeTask = await ledgerTask;
            ledgerTask = null;

            // 落库后读取会话/角色（供叙事日志与状态推送）
            var session = await sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
            var characterForState = await characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);

            // 记录叙事日志（用落库后的 InteractionCount）
            if (!string.IsNullOrEmpty(narrativeText) && session != null)
            {
                await narrativeLogRep.AsInsertable(new GameNarrativeLog
                {
                    SessionId = input.SessionId,
                    InteractionIndex = session.InteractionCount,
                    PlayerInput = actionText,
                    NarrativeText = narrativeText,
                    Timestamp = DateTime.Now,
                    IsAdult = false
                }).ExecuteCommandAsync();
            }

            // 构建下一轮选项DTO（此时 SuggestedActions 已由后台链 await 书记官回填；成人轮同样产出选项）
            List<SuggestedActionOptionDto>? nextOptionDtos = null;
            if (result.SuggestedActions is { Count: >= 2 })
            {
                nextOptionDtos = result.SuggestedActions!.Take(2).Select((sa, i) => new SuggestedActionOptionDto
                {
                    Index = i,
                    ActionText = sa.ActionText,
                    Hint = sa.Hint
                }).ToList();
            }

            // 5. 推送游戏状态更新
            if (session != null && characterForState != null)
            {
                await hubContext.Clients.Client(connectionId).UpdateGameState(
                    BuildGameState(characterForState, session.CurrentDay, session.CurrentSegment, session.TensionLevel));
            }

            // 7. 推送时段变化（如有）
            if (result.StateChanges?.TimeAdvanced == true && session != null)
            {
                await hubContext.Clients.Client(connectionId).SendTimeTransition(new TimeTransitionDto
                {
                    Narrative = GetTimeTransitionNarrative(session.CurrentSegment),
                    NewSegment = GetSegmentName(session.CurrentSegment),
                    NewDay = session.CurrentDay
                });
            }

            // 8. 推送支线任务更新（如有）
            if (result.StateChanges?.QuestProgress != null && session?.SideQuests != null)
            {
                var unlockedQuestInfos = new List<SideQuestInfoDto>();
                var unlockedNames = result.StateChanges.QuestProgress.UnlockedSideQuests ?? new List<string>();
                if (unlockedNames.Count > 0)
                {
                    try
                    {
                        var allQuests = JsonConvert.DeserializeObject<List<SideQuestData>>(session.SideQuests);
                        if (allQuests != null)
                        {
                            var unlockedSet = new HashSet<string>(unlockedNames);
                            unlockedQuestInfos = allQuests
                                .Where(sq => unlockedSet.Contains(sq.Name))
                                .Select(sq => new SideQuestInfoDto
                                {
                                    Name = sq.Name,
                                    Description = sq.Description,
                                    IsCompleted = (result.StateChanges.QuestProgress.CompletedSideQuests ?? new List<string>()).Contains(sq.Name)
                                }).ToList();
                        }
                    }
                    catch { /* 解析失败时推送空解锁列表 */ }
                }

                await hubContext.Clients.Client(connectionId).UpdateSideQuests(new SideQuestUpdateDto
                {
                    CompletedSideQuests = result.StateChanges.QuestProgress.CompletedSideQuests ?? new List<string>(),
                    UnlockedSideQuests = unlockedQuestInfos
                });
            }

            // 9. 恢复输入
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "input_control",
                Message = "enable",
                Timestamp = DateTime.Now
            });

            // 10. 推送建议行动选项（预计算已在步骤3.5记账链中与叙事流式并行启动）
            if (precomputeTask != null && nextOptionDtos != null)
            {
                // 若预计算已在阅读期间跑完，则按钮直接可点；否则先显示加载态
                var alreadyReady = precomputeTask.IsCompleted;
                _precomputeService.MarkOptionsShown(input.SessionId); // 埋点①：记录选项推送时刻
                await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                {
                    Options = alreadyReady ? ApplyFeasibility(input.SessionId, nextOptionDtos) : nextOptionDtos,
                    IsComputing = !alreadyReady
                });

                // 预计算尚未完成时，等其结束后再通知前端 IsComputing=false（并回填可行性）
                if (!alreadyReady)
                {
                    var pendingTask = precomputeTask;
                    var pendingOptions = nextOptionDtos;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await pendingTask;
                            await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                            {
                                Options = ApplyFeasibility(input.SessionId, pendingOptions),
                                IsComputing = false
                            });
                        }
                        catch { /* 预计算失败，按钮保持禁用，玩家可手动输入 */ }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SelectCachedAction后台处理失败: SessionId={SessionId}", input.SessionId);
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "error",
                Message = "行动处理异常，请稍后重试",
                Timestamp = DateTime.Now
            });
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "input_control",
                Message = "enable",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            // 叙事异常中断时，确保后台记账链在scope销毁前跑完（链内已自行捕获异常）
            if (ledgerTask != null)
            {
                try { await ledgerTask; } catch { /* 已在链内记录 */ }
            }
        }
    }

    /// <summary>
    /// 确认时段推进
    /// </summary>
    public async Task ConfirmTimeAdvance(ConfirmTimeAdvanceInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        try
        {
            if (input.Choice == "rest")
            {
                // 触发长休息
                var timeInfo = await _timeSegmentService.LongRestAsync(new SessionIdInput { SessionId = input.SessionId });
                var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
                var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);

                if (character != null && session != null)
                {
                    await _broadcast.StreamNarrativeAsync(userId.Value,
                        "你找到一处安全的地方休息。疲惫的身体在睡眠中逐渐恢复……", "scene_transition");

                    await Clients.Caller.SendTimeTransition(new TimeTransitionDto
                    {
                        Narrative = "───── 夜幕降临，你沉入了梦乡 ─────",
                        NewSegment = timeInfo.SegmentName,
                        NewDay = timeInfo.Day
                    });

                    await Clients.Caller.UpdateGameState(BuildGameState(character, timeInfo.Day, timeInfo.Segment, session.TensionLevel));
                }
            }
            else
            {
                // 推进时段
                var timeInfo = await _timeSegmentService.AdvanceTimeAsync(new SessionIdInput { SessionId = input.SessionId });
                var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
                var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);

                if (character != null && session != null)
                {
                    await Clients.Caller.SendTimeTransition(new TimeTransitionDto
                    {
                        Narrative = GetTimeTransitionNarrative(timeInfo.Segment),
                        NewSegment = timeInfo.SegmentName,
                        NewDay = timeInfo.Day
                    });

                    await Clients.Caller.UpdateGameState(BuildGameState(character, timeInfo.Day, timeInfo.Segment, session.TensionLevel));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmTimeAdvance失败: SessionId={SessionId}", input.SessionId);
            await SendError("时段推进异常");
        }
    }

    /// <summary>
    /// 加班选择
    /// </summary>
    public async Task ChooseOvertime(ChooseOvertimeInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        try
        {
            if (input.Choice == "rest")
            {
                // 触发长休息
                var timeInfo = await _timeSegmentService.LongRestAsync(new SessionIdInput { SessionId = input.SessionId });
                var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
                var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);

                if (character != null && session != null)
                {
                    await _broadcast.StreamNarrativeAsync(userId.Value,
                        "你决定不再熬夜，找到休息的地方让身体恢复。", "scene_transition");

                    await Clients.Caller.UpdateGameState(BuildGameState(character, timeInfo.Day, timeInfo.Segment, session.TensionLevel));
                }
            }
            else
            {
                // 标记加班继续
                await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "warning",
                    Message = "你选择继续行动，疲劳在积累...",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChooseOvertime失败: SessionId={SessionId}", input.SessionId);
            await SendError("加班选择处理异常");
        }
    }

    /// <summary>
    /// 确认高风险行动（异步返回，确认执行后走PlayerAction的fire-and-forget流程）
    /// </summary>
    public async Task ConfirmDangerousAction(ConfirmDangerousActionInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        if (input.Confirmed)
        {
            // 立即推送进度
            await Clients.Caller.DungeonGenerating(new GeneratingProgressDto
            {
                Phase = "世界推演中",
                ProgressPercent = 50,
                Message = "正在处理危险行动..."
            });

            await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "input_control",
                Message = "disable",
                Timestamp = DateTime.Now
            });

            // fire-and-forget：后台执行PlayerAction流程
            var connectionId = Context.ConnectionId;
            _ = ProcessPlayerActionAsync(userId.Value, connectionId, new PlayerActionInput
            {
                SessionId = input.SessionId,
                ActionText = $"[已确认危险行动:{input.ActionId}]"
            });
        }
        else
        {
            // 取消行动，等待新输入
            await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "info",
                Message = "你取消了这个行动，请选择其他方式。",
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 放弃副本
    /// </summary>
    public async Task AbandonSession(long sessionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        try
        {
            var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
            if (session == null) return;

            // 标记会话已放弃；同时清空 LastSuggestedActions（已结束不再恢复，避免僵尸数据）
            session.Status = 2; // 已放弃
            session.EndTime = DateTime.Now;
            session.LastSuggestedActions = null;
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.Status, s.EndTime, s.LastSuggestedActions })
                .ExecuteCommandAsync();

            // 清除预计算内存缓存（避免僵尸选项被其他路径误命中）
            _precomputeService.InvalidateCache(sessionId);

            // 移除活跃会话
            _sessionManager.RemoveActiveSession(userId.Value);

            // 评分 + 结算叙事 + 奖励同步到Meta层
            string? scoreLevel = null;
            string? exitNarrative = null;
            string? epilogue = null;
            string? comment = null;
            try
            {
                var scoreResult = await _scoringService.CalculateScoreAsync(new SessionIdInput { SessionId = sessionId });
                scoreLevel = scoreResult.ScoreLevel;

                var settlement = await _settlementNarrative.GenerateSettlementAsync(sessionId);
                exitNarrative = settlement.ExitNarrative;
                epilogue = settlement.Epilogue;
                comment = settlement.Comment;

                // 结算后同步奖励到Meta层（经验/等级/天赋点/技能回写，幂等）
                await _metaProgression.SyncDungeonResultAsync(new SyncDungeonResultInput
                {
                    UserId = userId.Value,
                    SessionId = sessionId
                });
            }
            catch (Exception scoreEx)
            {
                _logger.LogWarning(scoreEx, "AbandonSession评分/结算/奖励同步失败(非致命): SessionId={SessionId}", sessionId);
            }

            // 清理叙事日志（副本已结束，无需保留）
            // 注：GenerateSettlementAsync内部已清理，此处作为降级兜底
            await _narrativeLogRep.AsDeleteable()
                .Where(l => l.SessionId == sessionId)
                .ExecuteCommandAsync();

            // 清理已知情报账本（副本已结束，账本生命周期随会话结束）
            try
            {
                using var cleanScope = _scopeFactory.CreateScope();
                var knownAssetRep = cleanScope.ServiceProvider.GetRequiredService<SqlSugarRepository<GameKnownAsset>>();
                await knownAssetRep.AsDeleteable()
                    .Where(a => a.SessionId == sessionId)
                    .ExecuteCommandAsync();
            }
            catch (Exception cleanEx)
            {
                _logger.LogWarning(cleanEx, "AbandonSession清理已知情报失败(非致命): SessionId={SessionId}", sessionId);
            }

            // 推送会话结束（含评分+结算数据）
            await Clients.Caller.SessionEnded(new SessionEndDto
            {
                SessionId = sessionId,
                EndReason = "abandoned",
                ScoreLevel = scoreLevel,
                ExitNarrative = exitNarrative,
                Epilogue = epilogue,
                Comment = comment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AbandonSession失败: SessionId={SessionId}", sessionId);
            await SendError("放弃副本处理异常");
        }
    }

    /// <summary>
    /// 挂起副本（保存进度，下次可继续）
    /// </summary>
    public async Task SuspendSession(long sessionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        try
        {
            var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
            if (session == null) return;

            // 标记会话已挂起（保留所有进度数据）
            session.Status = 4; // 已挂起
            await _sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new { s.Status })
                .ExecuteCommandAsync();

            // 移除活跃会话映射（下次进入时通过checkActiveSession恢复）
            _sessionManager.RemoveActiveSession(userId.Value);

            // 推送挂起确认
            await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "info",
                Message = "副本已挂起，下次可继续游玩",
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuspendSession失败: SessionId={SessionId}", sessionId);
            await SendError("挂起副本处理异常");
        }
    }

    /// <summary>
    /// 重新开始（保留世界/副本不变，清除历史记录，重置角色和会话状态）
    /// </summary>
    public async Task RestartSession(RestartSessionInput input)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return;

        var connectionId = Context.ConnectionId;

        // 1. 立即推送进度
        await Clients.Caller.DungeonGenerating(new GeneratingProgressDto
        {
            Phase = "世界重置中",
            ProgressPercent = 0,
            Message = "正在重置副本历史..."
        });

        // 2. fire-and-forget
        _ = ProcessRestartSessionAsync(userId.Value, connectionId, input.SessionId, input.IsVipMode);
    }

    /// <summary>
    /// 后台处理重新开始：清除历史数据，重置状态，重新生成开场叙事
    /// </summary>
    private async Task ProcessRestartSessionAsync(long userId, string connectionId, long sessionId, bool isVip = false)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var sessionRep = sp.GetRequiredService<SqlSugarRepository<GameDungeonSession>>();
        var characterRep = sp.GetRequiredService<SqlSugarRepository<GameCharacter>>();
        var narrativeLogRep = sp.GetRequiredService<SqlSugarRepository<GameNarrativeLog>>();
        var diceRecordRep = sp.GetRequiredService<SqlSugarRepository<GameDiceRollRecord>>();
        var timeSegmentRep = sp.GetRequiredService<SqlSugarRepository<GameTimeSegment>>();
        var worldStateRep = sp.GetRequiredService<SqlSugarRepository<GameWorldState>>();
        var inventoryRep = sp.GetRequiredService<SqlSugarRepository<GameInventoryItem>>();
        var knownAssetRep = sp.GetRequiredService<SqlSugarRepository<GameKnownAsset>>();
        var npcRep = sp.GetRequiredService<SqlSugarRepository<GameNpcProfile>>();
        var narrativeAi = sp.GetRequiredService<NarrativeAiService>();
        var broadcast = sp.GetRequiredService<HubBroadcastService>();
        var hubContext = sp.GetRequiredService<IHubContext<GameSessionHub, IGameSessionHub>>();
        var logger = sp.GetRequiredService<ILogger<GameSessionHub>>();
        var aiCoordinator = sp.GetRequiredService<AiCoordinatorService>();

        try
        {
            // 1. 获取会话和角色
            var session = await sessionRep.GetFirstAsync(s => s.Id == sessionId && s.UserId == userId);
            if (session == null)
            {
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error", Message = "会话不存在", Timestamp = DateTime.Now
                });
                return;
            }

            var character = await characterRep.GetFirstAsync(c => c.SessionId == sessionId);
            if (character == null)
            {
                await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
                {
                    Type = "error", Message = "角色不存在", Timestamp = DateTime.Now
                });
                return;
            }

            // 2. 软删除历史数据
            var narrativeLogs = await narrativeLogRep.AsQueryable().Where(l => l.SessionId == sessionId).ToListAsync();
            if (narrativeLogs.Count > 0)
            {
                narrativeLogs.ForEach(l => l.IsDelete = true);
                await narrativeLogRep.AsUpdateable(narrativeLogs).UpdateColumns(l => new { l.IsDelete }).ExecuteCommandAsync();
            }

            var diceRecords = await diceRecordRep.AsQueryable().Where(r => r.SessionId == sessionId).ToListAsync();
            if (diceRecords.Count > 0)
            {
                diceRecords.ForEach(r => r.IsDelete = true);
                await diceRecordRep.AsUpdateable(diceRecords).UpdateColumns(r => new { r.IsDelete }).ExecuteCommandAsync();
            }

            var timeSegments = await timeSegmentRep.AsQueryable().Where(t => t.SessionId == sessionId).ToListAsync();
            if (timeSegments.Count > 0)
            {
                timeSegments.ForEach(t => t.IsDelete = true);
                await timeSegmentRep.AsUpdateable(timeSegments).UpdateColumns(t => new { t.IsDelete }).ExecuteCommandAsync();
            }

            var worldStates = await worldStateRep.AsQueryable().Where(w => w.SessionId == sessionId).ToListAsync();
            if (worldStates.Count > 0)
            {
                worldStates.ForEach(w => w.IsDelete = true);
                await worldStateRep.AsUpdateable(worldStates).UpdateColumns(w => new { w.IsDelete }).ExecuteCommandAsync();
            }

            // 3. 删除背包道具
            var items = await inventoryRep.AsQueryable().Where(i => i.CharacterId == character.Id).ToListAsync();
            if (items.Count > 0)
            {
                items.ForEach(i => i.IsDelete = true);
                await inventoryRep.AsUpdateable(items).UpdateColumns(i => new { i.IsDelete }).ExecuteCommandAsync();
            }

            // 3.5 删除已知情报账本（新周目不应残留上周目情报，否则门卫与线索区会读到旧数据）
            var knownAssets = await knownAssetRep.AsQueryable().Where(a => a.SessionId == sessionId).ToListAsync();
            if (knownAssets.Count > 0)
            {
                knownAssets.ForEach(a => a.IsDelete = true);
                await knownAssetRep.AsUpdateable(knownAssets).UpdateColumns(a => new { a.IsDelete }).ExecuteCommandAsync();
            }

            // 4. 重置NPC状态（态度恢复初始，清空交互历史）
            var npcs = await npcRep.AsQueryable().Where(n => n.SessionId == sessionId).ToListAsync();
            foreach (var npc in npcs)
            {
                npc.CurrentAttitude = npc.InitialAttitude;
                npc.InteractionHistory = null;
                npc.IsAlive = true;
            }
            if (npcs.Count > 0)
            {
                await npcRep.AsUpdateable(npcs)
                    .UpdateColumns(n => new { n.CurrentAttitude, n.InteractionHistory, n.IsAlive })
                    .ExecuteCommandAsync();
            }

            // 5. 重置角色状态（满血，清除异常状态）
            character.CurrentHp = character.MaxHp;
            character.IsInCombat = false;
            character.IsFatigued = false;
            character.IsWounded = false;
            character.IsDying = false;
            character.WoundCount = 0;
            character.CurrentLocation = null;
            character.Level = 1;
            await characterRep.AsUpdateable(character)
                .UpdateColumns(c => new {
                    c.CurrentHp, c.IsInCombat, c.IsFatigued, c.IsWounded,
                    c.IsDying, c.WoundCount, c.CurrentLocation, c.Level
                })
                .ExecuteCommandAsync();

            // 6. 重置会话计数器（保留世界设定/主线/支线/隐藏内容/难度参数）；同时清空 LastSuggestedActions（新周目不应残留旧选项）
            session.CurrentDay = 1;
            session.CurrentSegment = 0;
            session.TensionLevel = 1;
            session.InteractionCount = 0;
            session.OvertimeCount = 0;
            session.Status = 0;
            session.StartTime = DateTime.Now;
            session.EndTime = null;
            session.LastSuggestedActions = null;
            await sessionRep.AsUpdateable(session)
                .UpdateColumns(s => new {
                    s.CurrentDay, s.CurrentSegment, s.TensionLevel,
                    s.InteractionCount, s.OvertimeCount, s.Status, s.StartTime, s.EndTime,
                    s.LastSuggestedActions
                })
                .ExecuteCommandAsync();

            // 6.5 清除预计算内存缓存（新周目旧缓存已失效）
            _precomputeService.InvalidateCache(sessionId);

            // 7. 重新初始化世界状态（使用结构化局面快照）
            Dictionary<string, object>? worldSettingDict = null;
            if (!string.IsNullOrEmpty(session.WorldSetting))
            {
                try { worldSettingDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(session.WorldSetting); }
                catch { }
            }
            var restartSnapshot = new SituationSnapshotDto
            {
                WorldSetting = worldSettingDict ?? new(),
                Location = "",
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
            await worldStateRep.AsInsertable(new GameWorldState
            {
                SessionId = sessionId,
                StateJson = Newtonsoft.Json.JsonConvert.SerializeObject(restartSnapshot),
                SnapshotType = "current",
                InteractionIndex = 0
            }).ExecuteCommandAsync();

            // 8. 生成新的开场叙事（真流式：AI生成token实时推送到客户端）
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
                CharacterName = character.Name,
                WorldContext = AiCoordinatorService.BuildNarrativeWorldContext(session, null),
                StyleBible = BuildStyleBibleTextFromSession(session),
                MotifTracker = BuildMotifTrackerTextFromSession(session)
            };

            // 9. 先推送场景转换分割线，再流式推送叙事文本
            await hubContext.Clients.Client(connectionId).ReceiveNarrative(new NarrativeChunkDto
            {
                Text = "",
                ChunkType = "scene_transition",
                IsLast = true,
                Timestamp = DateTime.Now
            });
            var openingNarrative = await broadcast.StreamNarrativeLiveAsync(
                userId, narrativeAi, openingInput, sessionId, "narrative");

            // 记录开场叙事
            await narrativeLogRep.AsInsertable(new GameNarrativeLog
            {
                SessionId = sessionId,
                InteractionIndex = 0,
                PlayerInput = "[副本重新开始]",
                NarrativeText = openingNarrative,
                Timestamp = DateTime.Now
            }).ExecuteCommandAsync();

            // 10. 构建并推送游戏状态
            var gameState = BuildGameState(character, 1, 0, 1);

            // 11. 推送副本就绪通知（复用 DungeonReady）
            var template = await sp.GetRequiredService<SqlSugarRepository<GameDungeonTemplate>>()
                .GetFirstAsync(t => t.Id == session.TemplateId);

            // 解析世界设定JSON，构建WorldInfo
            var worldBackground = "";
            var keyLocations = new List<string>();
            if (!string.IsNullOrEmpty(session.WorldSetting))
            {
                try
                {
                    var ws = Newtonsoft.Json.Linq.JObject.Parse(session.WorldSetting);
                    var parts = new List<string>();
                    if (ws["era"]?.ToString() is string era && !string.IsNullOrEmpty(era))
                        parts.Add($"时代: {era}");
                    if (ws["technology_level"]?.ToString() is string tech && !string.IsNullOrEmpty(tech))
                        parts.Add($"科技水平: {tech}");
                    if (ws["culture"]?.ToString() is string culture && !string.IsNullOrEmpty(culture))
                        parts.Add($"文化: {culture}");
                    if (ws["geography"]?.ToString() is string geo && !string.IsNullOrEmpty(geo))
                        parts.Add($"地理: {geo}");
                    worldBackground = string.Join("\n", parts);

                    var locations = ws["key_locations"] as Newtonsoft.Json.Linq.JArray;
                    if (locations != null)
                    {
                        foreach (var loc in locations)
                        {
                            var name = loc["name"]?.ToString() ?? "";
                            var desc = loc["description"]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(name))
                                keyLocations.Add($"{name}: {desc}");
                        }
                    }
                }
                catch { /* 解析失败则留空 */ }
            }

            // 解析主线任务JSON
            var mainQuestObjective = "";
            var mainQuestNodes = new List<string>();
            if (!string.IsNullOrEmpty(session.MainQuest))
            {
                try
                {
                    var mq = Newtonsoft.Json.Linq.JObject.Parse(session.MainQuest);
                    mainQuestObjective = mq["objective"]?.ToString() ?? "";
                    var nodes = mq["key_nodes"] as Newtonsoft.Json.Linq.JArray;
                    if (nodes != null)
                        mainQuestNodes = nodes.Select(n => n.ToString()).ToList();
                }
                catch { /* 解析失败则留空 */ }
            }

            // 解析支线任务JSON
            var sideQuests = new List<SideQuestInfoDto>();
            if (!string.IsNullOrEmpty(session.SideQuests))
            {
                try
                {
                    var sqList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SideQuestData>>(session.SideQuests);
                    if (sqList != null)
                    {
                        sideQuests = sqList.Select(sq => new SideQuestInfoDto
                        {
                            Name = sq.Name ?? "",
                            Description = sq.Description ?? "",
                            IsCompleted = false
                        }).ToList();
                    }
                }
                catch { /* 解析失败则留空 */ }
            }

            await hubContext.Clients.Client(connectionId).DungeonReady(new DungeonReadyDto
            {
                SessionId = sessionId,
                DungeonName = template?.Name ?? "未知副本",
                WorldInfo = new WorldInfoDto
                {
                    DungeonName = template?.Name ?? "未知副本",
                    WorldBackground = worldBackground,
                    MainQuestObjective = mainQuestObjective,
                    MainQuestNodes = mainQuestNodes,
                    KeyLocations = keyLocations,
                    SideQuests = sideQuests
                },
                GameState = gameState,
                // 重新开始时目标依然保留（玩家可能想继续之前的意图）
                CurrentPlayerGoal = session?.CurrentPlayerGoal ?? ""
            });

            // ★ 重新开始：生成开场行动选项（方案C-1，复用书记官轻量模式，与首次进入一致）
            //    保留世界/副本不变，开场叙事作为[本轮既定事实]产出2个引导选项（新手引导 + UX 一致性）。
            try
            {
                var openingOptions = await aiCoordinator.GenerateSuggestionsOnlyAsync(
                    sessionId, "[副本重新开始]", openingNarrative, character.Name);
                if (openingOptions != null && openingOptions.Count >= 2)
                {
                    var optionsToCompute = openingOptions.Take(2).ToList();

                    // 持久化选项文本（挂起/断线恢复时可继续显示按钮）
                    session.LastSuggestedActions = JsonConvert.SerializeObject(optionsToCompute);
                    await sessionRep.AsUpdateable(session)
                        .UpdateColumns(s => new { s.LastSuggestedActions })
                        .ExecuteCommandAsync();

                    // 启动预计算（玩家点选时秒响应）
                    var precomputeTask = _precomputeService.PrecomputeAsync(sessionId, optionsToCompute, null, false, isVip);
                    var optionDtos = optionsToCompute.Select((sa, i) => new SuggestedActionOptionDto
                    {
                        Index = i,
                        ActionText = sa.ActionText,
                        Hint = sa.Hint
                    }).ToList();

                    var alreadyReady = precomputeTask.IsCompleted;
                    _precomputeService.MarkOptionsShown(sessionId);
                    await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                    {
                        Options = alreadyReady ? ApplyFeasibility(sessionId, optionDtos) : optionDtos,
                        IsComputing = !alreadyReady
                    });

                    if (!alreadyReady)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await precomputeTask;
                                await hubContext.Clients.Client(connectionId).ReceiveSuggestedActions(new SuggestedActionsDto
                                {
                                    Options = ApplyFeasibility(sessionId, optionDtos),
                                    IsComputing = false
                                });
                            }
                            catch { /* 预计算失败，按钮保持禁用，玩家可手动输入 */ }
                        });
                    }
                }
            }
            catch (Exception openingEx)
            {
                logger.LogWarning(openingEx, "重新开始开场选项生成失败(非致命): SessionId={SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RestartSession后台处理失败: SessionId={SessionId}", sessionId);
            await hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
            {
                Type = "error",
                Message = "重置副本异常，请稍后重试",
                Timestamp = DateTime.Now
            });
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 获取当前连接的UserId
    /// </summary>
    private long? GetCurrentUserId()
    {
        return _sessionManager.GetUserId(Context.ConnectionId);
    }

    /// <summary>
    /// 推送错误消息
    /// </summary>
    private async Task SendError(string message)
    {
        await Clients.Caller.ReceiveSystemMessage(new SystemMessageDto
        {
            Type = "error",
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 构建游戏状态DTO
    /// </summary>
    private static GameStateDto BuildGameState(GameCharacter character, int currentDay, int currentSegment, int tensionLevel)
    {
        var hpPercent = character.MaxHp > 0 ? (int)(character.CurrentHp * 100.0 / character.MaxHp) : 100;
        var status = hpPercent switch
        {
            > 75 => "正常",
            > 50 => "轻伤",
            > 25 => "重伤",
            _ => "濒死"
        };

        return new GameStateDto
        {
            CurrentHp = character.CurrentHp,
            MaxHp = character.MaxHp,
            HpPercent = hpPercent,
            Status = status,
            CurrentDay = currentDay,
            CurrentSegment = GetSegmentName(currentSegment),
            TensionLevel = tensionLevel,
            IsFatigued = character.IsFatigued,
            IsInCombat = character.IsInCombat
        };
    }

    /// <summary>
    /// 获取时段中文名
    /// </summary>
    private static string GetSegmentName(int segment)
    {
        return segment switch
        {
            0 => "上午",
            1 => "下午",
            2 => "傍晚",
            3 => "夜间",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取时段过渡叙事文本
    /// </summary>
    private static string GetTimeTransitionNarrative(int newSegment)
    {
        return newSegment switch
        {
            0 => "───── 晨光熹微，新的一天开始了 ─────",
            1 => "───── 日头渐高，午后的光线变得慵懒 ─────",
            2 => "───── 天色渐暗，城市亮起灯火 ─────",
            3 => "───── 夜幕降临，黑暗吞噬了一切 ─────",
            _ => "───── 时光流转 ─────"
        };
    }

    private static readonly Newtonsoft.Json.JsonSerializerSettings _hubJsonSettings = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
        }
    };

    /// <summary>
    /// 从会话的文风圣经JSON构建文本（与AiCoordinatorService.BuildStyleBibleText逻辑一致）
    /// </summary>
    private static string BuildStyleBibleTextFromSession(GameDungeonSession session)
    {
        if (string.IsNullOrEmpty(session.StyleBibleJson)) return "";
        try
        {
            var sb = Newtonsoft.Json.JsonConvert.DeserializeObject<StyleBibleData>(session.StyleBibleJson, _hubJsonSettings);
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
    /// 从会话的意象JSON构建文本（与AiCoordinatorService.BuildMotifTrackerText逻辑一致）
    /// </summary>
    private static string BuildMotifTrackerTextFromSession(GameDungeonSession session)
    {
        if (string.IsNullOrEmpty(session.MotifsJson)) return "";
        try
        {
            var motifs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MotifData>>(session.MotifsJson, _hubJsonSettings);
            if (motifs == null || motifs.Count == 0) return "";
            var str = new System.Text.StringBuilder();
            str.AppendLine("贯穿意象（鼓励在叙事中使用，每次赋予新含义）:");
            foreach (var m in motifs)
            {
                str.AppendLine($"  - 「{m.Name}」初始: {m.InitialState}");
                if (!string.IsNullOrEmpty(m.EvolutionHint))
                    str.AppendLine($"    进化方向: {m.EvolutionHint}");
            }
            return str.ToString().TrimEnd();
        }
        catch { return ""; }
    }

    #endregion
}
