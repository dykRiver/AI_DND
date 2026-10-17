# 预计算链路延迟优化 · 分阶段实施方案（设计稿 v0.1）

> 状态：**待用户拍板**。本文档为决策完备的实施设计，实施者据此无需再做架构决策。
> 目标读者：本项目开发者 / design-driven-dev 执行代理。
> 涉及契约文档：`ai-architecture.md`、`player-experience.md`（落地后需同步，见 §6）。

---

## 0. 背景与目标

### 0.1 痛点
`micro`/`normal` 短叙事轮（200–1200 字，玩家 ~25–50s 读完），点选 A 后**下一轮 B 的选项迟迟不可点**，玩家干等。根因是 B 就绪链是一条硬串行链。

### 0.2 B 就绪链现状（点选轮 `ProcessSelectCachedActionAsync` 的 ledgerTask，GameSessionHub.cs L1201–1306）
```
点选 A1
 → await ScribeTask（书记官，通常已完成，≈0）              L1205-1206
 → ApplyCachedActionResultAsync（世界落库 + 末尾辅导员评估 0-20s） L1208 / AiCoordinator L670-770
 → PushCoachLifecycleAsync（推送，快）                      L1215
 → 物资官 RecordFromBlueprintAsync（LLM low 5-15s，if ItemHints） L1217-1234
 → 背包/情报推送（快）                                       L1236-1271
 → 持久化 LastSuggestedActions（快）                         L1274-1295
 → B 预计算 PrecomputeAsync                                 L1298
     内部：分类(low ~10s) → 掷骰 → 导演(low, micro/normal ~15-30s) → 书记官(fire-and-forget)
```
**B 就绪典型 ~30–75s（micro/normal）**，盖不住 ~25s 的短叙事阅读窗口。

### 0.3 目标（量化成功标准）
| 指标 | 现状 | 目标 |
|---|---|---|
| B 就绪典型耗时（micro/normal） | ~30–75s | **≤ 25–40s** |
| 点选后物资官 LLM 调用 | 1 次（5-15s 串行） | **0 次**（用预暂存 delta） |
| 辅导员评估是否阻塞 B 导演 | 是（0-20s 串行） | **否**（与 B 导演并行） |
| 按钮禁用干等 | 有（IsComputing 禁用态） | **无**（乐观可点 + 点击接管） |
| 分类 AI 调用（激进版后） | 每轮 2 次（VIP 双选项） | **0 次**（书记官附带分类） |

---

## 1. 关键约束与已验证事实（代码实证，勿推翻）

1. **降档无效**：`ReasoningEffort` 仅对 DashScope deepseek-v4 思考模式生效；Classifier/Director/Scribe/Quartermaster/Narrative **已全部为 `low`**（最低档），Coach 为 `high`（GameAiOptions.json L8-55）。→ "分类/导演降档提速"路线作废，砍分类耗时只能靠**第二阶段删调用**。
2. **AI 主链硬串行**：分类AI（读背包/情报）→ 掷骰（依赖分类 Judgment）→ 导演（依赖分类 ActionIntent + 掷骰结果 + 背包）。见 AiCoordinatorService.cs L113-300。**B 导演无法脱离 B 分类单独提前**。
3. **物资官账本基准一致性**：预计算 A1 时的账本 = 点选 A1 记账的基准账本（均为 L(N-1)，中间玩家只看选项不动账本）。→ **预暂存 delta 点选时可直接落库**。
4. **物资官记账本已近似两段**：`RecordFromBlueprintAsync` = LLM 产 delta（L66-120，输入仅 itemHints+currentLedger+blueprint，**不依赖已落库背包**）+ `ApplyDeltaAsync` 落库（L122，private L200）。且 `BuildFallbackDelta`（L136-195）证明 delta 骨架可**纯规则构造**。
5. **辅导员产出只喂 B 书记官**：辅导员评估落库 `session.CoachState`；B 预计算的书记官在 `RunScribeTaskAsync` 的 DryRun 分支复用 `prevCoachState`（AiCoordinator L1137-1143），来源是 `ProcessPlayerActionAsync` L435 传入的 `session.CoachState`。**B 导演（L300 之前）不读 CoachState**。→ 辅导员可与 B 导演并行，仅需在 L435 前落库。
6. **预计算"导演层就绪"不含书记官**：`PrecomputeAsync` 阶段1 `Task.WhenAll(PrecomputeDirectorAsync)` 完成即 `IsReady=true` 放开按钮（ActionPrecomputeService L114-154）；书记官 fire-and-forget。阶段2（L165-191）目前仅 VIP 叙事预生成。
7. **AIEval 只测单 AI 质量、不测端到端性能**：`EvalHost` 装配 Classifier/Director/Narrative/Quartermaster（SQLite 空库），**不含 Coordinator/Precompute/Coach**。→ 性能验证靠日志埋点，质量验证靠 AIEval。
8. **缓存对象引用在 InvalidateCache 后仍有效**：点选轮 L1124 取 `cached` 局部引用，L1195 `InvalidateCache` 只从字典移除，`cached.PreStagedDelta`/`cached.StreamBuffer` 仍可读（路径B 已依赖此语义）。

