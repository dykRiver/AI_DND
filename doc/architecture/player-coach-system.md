# 玩家辅导员（Player Coach）系统设计

> 版本：v1.3.0｜ 状态：阶段 1 核心闭环 + 阶段 2 体验增强 + 触发点迁移（辅导员 Full 评估主落点迁至点选回放轮）已实现（dotnet build / vue-tsc 0 错误）
> 关联文档：[ai-architecture.md](./ai-architecture.md)、[narrative-design.md](./narrative-design.md)、[player-experience.md](./player-experience.md)

## 1. 背景与动机

玩家在自由输入框提交的是**中长期目标（PlayerGoal）**，而非本轮行动。现状（v4.7.0）下目标原文被直接注入书记官提示词，由书记官"读原文猜意图"生成 `suggested_actions`。存在两个结构性缺陷：

1. **模糊消歧缺失**：玩家描述常简短/模糊（如"想办法搞到钱"），书记官职责被锁死为"纯记账、不创作"、且配 `low` 思考档，不适合揣测发散意图，选项难以贴合玩家真实想法。
2. **多步目标无法推进**：`CurrentPlayerGoal` 是扁平一句话、覆盖式存储、无进度记忆。书记官每轮独立地"围绕整句目标产 2 个选项"，轮与轮之间没有"目标推进到哪、下一步该做什么"的概念，多轮后易在原地绕圈。

**结论**：这两个问题都不应由书记官承担。新增专职角色 **玩家辅导员（Player Coach）**，作为"玩家意图的代理人"，负责目标消歧、进度评估、滚动子目标推演与全生命周期管理。

## 2. 角色定位与职责边界

| 角色 | 立场 | 职责 | 明确不做 |
|---|---|---|---|
| 导演（Director） | 世界的代理人 | 客观推演世界反应 | 不为玩家目标扭曲世界 |
| **玩家辅导员（Coach）** | **玩家意图的代理人** | 目标消歧 / 进度评估 / 滚动子目标 / 产出 `next_focus` / 目标状态机 | 不改写剧情、不产叙事、**不直接产 `suggested_actions`** |
| 书记官（Scribe） | 记账员 | 既定事实 → 结构化账目 + 把 `next_focus` 落地成具体选项 | 不做策略判断 |

**分层契约**：辅导员出**策略方向（next_focus）**，书记官出**贴合本轮场景的具体选项**。辅导员**只影响书记官的选项生成，不注入导演链路**（保持世界推演的客观性——已确认边界）。

## 3. 六条设计原则

1. **同源（读同一份世界快照）**：辅导员的进度评估必须与“它将注入 `next_focus` 的那一轮书记官”读到同一份世界状态。因书记官的 `world_state_changes` 只是导演既定事实的结构化编码（“GM 没写的事等于没发生”），故 `本轮开始前快照 S + 导演既定事实 ≡ 落库后 S+Δ`。**主路径（点选回放）**：A 轮世界状态落库后辅导员读 `S+ΔA`，恰与下一轮预计算 B 启动时读到的是同一份 `current` 快照（物资官记账只动背包表、不重写快照）；**回退路径（真实轮）**：辅导员读 `S + 导演既定事实`，与本轮书记官同源、无需等其落库。
2. **滚动子目标 > 线性步骤**：每轮基于最新世界状态重算"当前子目标 + 下一步"，不维护易失效的固定步骤清单，回避 low 档模型"逐步完成判定"不可靠的问题。用"离目标更近了吗 + 眼下最该做什么"替代硬性完成判定。
3. **分层不越权**：辅导员只给 `next_focus`，选项由书记官产出，保持书记官为选项唯一产出者。
4. **独立强模型**：辅导员不在玩家等待的关键路径上 → 可配 `ReasoningEffort=high`，专门保证进度判断与策略推演质量（这是相对"并入书记官"的决定性红利：书记官受记账精确性约束必须低温 low 档，无法兼顾策略发散）。
5. **门控**：仅 `PlayerGoal` 非空时才唤起辅导员；空目标零成本，保持现状零开销。
6. **后台并行 + 超时降级**：辅导员串在书记官前，但整条记账链在后台与叙事流式并行；辅导员失败/超时则书记官回退用原始 `PlayerGoal`（即现状行为，零回归风险）。

## 4. 数据流与时序

### 4.1 嵌入点（两条路径，共用 `EvaluateCoachAsync`）

正常游玩中玩家推进的**唯一常规入口是点选预计算选项 → 命中回放**，故辅导员 Full 评估的**主落点在回放路径**；真实轮路径保留作回退。两处共用提炼出的 `AiCoordinatorService.EvaluateCoachAsync`（`AnalyzeAsync → ToState → 回填 result + active 落库 CoachState → 返回 state`）：

| 路径 | 触发场景 | 落点 | 辅导员读到的世界状态 |
|---|---|---|---|
| **点选回放（主）** | 点选预计算选项且缓存命中（绝大多数轮） | `ApplyCachedActionResultAsync` **末尾**（世界状态 `S+ΔA` 落库、时段推进后） | `GetCurrentStateForDirectorAsync` 重读 = `S+ΔA`，与下一轮预计算 B 同源 |
| **真实轮（回退）** | 缓存未命中回退（走 `ProcessPlayerActionAsync`）/ 危险确认 | `RunScribeTaskAsync` 内、`ScribeAsync` 之前 | `世界状态快照(本轮开始前) + 导演既定事实`（≡ `S+ΔA`），与本轮书记官同源 |

两条路径**互斥不重复**：命中回放走 `ApplyCached` 评估，未命中回退走真实轮 `RunScribeTaskAsync` 评估。均复用独立 DI scope、自捕异常、20s 超时，不阻塞主链路与预计算就绪判定。

### 4.2 时序（主路径：点选回放 / 回退路径：真实轮）

**主路径——点选回放（正常游玩绝大多数轮，缓存命中）：**

```
预计算 A（上一轮回放后启动、已算完）：候选 A1/A2 各 DryRun（读库=S，复用 next_focus_{N-1} 产选项）
玩家点选 A1 → 缓存命中 → 回放 ledgerTask（独立 scope，与叙事流式并行）
   ① await A 的书记官 Task（回填 ScribeOutput）
   ② ApplyCachedActionResultAsync：落库 ΔA → DB=S+ΔA；InteractionCount++；时段推进
      ★辅导员 Full（high）：读 S+ΔA + 导演既定事实_A → next_focus_N → 落库 CoachState
      ★回填 result.CoachState(active) / result.CoachGoalEvent(complete|failed)
   ③ PushCoachLifecycleAsync：chip 进度 / 达成失效推送 + 归档清库
   ④ 物资官记账 → 推送背包/情报
   ⑤ 启动下一轮预计算 B：DryRun 读库=S+ΔA，复用 next_focus_N 注入书记官
```

**同源关键**：② 辅导员读 `S+ΔA`，⑤ B 预计算启动时（`GetCurrentStateForDirectorAsync`）读到的也是 `S+ΔA`——同一份 `current` 快照（物资官记账只动背包表、不重写快照），故辅导员与 B 书记官世界状态**严格一致**。A 已算完、辅导员无法介入 A；辅导员从 B 开始介入并同源。

**回退路径——真实轮（缓存未命中回退 / 危险确认，dryRun=false）：**

```
玩家行动 → 分类AI → 骰子 → 导演返回
                                │
        ┌───────────────────────┴────────────────────────┐
   叙事流式（玩家看）                          后台记账链 Task.Run（独立 scope）
        ‖ 并行                                    ① 若 PlayerGoal 非空：辅导员 AnalyzeAsync(Full/high)
                                                        → CoachOutput（next_focus / goal_status）
                                                  ② 书记官 ScribeAsync（读 next_focus）→ 选项 + 账目
                                                  ③ 落库：世界状态 + CoachState（含 next_focus）
                                                  ④ 物资官 → 推送背包/情报/选项 → 启动下一轮预计算
```