---

## 2. 第一阶段：低风险三件套

> 原则：**不碰 AI 输出质量，纯挪时间 / 改时机**。三件独立提交、独立验证（见 §5）。

### 2.1 ② 物资官预暂存（把点选后 5-15s LLM 平移到预计算后台）

**落点与改动：**

1. `QuartermasterAiService.cs` —— 拆分记账为两个 public 方法：
   - 新增 `public async Task<LedgerDelta?> ProduceDeltaAsync(string playerAction, string itemHints, string currentLedger, List<ItemHintInfo>? blueprint)`：即现 L66-120 的"LLM 产 delta + 失败按 blueprint 规则化保底"，**不落库**，返回 delta。
   - 将 `ApplyDeltaAsync`（现 private L200）改为 `public`。
   - `RecordFromBlueprintAsync` 保留，重构为 `ProduceDeltaAsync` + `ApplyDeltaAsync` 的组合（向后兼容常规轮/自由输入/未命中回退路径）。
2. `ActionPrecomputeService.cs`：
   - `PrecomputedActionCache` 加字段 `public LedgerDelta? PreStagedDelta { get; set; }`。
   - 阶段2（L165-191）**扩展为所有模式都跑物资官预暂存**（当前仅 VIP 叙事）：导演层就绪后，对每个 `Result?.ScribeTask` 存在的选项 detach 一个后台任务：`await ScribeTask` 取 `ItemHints` → 非空则 `ProduceDeltaAsync`（账本用预计算时刻的 `BuildLedgerText`）→ 写回 `Options[idx].PreStagedDelta`。VIP 叙事预生成保持不变，二者并列 detach。
   - **注意**：预暂存必须在阶段2（`IsReady=true` 之后），不得拖慢按钮放开。
3. `GameSessionHub.cs` 点选轮 ledgerTask（L1217-1234）与常规轮（L597-614）：
   - 优先用缓存：`if (cached?.PreStagedDelta is { HasAnyChange: true } d) ledgerDelta = d; await quartermaster.ApplyDeltaAsync(sessionId, d, round);`
   - 否则回退现状 `RecordFromBlueprintAsync`（自由输入/未命中/预暂存未就绪）。
   - `ledgerDelta` 用于后续背包/情报推送判断（L1237/L1266），逻辑不变。

**成功标准**：点选命中路径物资官 LLM 调用 = 0；预暂存 delta 与实时记账 delta 一致率 ≥ 95%（AIEval QuartermasterSuite 验证，见 §4）。
**风险与回退**：预暂存未就绪（玩家秒点）→ 自动回退 `RecordFromBlueprintAsync`，零功能损失。账本漂移（预计算到点选间账本被其他操作改动）→ 极低概率；回退路径兜底。

### 2.2 乐观显示 + 点击接管（消除按钮干等）

**落点与改动：**

1. `GameSessionHub.cs` 四处推送点（L347-367、L798-818、L1444-1466、L2129-2148）：
   - `IsComputing = !alreadyReady` → **`IsComputing = false`**（始终乐观可点）。
   - 保留"后台等预计算完成后回填 `ApplyFeasibility`"的 Task（把不可行选项补置灰），但**不再用于解禁**（本就可点）。