只要 `辅导员 + 书记官 < 叙事播放时长`，玩家无感（记账链 vs 叙事流式 = 并行，见 ai-architecture 并行边界）。

### 4.3 预计算时序处理（关键设计）

**风险**：辅导员 `high` 档比书记官 `low` 档慢 2-3 倍。预计算对 2 个候选选项各 DryRun，若每次都跑辅导员 = 2 次 high 档调用，会拖慢 `ScribeTask`（点选后需 `await` 回填），可能"赶不上预计算的书记官"，进而拖延点选后记账与下一轮预计算启动。

**分级策略（本方案决策）**：

| 场景 | 辅导员行为 | 说明 |
|---|---|---|
| **点选回放轮（主路径，缓存命中）** | `ApplyCached` 末尾 **Full 跑（high 档）** 读 `S+ΔA` → 更新 `CoachState`（含 `next_focus`）并落库 | **权威进度评估点（主）**，正常游玩每轮触发；与下一轮预计算 B 同源 |
| **真实推进轮（回退）**（dryRun=false 且 SuggestionsOnly=false；缓存未命中回退 / 危险确认） | **Full 跑（high 档）** → 更新 `CoachState` 并落库 | 权威进度评估点（回退），与回放轮互斥不重复 |
| **预计算 DryRun**（dryRun=true） | **跳过辅导员 LLM 调用**，DryRun 书记官**复用** session 已落库的 `CoachState.next_focus` | 规避时序压力、省 2 次 high 调用、`ScribeTask` 不被拖慢 |
| **退化轮**（不可行短路：dryRun=false 但 SuggestionsOnly=true） | **跳过辅导员 LLM 调用**，复用已落库 `CoachState.next_focus` | 世界未推进（拒绝叙事非实质既定事实），Full 重评无意义且浪费 high 调用；复用上轮方向引导“换个做法” |

**依据**：
- 预计算产的选项是"下一轮预览方向"，`next_focus` 作为**策略方向**跨一轮仍有效，滞后损失可接受。
- **点选回放轮**（A 落库后读 `S+ΔA`）才是权威进度评估点；预计算 DryRun 不落库，本就不该产生权威进度。
- 彻底规避"赶不上预计算书记官"的时序风险。

**Edge case**：
- `CoachState` 为空（玩家刚设目标、尚未跑过任何真实轮）：DryRun 书记官回退用原始 `PlayerGoal`（现状行为）。
- 首个行动轮：点选回放（或未命中回退真实轮）辅导员 Full 跑并落库 `CoachState`，此后预计算才有 `next_focus` 可复用。

## 5. 输入 / 输出 DTO

新增 `DHY.Game.AI/Dtos/CoachDtos.cs`。

### 5.1 CoachInput

复用书记官已装配的同源数据（世界状态快照 + 导演既定事实），另加上轮辅导员状态：

```csharp
public class CoachInput
{
    public string PlayerGoal { get; set; } = "";        // 玩家原始目标文本
    public string? PrevCoachState { get; set; }          // 上轮持久化的 CoachState JSON（首轮为 null）
    public string WorldState { get; set; } = "";         // 世界状态快照(本轮开始前，含历史)
    public string DirectorFacts { get; set; } = "";      // 本轮导演既定事实文本
    public string NpcProfiles { get; set; } = "";
    public string PlayerInventory { get; set; } = "";
    public string MainQuestProgress { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public bool IsAdult { get; set; }                    // 决定模板/模型（Coach / AdultCoach）
}
```

### 5.2 CoachOutput（严格 JSON，snake_case）

```json
{
  "clarified_intent": "消歧后的清晰目标（首轮确立，后续微调）",
  "goal_status": "active | complete | failed",
  "progress_note": "本轮世界状态下，目标推进到哪、已做了什么",
  "current_subgoal": "滚动更新后的当前子目标",
  "next_focus": [
    {"direction": "下一步策略方向（1-2 个，由辅导员自决数量）", "rationale": "为何是它"}
  ],
  "entities_involved": ["目标涉及的关键实体（人物/物品/地点）"],
  "completion_check": "goal_status=complete 时的达成依据；否则空字符串"
}
```

- `next_focus` 数量 **1-2 个由辅导员自决**（已确认）：目标路径单一时给 1 个主方向；存在有意义分岔（激进/稳健、主线/支线）时给 2 个。
- `next_focus` 须扎根当前世界状态、不指向未知实体（复用现有"目标实体存在性过渡"规则：涉及未确认实体时转为"打听下落/前往查探"）。

## 6. 提示词模板

**两套模板，区分成人/非成人**（与书记官、导演一致，仅模板与模型不同）：

| 轮次 | 模板文件 | 模型角色 key |
|---|---|---|
| 正常轮 | `player_coach_system.txt` | `Coach` |
| 成人轮 | `player_coach_adult_system.txt` | `AdultCoach` |

### 6.1 提示词骨架（`player_coach_system.txt`）

- **角色**：你是玩家的战略辅导员，站在玩家视角，把模糊的长期目标翻译成"当前世界状态下最优的下一步"。**你不写剧情、不改世界、不替玩家决策、不产具体行动选项**（那是书记官的职责）。
- **输入消息对**（按注意力权重排列，既定事实与目标放最后）：`[玩家角色名称]` → `[NPC档案]` → `[当前世界状态(含历史)]` → `[主线进度]` → `[玩家当前装备与道具]` → `[上轮辅导员状态]` → `[本轮既定事实(GM输出)]` → `[玩家目标]`。
- **规则要点**：
  1. 首轮（`PrevCoachState` 为空）先消歧，确立 `clarified_intent`。
  2. 基于既定事实 + 世界状态评估进度（`progress_note`），用"离目标更近了吗"而非硬性完成判定。
  3. 滚动更新 `current_subgoal`（可随世界状态调整，不死守初始规划）。
  4. 产出 1-2 个 `next_focus`，须扎根当前世界、服务目标、不指向未知实体。
  5. 仅当世界状态明显满足/否定目标时才置 `goal_status` 为 `complete`/`failed`，否则 `active`。
  6. 严格输出 JSON，禁止叙事文字。
- **成人版差异**：`player_coach_adult_system.txt` 与正常版同构，仅解除成人题材限制、措辞适配成人世界观（与 `scribe_adult_system.txt` 相对 `scribe_system.txt` 的差异模式一致）。

## 7. 模型配置

`GameAiOptions.json` 的 `Models` 下新增两个节点：

```jsonc
"Coach": {
  "ModelId": "deepseek-v4-flash-0731",
  "Temperature": 0.6,          // 略高于书记官(0.3)，允许策略发散
  "EnableThinking": true,
  "ReasoningEffort": "high",   // 关键：策略/进度判定质量靠它
  "BaseUrl": "https://dashscope.aliyuncs.com/compatible-mode/v1",
  "ApiKey": "..."
},
"AdultCoach": {
  "ModelId": "grok-4.3",
  "Temperature": 0.6,
  "EnableThinking": true,
  "Provider": "poixe",         // ReasoningEffort 对 poixe 不生效，靠 grok 自身能力
  "BaseUrl": "https://api.poixe.com/v1",
  "ApiKey": "..."
}
```

- 取用：`_modelFactory.GetModelConfig(input.IsAdult ? "AdultCoach" : "Coach")`；缺失回退 `qwen-plus`+thinking ON。
- 客户端路由：成人轮走 `CreateClient(config)` 按 `Provider` 路由到 `PoixeClient`（与成人书记官一致）。
- **改 JSON 后需重启**（`AiModelFactory` 用 `IOptions` 缓存，不热更新）。