2. `ActionPrecomputeService.cs`：
   - `SessionActionCache` 加字段 `public Task? PrecomputeTask { get; set; }`（预计算总句柄）。
   - `PrecomputeAsync` 启动时把自身 Task 存入（或由 Hub 推送时存入 `GetCachedOptions(sessionId).PrecomputeTask`）。
   - 新增 `public async Task<bool> AwaitPrecomputeAsync(long sessionId, int timeoutMs)`：缓存存在且 `IsReady=false` 时 await `PrecomputeTask`（带超时），返回是否就绪。
3. `GameSessionHub.cs` `ProcessSelectCachedActionAsync`（L1124-1169）——**接管**：
   - `GetCachedResult` 未命中时，先判断缓存是否"进行中"（`GetCachedOptions` 非空且 `IsReady=false`）→ 若是，`await AwaitPrecomputeAsync`（超时如 60s）→ 再 `GetCachedResult`；命中则走正常回放，仍未命中才回退常规全链路（L1167）。
   - 目的：点击未就绪选项时**接管正在跑的预计算**，而非另起一次实时全链路（避免重复计算）。
4. 前端 `client/src/components/SuggestedActions.vue`（L19-21、L45-57）：
   - `isDisabled` 去掉 `isComputing.value ||`，仅保留 `option.isFeasible === false`。
   - `isComputing` 相关的 `cursor-wait`/置灰样式改为"可选但轻量 loading 提示"（点击后由叙事流接管反馈）。
   - `handleClick` 逻辑不变（仍调 SelectCachedAction）。

**成功标准**：选项推送后按钮立即可点（无禁用态）；点击未就绪选项走接管（复用同一预计算），日志中不出现"点击触发的第二次完整 AI 链"。
**风险与回退**：玩家点了实际不可行选项 → 预计算完成后 `ApplyFeasibility` 已补置灰；若点在置灰前 → 走现有 infeasible 拒绝叙事（本就有）。接管超时 → 回退常规全链路。

### 2.3 ③ 辅导员并行（把 0-20s 移出 B 导演的串行路径）

> 本项为**中风险**（跨方法签名传递），单独提交、单独验证。

**落点与改动：**

1. `AiCoordinatorService.cs` `ApplyCachedActionResultAsync`（L670-770）拆分：
   - 核心落库段（L672-736：世界状态/NPC态度/计数/时段）保持 `await`。
   - 辅导员评估段（L738-769）提取为 `private Task<CoachState?> EvalCoachDetachedAsync(...)`，**fire-and-forget 启动、返回 Task 句柄**（内部仍 20s 超时 + try-catch 落库 CoachState）。
   - `ApplyCachedActionResultAsync` 返回类型改为 `Task<Task<CoachState?>>`：await 核心落库后，返回辅导员 Task 句柄（供调用方传给 B 预计算）。
2. `GameSessionHub.cs` 点选轮 ledgerTask（L1208）：
   - `var coachTask = await aiCoordinator.ApplyCachedActionResultAsync(...);`（拿到辅导员句柄，核心落库已完成）。
   - B 预计算启动（L1298）把 `coachTask` 传入：`PrecomputeAsync(sessionId, nextOptions, ..., coachTask)`。
3. `ActionPrecomputeService.cs`：`PrecomputeAsync` / `PrecomputeDirectorAsync` 增加可选参数 `Task<CoachState?>? coachTask = null`，透传到 `ProcessActionInput`。
4. `AiCoordinatorService.cs` `ProcessPlayerActionAsync`：
   - `ProcessActionInput` 加字段 `public Task<CoachState?>? CoachTask { get; set; }`。
   - 在 L435（`RunScribeTaskAsync(..., session.CoachState)`）**之前**插入：`if (input.CoachTask != null) { try { await input.CoachTask; } catch {} session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId) ?? session; }`（await 辅导员落库后**重读 session**，确保 L435 拿到新 CoachState）。
   - 效果：分类→掷骰→导演（L124-300）与辅导员并行；仅书记官启动前 await 辅导员。

**成功标准**：B 导演层就绪时刻不再包含辅导员评估耗时；B 书记官读到的 `CoachState.next_focus` 与串行版本一致（日志比对）。
**风险与回退**：`coachTask` 为 null（常规轮/自由输入/未命中）时 L435 行为完全不变（零回归）。重读 session 增加一次 DB 读（毫秒级）。若并行引入时序 bug → 回退为 `await coachTask` 后再启动 B 预计算（即退回串行，仅保留②和乐观显示）。