## 8. 持久化

`GameDungeonSession` 新增字段（`CurrentPlayerGoal` 已存在，复用）：

```csharp
/// <summary>
/// 玩家辅导员状态 JSON（滚动子目标 + 进度 + next_focus）。
/// 真实轮辅导员 Full 跑后覆盖写入；预计算 DryRun 读取复用其 next_focus、不写。
/// 断线续玩/刷新可恢复。结构见 CoachState。
/// </summary>
[SugarColumn(ColumnDescription = "玩家辅导员状态JSON", ColumnDataType = "nvarchar(max)", IsNullable = true)]
public string? CoachState { get; set; }
```

**CoachState JSON 结构**（必须持久化 `next_focus`，供预计算 DryRun 复用）：

```json
{
  "clarified_intent": "...",
  "current_subgoal": "...",
  "progress_note": "...",
  "goal_status": "active",
  "next_focus": [{"direction": "...", "rationale": "..."}],
  "entities_involved": ["..."],
  "updated_round": 12
}
```

- SqlSugar 存 JSON 用字符串字段 + 手动序列化（与 `LastSuggestedActions` 同款），新增可空字段无需 `DefaultValue`。
- 目标清空/变更时一并清空 `CoachState`。

## 9. 目标全生命周期状态机

| 事件 | 处理 |
|---|---|
| 提交目标（`SetPlayerGoal`） | 写 `CurrentPlayerGoal`，清空旧 `CoachState`（下一真实轮辅导员冷启动消歧，`goal_status=active`） |
| 每轮记账（点选回放轮 / 真实轮） | 辅导员 Full 滚动评估 → 覆盖 `CoachState` |
| `complete` | 归档目标、推送"目标达成"（`ReceiveSystemMessage` type=success）、引导设新目标、清空 `CoachState` |
| `failed`（如目标 NPC 死亡/目标不可达） | 标记失效、推送提示（type=warn）、请玩家调整目标 |
| 变更目标 | 覆盖 `CurrentPlayerGoal` + 重置 `CoachState` |
| 清空目标 | `CurrentPlayerGoal` 与 `CoachState` 皆清（复用现有 `clearGoal`） |

> 冷启动时机：阶段 1 只在"首个真实行动轮"由辅导员 Full 消歧（改动最小）。阶段 3 可选增强——`SetPlayerGoal` 提交时异步 fire-and-forget 跑一次辅导员，立即存 `clarified_intent` + 初始规划，前端即时反馈"辅导员已理解你的目标：…"。

## 10. 与书记官的衔接

- `ScribeInput` 新增字段 `CoachFocus`（= `clarified_intent + current_subgoal + next_focus` 拼装文本）。
- `ScribeAiService`：`CoachFocus` 非空时注入 `[辅导员策略指引]` 消息对（替代原 `[玩家当前目标]` 原文）；`CoachFocus` 为空（辅导员失败/预计算无 CoachState）时回退注入原始 `PlayerGoal`（现状行为）。
- 装配流程（两处 Full 评估共用 `EvaluateCoachAsync`；门控：`PlayerGoal` 非空）：
  - **点选回放轮（主）**：`ApplyCachedActionResultAsync` 末尾（门控 `CurrentPlayerGoal` 非空 && 导演蓝图可得）→ 辅导员 Full（20s 超时，读 `S+ΔA`）→ `EvaluateCoachAsync` 回填 `result` + active 落库 `CoachState`（独立 try-catch）；整块再包一层 try-catch，异常绝不影响已落库世界状态与下游物资官/预计算。
  - **真实推进轮（回退）**：`RunScribeTaskAsync` 内、`ScribeAsync` 前（门控 `!dryRun && !SuggestionsOnly`）→ 辅导员 Full → 拼 `scribeInput.CoachFocus`（落库前就绪）→ 落库 `CoachState`（独立 try-catch 隔离，落库失败仅记 warn、不阻塞书记官）→ 书记官。
  - **预计算 DryRun / 退化轮（不可行短路）**：读 session `CoachState` → 拼 `scribeInput.CoachFocus`（复用，不调辅导员）→ 书记官。
  - **开场轻量路径**（`GenerateSuggestionsOnlyAsync`，绕过 `RunScribeTaskAsync`）：✅ 阶段 2 已实现——`BuildSuggestionsOnlyInputAsync` 读 `session.CoachState` 拼 `CoachFocus` 复用（无则回退原始 `PlayerGoal`）。
- **达成/chip 推送共用**：`GameSessionHub.PushCoachLifecycleAsync` 封装 `complete/failed` 达成失效推送 + 归档清库、`active` chip 进度推送，供真实轮与回放轮两处 ledgerTask 共用。
- **书记官提示词改造**：4 个模板（`scribe_system` / `scribe_adult_system` / `scribe_suggestions_system` / `scribe_adult_suggestions_system`）的 `<!--GOAL:ON-->` 块，把"至少一个选项服务 `[玩家当前目标]`"改为"**至少一个选项服务 `next_focus` 主方向**，另一个可接引导线索 / 备选方向 / 场景推进"，保留双选项差异性规则与"实体存在性过渡"规则。
  - ⚠️ 模板改动后必须 rebuild，靠 csproj `PreserveNewest` 同步到 `bin/Prompts/Templates` 方生效。

## 11. 成人轮处理

- 与书记官、导演、分类一致：`aiRole = IsAdult ? "AdultCoach" : "Coach"`，模板二选一（`player_coach_adult_system.txt` / `player_coach_system.txt`）。
- 成人轮辅导员产出 `next_focus` 流入**成人书记官**（`AdultScribe`），全链路成人自洽。
- 预计算 DryRun 继承会话级成人模式（`isAdult` 透传），成人轮 DryRun 同样跳过辅导员、复用 `CoachState.next_focus`。

## 12. 超时降级与失败策略

- 辅导员调用照书记官模式：失败**重试 1 次**；仍失败返回 `null`。
- 回放轮 / 真实轮两处 Full 评估均设超时（20s，high 档留足余量）；超时/失败 → `CoachFocus=null` → 书记官回退原始 `PlayerGoal`，`CoachState` 保持上轮值不覆盖，记 warn 日志。
- 辅导员 Task 内自捕异常，**永不抛给调用方、不阻塞记账链**（与 `RunScribeTaskAsync` 一致）。
- AI 调用全量记 `GameAiCallLog`（复用书记官日志规范），`EnableDebugLog` 时输出调用链。

## 13. 前端（✅ 阶段 2 已实现）

- **目标 chip 增强**：展示 `clarified_intent`，可展开显示 `current_subgoal` / `progress_note`，让玩家看到"辅导员理解的目标 + 当前进展"，增强目标感与沉浸感。
- **达成/失效推送**：复用 `ReceiveSystemMessage`（success/warn）。
- **断线续玩一致**：`DungeonReadyDto` / `ActiveSessionCheckOutput` 增补 `CoachState`（或其展示字段）回填，与 `CurrentPlayerGoal` 同步。
- Pinia `game` store 增补 `coachState` 状态 + localStorage 持久化（与 `currentGoal` 同款）。

## 14. 落地改动清单（文件级）