---

## 3. 第二阶段：激进书记官删分类

> 前置条件：第一阶段三件套已上线并验证。**动手改代码前先做 §3.3 的 DC 前置验证**，达标才继续。

### 3.1 依据
- B 选项由书记官生成，书记官输入（ScribeInput）比分类AI 更全（含 DirectorFacts/PlayerGoal/CoachFocus/支线/隐藏）。
- 分类AI 的 `feasibility` 三态门卫是防"玩家凭空引用账本没有的东西"；**书记官产出的选项天然 feasible**（它不会建议用不存在的道具）→ 对预计算选项，feasibility 判定冗余。
- 降档无路（§1.1），删分类调用是砍掉那 ~10s 的唯一手段。

### 3.2 改动
1. `SuggestedActionInfo`（DirectorOutput.cs L129-146）加字段：
   - `intent`（string?）、`judgment`（JudgmentInfo?：needed/skill/dc/advantage/disadvantage/context）、`feasibility`（string，默认 feasible）、`is_adult`（bool）。
2. `scribe_system.txt`（及成人版 `scribe_adult_*`）提示词：产出 `suggested_actions` 时，为每个选项附带上述分类字段（复用 classifier_system 的 DC 标尺与技能列表措辞）。
3. `RunScribeTaskAsync`（L1158-1163）回填 Scale 时，保留书记官产出的分类字段。
4. `ProcessPlayerActionAsync`（L110-124）：当 `input` 来自预计算选项且选项自带完整分类字段时，**跳过 `ClassifyAsync`**，用选项字段直接构造 `ClassificationResult`；否则（自由输入/字段缺失）仍走 `ClassifyAsync`。
   - 需要 `ProcessActionInput` 能携带"选项自带分类"（预计算时从 `SuggestedActionInfo` 传入）。

### 3.3 DC 前置验证（改代码前，用 AIEval）
- 新建 `ScribeSuite`：对同一批 case，比较"书记官附带判的 judgment/DC/feasibility" vs "分类AI 判的"。
- **达标线**：书记官 DC 落在合理区间且与分类AI 一致率 ≥ 90%、feasibility 一致率 ≥ 95%、技能匹配率 ≥ 90%。
- 不达标 → 放弃删分类，回退到"保留分类AI 但关 EnableThinking 提速"（同样用 ClassifierSuite 验证质量不降）。

**成功标准**：预计算选项路径分类AI 调用 = 0；AIEval 判定质量达标线通过；叙事/判定回归无异常。
**风险与回退**：DC 质量下降 → §3.3 前置拦截；上线后发现个别 case 判定异常 → 保留 `ClassifyAsync` 回退开关（配置项）。

---

## 4. 第三阶段：自动化测试验证

### 4.1 AI 质量（AIEval，载体已存在）
| Suite | 验证内容 | 阶段 |
|---|---|---|
| `ClassifierSuite`（已有） | 分类质量基线回归（可行性三态/DC区间/技能匹配） | 全程 |
| `QuartermasterSuite`（已有，需扩展） | **预暂存 delta vs 实时记账 delta 一致性** | ① |
| `ScribeSuite`（**新建**） | 书记官附带 DC/judgment/feasibility vs 分类AI | ② |
| `DirectorSuite`（已有） | 导演输出回归 | 全程 |
- 用 `BaselineComparer` 对比改造前后；`ReportGenerator` 出报告。
- 运行：`dotnet run --project DHY.Game.AIEval`（`EvalHost.ResolveConfigPath` 自动定位 GameAiOptions.json）。

### 4.2 性能（日志埋点，AIEval 测不了端到端）
- 现有埋点：`MarkOptionsShown`/`GetOptionsShownAt`（玩家思考间隔，L1119-1122）。
- **新增埋点**（AiDebugLogger / ILogger）：
  - B 预计算各阶段耗时：分类、掷骰、导演、（并行）辅导员。
  - 点选 → B 就绪的墙钟耗时。
  - 物资官：预暂存命中率、命中时点选后落库耗时（应 <1s）。