| 层 | 文件 | 改动 |
|---|---|---|
| 配置 | `DHY.FrameWork.Application/Configuration/GameAiOptions.json` | +`Coach` / `AdultCoach` 节点 |
| DTO | `DHY.Game.AI/Dtos/CoachDtos.cs`（新） | `CoachInput` / `CoachOutput` / `CoachState` / `NextFocusItem` |
| DTO | `DHY.Game.AI/Dtos/ScribeOutput.cs` | `ScribeInput` +`CoachFocus` |
| Service | `DHY.Game.AI/Services/PlayerCoachService.cs`（新） | `AnalyzeAsync`（照 `ScribeAiService` 模式：重试/日志/JSON 解析） |
| 提示词 | `DHY.Game.AI/Prompts/Templates/player_coach_system.txt`（新） | 辅导员正常轮提示词 |
| 提示词 | `DHY.Game.AI/Prompts/Templates/player_coach_adult_system.txt`（新） | 辅导员成人轮提示词 |
| 提示词 | 4× `scribe_*system.txt` | `<!--GOAL:ON-->` 块改服务 `next_focus` |
| 编排 | `DHY.Game.AI/Services/AiCoordinatorService.cs` | 提炼 `EvaluateCoachAsync`（真实轮 + 回放轮共用）；`RunScribeTaskAsync` 真实轮 Full / DryRun 复用；**`ApplyCachedActionResultAsync` 末尾新增回放 Full 评估**（读 `S+ΔA`、门控 `CurrentPlayerGoal`+导演蓝图、整块 try-catch 隔离） |
| 实体 | `DHY.Game.Core/Entities/GameDungeonSession.cs` | +`CoachState` 字段 |
| Hub | `DHY.Game.Hub/Hubs/GameSessionHub.cs` / `Dtos/GameHubDtos.cs` | `SetPlayerGoal` 重置 `CoachState`；提炼 `PushCoachLifecycleAsync`（达成/失效/chip 推送）供**真实轮 + 回放轮两处 ledgerTask 共用**；`DungeonReady`/`ActiveSessionCheck` 回填 |
| 前端 | `client/src/stores/game.ts`、`composables/useSignalR.ts`、`components/PlayerInput.vue`、`types/game.ts` | chip 进度展示 + 状态同步 |

## 15. 分期实施

- **阶段 1（核心闭环，✅ 已实现）**：辅导员 Service + 2 套提示词 + `Coach`/`AdultCoach` 配置 + 嵌入 `RunScribeTaskAsync`（真实推进轮 Full / 预计算与退化轮复用，门控 `!dryRun && !SuggestionsOnly`）+ `next_focus` 注入书记官 + 4 书记官模板 `GOAL:ON` 改造 + `CoachState` 持久化 + 超时降级 + 落库隔离 + `SetPlayerGoal` 覆盖/清空目标时一并清空 `CoachState`。
- **阶段 2（体验增强，✅ 已实现）**：
  - 目标状态机推送：辅导员判定 `complete`/`failed` → 协调器回填 `GameActionResult.CoachGoalEvent` → Hub 后台链 `ReceiveSystemMessage`（success 绿/warning 琥珀）+ 归档清空 `CurrentPlayerGoal`+`CoachState` + `UpdatePlayerGoal` 空态收起前端 chip；`completion_check` 经事件 `Note` 直传前端、不入 `CoachState`（ℹ️-C）。
  - 前端 chip 进度：真实推进轮 active → `GameActionResult.CoachState` → Hub `UpdatePlayerGoal` 推送 → `PlayerInput` chip 展示 `clarified_intent`、可展开 `current_subgoal`/`progress_note`；Pinia `game` store `coachState` + localStorage 持久化。
  - 断线回填：`DungeonReadyDto`（Hub `CoachStateDto`）+ `ActiveSessionCheckOutput`（Core `CoachStateViewDto`，JObject 解析 snake_case）增补 `CoachState`，与 `CurrentPlayerGoal` 同步。
  - 开场轻量路径：`BuildSuggestionsOnlyInputAsync` 复用 `session.CoachState.next_focus`（覆盖开场 + 不可行短路）。
  - `failed` 语义：采用“清空归档 + warning 提示重设”（目标不可达后不再引导）。
- **阶段 2.1（触发点迁移，✅ 已实现）**：修复“辅导员 0 调用”缺陷——原 Full 评估只挂真实轮分支（`!dryRun && !SuggestionsOnly`），而正常游玩几乎全程走点选命中回放（`ApplyCachedActionResultAsync`），导致辅导员实际从不触发、`next_focus` 永不落库注入。将 Full 评估**主落点迁移到回放路径末尾**（读 `S+ΔA`、与下一轮预计算 B 同源），提炼 `EvaluateCoachAsync`（真实轮+回放轮共用）与 `PushCoachLifecycleAsync`（两处推送共用）；真实轮块保留覆盖缓存未命中回退与危险确认。dotnet build 0 错误。
- **阶段 3（可选）**：`SetPlayerGoal` 提交时异步冷启动消歧 + 前端即时反馈。

## 16. 验收标准（目标驱动）

1. 玩家提交模糊目标后，首个真实行动轮产出的选项中，至少一个明显贴合"消歧后的具体意图"（对比现状的字面猜测）。
2. 多轮推进同一目标时，选项体现"子目标滚动前进"，不在原地绕圈；`CoachState.progress_note` 逐轮反映进度。
3. 目标达成/失效时收到推送，`CoachState` 被正确清理。
4. **点选回放轮触发辅导员 Full 评估**（日志 `[AI链路][Coach]` 核验）：设目标 → 点选选项推进后，`CoachState` 落库且 `updated_round` 递增，下一轮预计算书记官 GOAL 块含 `next_focus`；预计算 DryRun 阶段**不产生辅导员 LLM 调用**，点选后选项受 `next_focus` 引导。
5. 辅导员超时/失败时，书记官回退原始目标、记账链不中断、无异常抛出（故障注入核验）。
6. 成人轮走 `AdultCoach` + 成人模板，链路自洽。
7. `dotnet build` 0 错误、`vue-tsc` 0 错误；提示词模板经 rebuild 同步到 `bin/Prompts/Templates`。

## 17. 变更记录

| 版本 | 日期 | 说明 |
|---|---|---|
| v1.0.0 | 2026-10-01 | 玩家辅导员系统设计定稿：专职角色管理玩家目标全生命周期，滚动子目标 + next_focus 注入书记官；真实轮 Full/预计算复用的分级时序；区分成人/非成人（Coach/AdultCoach） |
| v1.1.0 | 2026-10-01 | 阶段 1 核心闭环落地（dotnet build 0 错误）：`RunScribeTaskAsync` 门控路由 `!dryRun && !SuggestionsOnly` 才 Full、`CoachState` 落库独立 try-catch 隔离、`SetPlayerGoal` 覆盖/清空目标时清空 `CoachState`；确认退化轮（不可行短路）复用、开场路径复用与 `completion_check` 达成推送归阶段 2 |
| v1.2.0 | 2026-10-01 | 阶段 2 体验增强落地（dotnet build / vue-tsc 0 错误）：目标达成/失效状态机推送（`CoachGoalEvent`→Hub `ReceiveSystemMessage` success/warning + 归档清空 + `UpdatePlayerGoal` 收起 chip，`completion_check` 经事件直传不入 `CoachState`）；前端 chip 展示 `clarified_intent`+可展开子目标/进度、`coachState` store 持久化；断线回填 `DungeonReadyDto`/`ActiveSessionCheckOutput`（Core `CoachStateViewDto` JObject 解析）；开场路径 `BuildSuggestionsOnlyInputAsync` 复用 `CoachState`；`failed` 采清空归档语义 |
| v1.3.0 | 2026-10-06 | 触发点迁移（修复“辅导员 0 调用”缺陷，dotnet build 0 错误）：辅导员 Full 评估主落点从真实轮迁移到点选命中回放路径（`ApplyCachedActionResultAsync` 末尾读 `S+ΔA`、与下一轮预计算 B 同源）；提炼 `EvaluateCoachAsync`（真实轮+回放轮共用评估核心）与 `GameSessionHub.PushCoachLifecycleAsync`（两处推送共用）；真实轮块保留覆盖缓存未命中回退/危险确认；DryRun/退化轮复用分支、`GameActionResult`/`CoachDtos`/前端均不改 |