- 采集真实会话日志，统计 micro/normal 轮 B 就绪分布，对照 §0.3 目标。

### 4.3 功能回归
- 编译：`dotnet build DHY.Game.Hub`（传递编译 DHY.Game.AI）EXITCODE=0、0 error、新增 0 warning。
- 前端：`vue-tsc --noEmit` 无类型错误。
- 端到端静态走查：记账正确性、辅导员 next_focus 注入 B 书记官、叙事连贯性、乐观显示点击接管。

---

## 5. 分阶段提交顺序与回归门

```
提交1  ② 物资官预暂存        → 门：编译0错 + QuartermasterSuite一致率≥95% + 点选物资官LLM=0
提交2  乐观显示 + 点击接管     → 门：编译0错 + vue-tsc0错 + 点击未就绪走接管(日志验证)
提交3  ③ 辅导员并行           → 门：编译0错 + B书记官CoachState与串行一致(日志比对)
——— 第一阶段完成，采集性能日志，确认 B 就绪 ≤25-40s ———
提交4  §3.3 DC前置验证(ScribeSuite) → 门：达标线通过才继续
提交5  激进书记官删分类        → 门：编译0错 + ScribeSuite达标 + ClassifierSuite回归不降
——— 第二阶段完成 ———
提交6  性能埋点 + AIEval基线对比报告 → 门：§0.3 全部量化指标达标
```
每个提交独立可回退；③ 与激进版风险最高，各自单独提交。

---

## 6. 契约文档同步清单（落地后）
- `ai-architecture.md`：记账链时序（物资官预暂存、辅导员并行）、预计算阶段2 扩展、分类AI 在预计算选项路径的跳过、版本号 + 版本历史表。
- `player-experience.md`：按钮乐观显示 + 点击接管交互、降级/回退策略、版本号 + 版本历史表。
- 遵循契约型文档规范（semver + 版本历史表，三文档联动）。

---

## 7. 风险登记
| 风险 | 等级 | 缓解 |
|---|---|---|
| ② 预暂存账本漂移 | 低 | 预计算→点选间账本不变；回退 RecordFromBlueprintAsync |
| 乐观显示点了不可行选项 | 低 | ApplyFeasibility 补置灰 + infeasible 拒绝叙事 |
| 接管超时 | 低 | 回退常规全链路 |
| ③ coachTask 时序 bug | 中 | null 时零回归；可回退串行 |
| 激进版 DC 质量下降 | 中 | §3.3 前置验证拦截 + 配置回退开关 |
| 提示词改动影响叙事文风 | 中 | DirectorSuite/NarrativeSuite 回归 + 人工抽检 |

---

## 附：核心文件与行号索引
- `AiCoordinatorService.cs`：ProcessPlayerActionAsync L95-438（分类 L124 / 掷骰 L247 / 导演 L300 / 书记官启动 L435）、ApplyCachedActionResultAsync L670-770（辅导员 L738-769）、RunScribeTaskAsync L1102-1207（DryRun 复用 CoachState L1137-1143）、EvaluateCoachAsync L1215。
- `ActionPrecomputeService.cs`：PrecomputeAsync L91-192（阶段1 L114-163 / 阶段2 L165-191）、PrecomputeDirectorAsync L281-318、PregenerateNarrativeAsync L325-360、缓存类 L13-66。
- `QuartermasterAiService.cs`：RecordFromBlueprintAsync L55-124、BuildFallbackDelta L136-195、ApplyDeltaAsync L200-370。
- `GameSessionHub.cs`：常规轮 ledgerTask L583-693、点选轮 ProcessSelectCachedActionAsync L1077-1306、推送点 L347/L798/L1444/L2129。
- `GameAiOptions.json`：Models L7-103。
- `Dtos`：ScribeOutput.cs、ClassificationResult.cs、DirectorOutput.cs（SuggestedActionInfo L129-146）。
- 前端：`SuggestedActions.vue` L19-57、`types/game.ts` L314-317、`Lobby.vue` L62-78。
- AIEval：`EvalHost.cs`、`ClassifierSuite.cs`、`QuartermasterSuite.cs`、`DirectorSuite.cs`、`BaselineComparer.cs`、`ReportGenerator.cs`。
