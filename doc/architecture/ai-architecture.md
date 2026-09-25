# AI系统架构 v4.9.1

> 版本：4.9.1 | 生效日期：2026-09-22 | 变更类型：**提示词调优（player_choice_point 与 beat_scale 解耦 + 判定收紧）**。导演模板 `director_front_system.txt` / `director_adult_front_system.txt` 两处调整：①**解耦**——删除「`player_choice_point=true` → 强制 `beat_scale=chapter`」触发条件，`player_choice_point` 不再影响叙事分档，仅作为书记官「选项节奏档」（`IsKeyMoment`）判据之一；`beat_scale` 分档依据回归「内容分量」（主线节点 / tension≥8 / 重大转折·关键揭示 / [推进型行动] 标记）。②**收紧 `player_choice_point` 判定**——语义明确为「有实质后果、会改变剧情走向的重大抉择点」，默认 false；true 条件加限定（不可逆后果 / 显著改变走向 / 需明确表态且实质影响局势），并新增负面清单（普通寒暄询问、无实质分歧的常规推进轮、`tension_level<6` 且无重大转折的普通轮、可自由输入的常规行动轮一律 false）。**背景**：原规则「任一条件满足即 true」过于宽泛（TRPG 几乎每轮都存在多路径/NPC提问/新区域），导致紧张度仅 5~6 的普通轮也频繁 `player_choice_point=true`，连带顶成 chapter，使书记官选项几乎恒为细粒度、粗粒度推进档形同虚设。**代码层 `AiCoordinatorService.IsKeyMoment`（`PlayerChoicePoint || BeatScale==chapter || TensionLevel>=8`）未改动**，仅输入变干净。需重新构建刷新 bin 模板副本后生效。详见 [叙事设计规范 v2.5.1](narrative-design.md)
>
> 历史版本 4.9.0（2026-09-19）：**VIP模式（预计算阶段预生成叙事，点选秒回放）**。新增会话级手动开关 `IsVipMode`（前端 `StatusPanel` 下拉菜单 ⚡ 按钮切换、`gameStore.isVipMode`，不持久化、刷新归零，与成人模式同构）。开启后预计算在**导演层之后额外预生成叙事正文**（相当于对 VIP 会话按需把预计算深度从 L1 升到 L3），玩家点选选项时直接**文本回放**（首字延迟≈0），而非实时流式生成。**代价**：每轮为2个选项各生成一次叙事，玩家只用1个 → 叙事 token 约翻倍；且预计算就绪时刻从「导演(~15s)」回升到「导演+叙事(~35s)」，玩家读得快时可能未就绪 → 该轮自动降级实时流式（不报错）。**实现**：`PrecomputeAsync`/`PrecomputeSingleOptionAsync` 加 `bool isVip` 参数，VIP 且 `NarrativeInput != null` 时调 `NarrativeAiService.GenerateNarrativeAsync`（非流式，内部自动处理章节档）预生成文本写入 `PrecomputedActionCache.NarrativeText`（复用 L1 改造后废弃字段），预生成失败静默降级（`NarrativeText` 留空）；`ProcessSelectCachedActionAsync` 点选时优先判 `cached.NarrativeText` 非空 → `StreamNarrativeAsync` 回放，否则降级 `StreamNarrativeLiveAsync`。`IsVipMode` 经4个输入DTO（`PlayerActionInput`/`SelectCachedActionInput`/`SelectDungeonInput`/`RestartSessionInput`）透传，4处预计算入口（常规轮/缓存轮记账链尾、开场轮、重新开始轮）均带上；`ProcessRestartSessionAsync` 签名加 `bool isVip`。**边界**：VIP 只影响预计算深度与点选回放；自由输入路径无预计算，仍实时流式；不可行短路（`NarrativeInput=null`）不预生成，点选回放协调器已产出的拒绝文案。
>
> 历史版本 4.8.0（2026-09-19）：**成人轮预计算继承会话级成人模式**（对齐世界难度 override 的“下一轮预计算起生效”语义）。废除 v4.6.0 的“成人轮不做预计算”门控：`ActionPrecomputeService.PrecomputeAsync` / `PrecomputeSingleOptionAsync` 新增 `bool isAdult` 参数，透传到 `ProcessActionInput.IsAdultMode`（与 `WorldDifficultyOverride` 同一条透传链）；`GameSessionHub` 两处预计算启动点（`ProcessPlayerActionAsync` / `ProcessSelectCachedActionAsync` 记账链尾）传 `input.IsAdultMode`，使成人轮也用成人模型（`AdultClassifier`→`AdultDirector`）预掷、缓存 `NarrativeInput.IsAdult = true` 的导演蓝图与 `AdultScribe` 书记官任务；点选命中后叙事实时按 `IsAdult` 选 `AdultNarrative`、书记官 `await` 成人 `ScribeTask`，全链路成人自洽。两处“成人轮直接推送选项”的 `else if` 分支已删除（成人轮 `precomputeTask != null` 走正常预计算推送）。`LastSuggestedActions` 持久化仍跳过成人轮（避免断线恢复显示成人选项文本）。**切换语义**：切换成人模式后当前已缓存选项按旧模式回放（最多一轮过渡），从下一次预计算起全链路切换；切换后手动输入立即生效（无过渡）。开场轮预计算无成人输入，固定传 `false`。
>
> 历史版本 4.7.0（2026-09-18）：**玩家自由输入语义重定义（目标声明制）**。前端自由输入框不再作为“本轮行动”进入分类/导演/叙事链路，而是作为“中长期目标声明”仅写库；下一轮书记官构造 `ScribeInput` 时读取该目标并以 `[玩家当前目标]` 消息对注入 prompt，让产出的 `suggested_actions` 围绕目标生成。新增 Hub 方法 `SetPlayerGoal` + DTO `SetPlayerGoalInput` + 实体字段 `GameDungeonSession.CurrentPlayerGoal`（覆盖式存储、跨会话持久化、不自动清空）+ `ScribeInput.PlayerGoal`；`DungeonReadyDto` 与 `ActiveSessionCheckOutput` 同步回填 `CurrentPlayerGoal`，支持断线续玩/跨设备/页面刷新一致性；4 个书记官模板（`scribe_system` / `scribe_adult_system` / `scribe_suggestions_system` / `scribe_adult_suggestions_system`）各加一条“两档通用规则”：若含 `[玩家当前目标]`，两个选项应共同服务于该目标但不得违反本轮既定事实与世界状态，目标与剧情冲突时优先输出化解冲突的路径。前端：`PlayerInput.vue` 新增目标 chip（🎯 当前目标 {text} ×）+ 100 字硬上限 + placeholder 改为“表达你的意图或目标…”；`gameStore.currentGoal` 接入 localStorage 持久化；`useGameSession` 新增 `setGoal` / `clearGoal` 包装（乐观更新）。**不新增 AI 节点、不新增链路**：目标提交流程只写库 + 推送一条 info 反馈消息，不触发任何 AI 调用、不失效预计算缓存、不刷新当前选项。Hub 既有 `PlayerAction` 方法保留，仅供 `SelectCachedAction` 缓存未命中时的回退路径内部使用（前端自由输入不再调用）。
>
> 历史版本 4.6.0（2026-09-18）：**成人内容全链路对齐**。成人轮（会话级成人模式开启 `IsAdultMode`，或分类AI判定 `is_adult`）不再走 `HandleAdultAction` 快捷通道跳过导演，而是与正常轮走**完全一致**的「分类 → 导演 → 叙事 → 书记官（完整记账）→ 物资官」全链路，仅由导演/叙事/书记官三个AI按 `IsAdult` 切换成人版提示词模板 + Poixe 模型配置。新增 `director_adult_front_system.txt` / `scribe_adult_system.txt` / `scribe_adult_suggestions_system.txt` 三个成人模板与 `AdultDirector` / `AdultScribe` 两个模型配置（`AdultNarrative` 已有）；`narrative_adult_system.txt` 改为消费导演蓝图（占位符与 `narrative_system` 一致），成人轮同样支持章节档 `beats`。成人轮**不做预计算**（避免用非成人模型生成预取预览），但书记官产出的2个选项照常推送、点击时经缓存未命中回退走实时成人全链路。
>
> 历史版本 4.5.0（2026-09-18）：**网文优先框架重构（去电影感）**。前台导演提示词的角色从「TRPG导演(DM)」改为「游戏主持人(GM)」（模型可见词，代码组件名 `Director`/`director_front_system.txt` 不变）；`narrative_seed` 从「250字文学场景速写」改为「**剧情细纲**」（时间顺序事件条目 + NPC台词大意 + 玩家状态变化），叙事AI**据细纲从零写正文**而非扩写散文；`prose_guidance` 从「句式节奏+感官重点+文学手法」改为「叙事节奏+玩家情绪落点+必须写清的信息」；`beats` 分镜表→分段细纲；叙事AI创作三原则→写作四原则（网文可读性优先）；建筑师文风圣经禁用电影/文学流派当语调；整链移除电影术语。详见 [叙事设计规范 v2.5.0](narrative-design.md)
>
> 历史版本 4.4.2（2026-09-16）：**书记官选项接住导演引导线索 + 两侧提示词微瘦身**。`scribe_system.txt` 新增规则：`suggested_actions` 必须接住既定事实中的「引导线索」（导演 `narrative_hooks` 经 `BuildDirectorFacts` 拼入）——至少一个选项顺线索延伸，多条线索时两选项分别对应不同线索，禁止同跟一条。同批删除已废弃的 `director_system.txt`（含 bin 副本），导演模板删纯旁白行，书记官模板去三处重复
>
> 历史版本 4.4.1（2026-09-16）：前台导演提示词瘦身第二批——删 `[常规行动]` 规则及导演侧 `IsRoutine` 注入链；主线进度规则并入主动引导规则，改为响应 `[剧情推进提示]`；schema 与规则段去双写；新增支线/隐藏内容清单使用规则
>
> 历史版本 4.4.0（2026-09-16）：整链拆除 `needs_state_change`（分类AI判定与输出、三处 DTO 字段、`[无需状态变更]` 标记与规则、时段推进分类门控），`time_advance` 改为纯由导演判定
>
> 历史版本 4.3.0（2026-09-16）：书记官常规轮始终完整记账——移除纯叙事轮的 SuggestionsOnly 分流，SuggestionsOnly 仅保留给无导演蓝图的场景（开场轮/不可行短路）
>
> 历史版本 4.1.0（2026-09-13）：预计算深度下调到 L1（导演层）——就绪时刻 ~35s→~15s，点选后叙事实时流式生成、后台链 await 书记官回填再落库；新增两个延迟埋点；L1.5 列为备用方案

## 六AI角色

### 分类AI（Classifier）

| 属性 | 值 |
|------|------|
| 职责 | 行动分类 + **可行性三态判定（唯一综合门卫）** + 成人内容判定 + **行动意图提炼** + **技能判定**（DC/技能/优劣势）；不再判定「是否需要状态变更」（v4.4.0 移除） |
| **模型（v4.0.0）** | `deepseek-v4-flash`，**要求 `EnableThinking = false`**（轻量路由任务，思考链无实质收益却占关键路径） |
| 输入上下文 | **局面快照(无历史)** + NPC档案 + **可用资产清单**（玩家背包 + 已知情报/线索账本，只读） |
| 输出格式 | JSON `{is_routine, confidence, reason, feasibility, infeasible_reason, is_adult, action_intent, judgment}` |

**judgment 结构**（非常规行动且可行时输出）：
```json
{
  "needed": true,
  "skill": "隐匿",
  "dc": 15,
  "advantage": false,
  "disadvantage": false,
  "context": "标准锁具，守卫在走廊巡逻"
}
```

### 前台导演AI（Director）

| 属性 | 值 |
|------|------|
| 职责 | 推演世界反应，**基于判定结果**生成本轮剧情细纲+写法提示+对话层级；**只做创作与节奏决策，不产出任何状态账目**（提示词角色为「游戏主持人(GM)」，代码组件名仍为 Director） |
| **模型（v4.0.0）** | `deepseek-v4-flash`，**要求 `EnableThinking = false`**（关键路径；输出 schema 砍半后解码耗时显著下降。`qwen3.7-max` 为备选，按实测质量定夺） |
| 输入上下文 | 副本设定 + NPC档案 + **局面快照(含历史)** + 主线进度 + 支线/隐藏内容清单（埋线索素材） + 玩家背包 + **[判定结果]** + **[行动意图+原始表达]**；条件注入：`[角色再定位]`、`[剧情推进提示]`（代码层停滞检测命中）、`[推进型行动]`（点选粗粒度选项）。不再注入 `[常规行动]` 标记（v4.4.1 移除：它与掷骰无因果关系，“无[判定结果]则直接描述结果”已覆盖其语义） |
| 输出格式 | 严格JSON（**narrative_seed**(250字剧情细纲：时间顺序事件条目+NPC台词大意+玩家状态变化) / **prose_guidance**(叙事节奏+玩家情绪落点+必须写清的信息) / **beat_scale**(节拍分档：micro/normal/chapter) / **beats**(章节档分段细纲4-8个，含seed/beat_type/focus) / **narrative_word_target**(目标字数) / npc_actions(含**dialogue_direction**四层结构，**不含 attitude_change**) / pacing / narrative_hooks / player_choice_point / time_advance） |
| 模板 | `director_front_system.txt`（v4.0.0 从 `director_system.txt` 拆出）；**成人轮切换 `director_adult_front_system.txt`**（v4.6.0，输出 schema 与正常导演一致） |

**关键变化（v4.0.0：前台/后台拆分）**：
- `world_state_changes`（含 `quest_progress`）、`item_hints`、`npc_attitude_changes`、`suggested_actions` 四类状态账目**全部移交书记官AI**，与叙事流式并行产出
- 前台导演（GM）的输出即**既定事实**：书记官只依此记账，**GM 没写的事等于没发生**——故剧情涉及的状态后果必须在 `narrative_seed` 或 `npc_actions` 中明确体现
- 输出 schema 减半（原 ~1965 token），直接缩短玩家等待的解码时间

**关键变化（v3.0.0）**：
- `narrative_direction`（100字事件概述）→ `narrative_seed`（250字文学性场景速写），从"剧情大纲"变为"小说家草稿"
- 新增 `prose_guidance`：句式节奏+感官重点+文学手法，指导叙事AI的文风选择
- `dialogue_gist`（字符串）→ `dialogue_direction`（四层结构：surface/subtext/conceal/body_language）
- 移除 `sensory_hint`（职责合并到 prose_guidance）
- `world_state_changes` 从字符串数组升级为结构化对象（`WorldStateChangesDto`），所有字段 nullable，仅变化时输出（v4.0.0 起由书记官输出）

### 书记官AI（Scribe）

| 属性 | 值 |
|------|------|
| 职责 | **状态记账执行器**：依前台导演的既定事实，产出全部结构化状态字段（不参与创作、不改写剧情） |
| 模型 | `deepseek-v4-flash`，`Temperature = 0.3`，**要求 `EnableThinking = false`** |
| 时机 | **前台导演返回后，与叙事流式并行**（作为后台 `Task` 随 `GameActionResult.ScribeTask` 透传，Hub 在叙事播放期间 `await`） |
| 触发门控 | **所有经过导演蓝图的常规轮一律走完整记账**（无分类门控）；仅**无导演蓝图的场景**（开场轮/不可行短路）走 SuggestionsOnly 轻量模式 |
| 输入上下文 | 玩家行动+意图 + 判定结果 + **前台导演既定事实文本** + **玩家当前目标（v4.7.0，非空时注入）** + 局面快照 + NPC档案 + 玩家背包 + 主线进度 + 支线/隐藏内容清单 |
| 输出格式 | JSON `{world_state_changes(含quest_progress), item_hints, npc_attitude_changes, suggested_actions}` |
| 落库职责 | `WorldStateService.ApplyChangesAsync`（世界状态）+ `NpcService.UpdateAttitudeAsync`（NPC态度）；`item_hints` 交物资官、`suggested_actions` 交预计算 |
| 轻量模式（SuggestionsOnly） | `ScribeInput.SuggestionsOnly = true` 时切换模板 `scribe_suggestions_system.txt`，只产出 `suggested_actions`（恰好2个）；`RunScribeTaskAsync` 内强制忽略 `world_state_changes`/`npc_attitude_changes` 不落库。**v4.3.0 起仅覆盖开场轮（首次进入·重新开始·重玩）与不可行短路**（无导演蓝图的场景），纯叙事轮已回归完整记账 |
| 玩家目标注入（v4.7.0） | `ScribeInput.PlayerGoal` 非空时，`ScribeAiService` 在 `[本轮既定事实]` 后、`[选项节奏档]` 前注入 `[玩家当前目标]` 消息对（附带“中长期意图、非本轮行动、选项须服务于目标但不得脱离既定事实”提示）。目标来源：`GameDungeonSession.CurrentPlayerGoal`（前端 `SetPlayerGoal` Hub 方法写入）。常规路径与 SuggestionsOnly 路径均支持；断线续玩/重开时目标仍生效 |
| 成人轮模板（v4.6.0） | `ScribeInput.IsAdult = true` 时切换成人版模板，与正常轮同构二选一：完整记账轮用 `scribe_adult_system.txt`（输出全量结构化账目、正常落库），SuggestionsOnly 轻量轮用 `scribe_adult_suggestions_system.txt`；模型统一走 `AdultScribe` |
| 失败策略 | 失败**重试1次**；仍失败则**本轮状态不变、叙事不中断**，记 error 日志（Task 内自捕异常，永不抛给调用方） |
| 纪律 | 只记录既定事实，不新增剧情、不改写导演结论 |

### 叙事AI（Narrative）

| 属性 | 值 |
|------|------|
| 职责 | 第二人称通俗小说写作（网文可读性优先），基于 GM 的剧情细纲从零写正文（而非扩写润色散文） |
| 输出格式 | 流式纯文本 |
| 成败感知 | 仅从细纲的 narrative_seed 感知（不重复注入骰子数值） |
| 道具纪律 | **不得新增或取消细纲中的道具/情报交付**，也不得自行发明细纲里没有的NPC/武器/事件；细纲里列的事一件不漏、没有的事一件不加 |
| 创作指导 | 写作四原则（先说清事/大白话优先/情绪直给/一个细节就够）+ 硬性禁令 + 对话写作原则 + 创作思考指南(thinking mode) + 场景文风模块 + 文风圣经 + 意象追踪 + 3个白话Few-shot示例 |
| 模板 | `narrative_system.txt`；**成人轮切换 `narrative_adult_system.txt`**（v4.6.0 已改为消费导演蓝图，占位符与 `narrative_system` 完全一致，同样支持章节档分段生成） |

### 道具AI（Quartermaster，物资官）

| 属性 | 值 |
|------|------|
| 职责 | **纯记账员**：依书记官蓝图 `item_hints` 逐条扩展出完整数值并落库，**资产账本的唯一写入权威** |
| 时机（v4.0.0） | **书记官 `await` 完成后，与叙事流式并行**（`RecordFromBlueprintAsync`，书记官蓝图为事实基准，不读叙事正文） |
| 记账链顺序 | 书记官记账 → 物资官记账 → 背包/情报推送 → 启动下一轮预计算（保证预计算的分类AI读到最新账本） |
| 触发门控 | 书记官输出 `item_hints` 非空才调用（纯对话/观察轮零成本跳过） |
| 落库路径 | 物理道具走 `InventoryService`（背包）；无形资产（情报/线索/承诺）走 `KnownAssetService`（已知情报账本） |
| 输出格式 | JSON `LedgerDelta`：`{acquired_items, consumed_items, lost_items, acquired_info, invalidated_info}`，未变更分组省略 |
| 纪律 | 逐条落实蓝图、补全合理数值（重量/加值/次数等），**不得新增蓝图外的道具** |
| 降级保底 | LLM 调用失败/解析失败时，按结构化蓝图规则化保底落库：物品按 `is_key` 记为关键道具/杂物（默认重量 0.5），情报以蓝图 `note` 为内容、来源标注“导演蓝图（规则化保底）”——关键资产不因模型故障静默丢失；失败日志含会话/行动/蓝图摘要 |

### 建筑师AI（Architect）

| 属性 | 值 |
|------|------|
| 职责 | 一次性生成完整副本世界 + **文风圣经** + **意象系统** |
| 输出内容 | NPC / 任务 / 隐藏内容 / 时间线 / **style_bible** / **motifs** |
| 输出格式 | 结构化JSON |

**文风圣经（Style Bible）**（一次性生成，每轮注入叙事AI）：
```json
{
  "tone": "语调（一句大白话说清故事气质，如'糙糙的求生故事，没有人是好人'；禁用电影/文学流派如'黑色电影风格''魔幻现实主义'）",
  "sentence_rhythm": "叙事节奏偏好（如'对话多、推进快；危险时句子变短'；禁'从句''窒息感''碎片化''黏稠''绵长'）",
  "sensory_palette": ["消毒水味", "灯管嗡嗡响", "瓷砖冰冷"],
  "forbidden_cliches": ["令人毛骨悚然", "不寒而栗", "腔背发凉"]
}
```

**意象系统（Motifs）**（3-5个贯穿副本的核心意象，带进化路径）：
```json
[
  {
    "name": "滴水声",
    "initial_state": "走廊尽头的天花板在漏水，滴答声在空旷的空间里回荡",
    "evolution_hint": "从背景噪音→战斗中的节奏干扰→揭示为血水"
  }
]
```

文风圣经和意象存储在 `GameDungeonSession.StyleBibleJson` 和 `MotifsJson`，每轮由代码层注入叙事AI。旧副本（无此字段）优雅降级为空字符串。

## 协作流程

### 标准流程（非常规行动）

```
玩家输入 → Classifier(分类+三态可行性门卫+意图提炼+技能判定)
         → 代码层掷骰(D20+调整值 vs DC+世界难度修正)
         → Director(知成败，接收意图+原文，生成叙事种子+世界反应+节奏决策)
         → 【玩家等待在此结束，叙事开始流式推送】
         → [ Narrative(流式叙事，玩家阅读中)
           ‖ Scribe(记账) → Quartermaster(依 item_hints 落库) → 背包/情报推送 → 启动预计算(读最新账本) ]
```

**信息流关键改进**：
- 导演AI在做所有决策时已知道骰子结果，消除了旧架构中"导演蓝图可能暗示成功但骰子失败"的信息断层
- **资产变更跟随书记官蓝图**（书记官依前台导演既定事实记账，叙事仅扩写）
- **记账链整体移出关键路径**（v4.0.0）：书记官与物资官在玩家阅读叙事的时间窗口内完成，不再阻塞叙事首 token；因预计算在记账链尾启动，下一轮门卫与预计算仍能读到本轮变更

### 常规行动（快速路径）

```
玩家输入 → Classifier判定为常规(无judgment) → 直接 → Narrative流式叙事
```

### 可行性三态分流

```
玩家输入 → Classifier三态判定:
    feasible   → 正常走标准流程
    uncertain  → 照常走导演流程，由导演AI结合完整剧情做叙事终审
    infeasible → 直接返回拒绝叙事（不进入导演流程）
```

### 成人内容全链路（v4.6.0）

成人内容与正常内容共用同一条链路，**唯一差异是三个创作/记账AI切换成人版提示词模板与 Poixe 模型**；分类、掷骰、可行性门卫、时段推进、完整状态记账、物资官落库、NPC态度更新、选项推送全部照常。

**触发与判定**（`AiCoordinatorService.ProcessPlayerActionAsync`）：

```
isAdult = input.IsAdultMode（会话级成人模式开关） || classification.IsAdult（分类AI逐条判定）
```

- 分类AI之后计算 `isAdult`，透传到 `DirectorInput.IsAdult` / `NarrativeInput.IsAdult` / `ScribeInput.IsAdult`
- **v4.6.0 前**：`IsAdultMode` 走 step 0 快捷通道、`classification.IsAdult` 走 step 2 短路，二者均调 `HandleAdultAction` 跳过分类/导演，叙事不依赖蓝图、无书记官、无选项。**该快捷通道与 `HandleAdultAction` 已删除**
- 主轮 `ScribeInput.SuggestionsOnly = false`（成人同样完整记账）；仅开场/不可行补救走 SuggestionsOnly 轻量模式

**三个AI的成人切换**：

| AI | 正常模板 / 模型 | 成人模板 / 模型 |
|----|----------------|----------------|
| 前台导演 | `director_front_system` / `Director` | `director_adult_front_system` / `AdultDirector` |
| 叙事 | `narrative_system` / `Narrative` | `narrative_adult_system` / `AdultNarrative` |
| 书记官（完整记账） | `scribe_system` / `Scribe` | `scribe_adult_system` / `AdultScribe` |
| 书记官（仅选项） | `scribe_suggestions_system` / `Scribe` | `scribe_adult_suggestions_system` / `AdultScribe` |

- 三个成人模型均走 `Provider = poixe`（`grok-4-latest`）。⚠️ `DirectorAiService` / `ScribeAiService` 必须用 `AiModelFactory.CreateClient(config)`（而非无参 `CreateClient()`）才能按 `Provider` 路由到 `PoixeClient`
- `narrative_adult_system.txt` 已改为消费导演蓝图，成人轮同样支持章节档 `beats` 分段流式生成（`IsChapterScale` 不再排除成人）

**预计算与选项推送（v4.8.0：成人轮也做预计算）**：预计算继承会话级成人模式——`PrecomputeAsync` / `PrecomputeSingleOptionAsync` 的 `isAdult` 参数透传到 `ProcessActionInput.IsAdultMode`，`GameSessionHub` 两处预计算启动点传 `input.IsAdultMode`。成人轮因此用成人模型（`AdultClassifier`→`AdultDirector`）预掷，缓存 `NarrativeInput.IsAdult = true` 的导演蓝图与 `AdultScribe` 书记官任务；点选命中后叙事实时按 `IsAdult` 选 `AdultNarrative`、书记官 `await` 成人 `ScribeTask`，全链路成人自洽。v4.6.0 的“成人轮不预计算 + `else if` 直接推送”分支已删除（成人轮 `precomputeTask != null` 走正常预计算推送：加载态→就绪回填可行性）。`LastSuggestedActions` 持久化仍跳过成人轮（避免断线恢复显示成人选项文本）。**切换语义对齐世界难度 override**：切换模式后当前已缓存选项按旧模式回放（最多一轮过渡），下一次预计算起切换；切换后手动输入立即生效。开场轮预计算固定传 `false`。

**叙事历史过滤**：规则不变——仅当本次为非成人且上次为成人时，跳过中间成人记录取最近5条非成人叙事；判定条件由 `classification.IsAdult` 改为 `isAdult`。

### 副本创建

```
选择副本 → Architect一次性生成完整世界 → 存储 → 游戏开始
```

## 文学引擎三层架构

叙事系统的核心设计，通过三层分离实现「通俗小说级」可读性（v4.5.0：目标从“小说家级文学品质”调整为“普通玩家一遍读懂”）：

```
Layer 0（建筑师AI，一次性）→ style_bible + motifs → 存入 session
Layer 1（导演AI，每轮）    → narrative_seed（剧情细纲）+ prose_guidance + dialogue_direction
Layer 2（叙事AI，每轮）    → 据细纲从零写正文，受文风圣经/意象/场景文风约束
```

### Layer 0：建筑师层（一次性，副本创建时）

- **文风圣经**：建筑师AI根据副本主题生成语调/句式/感官调色板/禁用陈词，存入 `GameDungeonSession.StyleBibleJson`
- **意象系统**：3-5个贯穿副本的核心意象，带初始状态和进化路径，存入 `GameDungeonSession.MotifsJson`
- 每轮叙事时由代码层解析并渲染为文本，注入叙事AI的 system prompt

### Layer 1：导演层（每轮）

| 输出字段 | 说明 | 对叙事质量的影响 |
|----------|------|------------------|
| `narrative_seed` | 250字剧情细纲（时间顺序事件条目，非散文也非一句话摘要） | 叙事AI拿它当骨架从零写正文，而非从干骨架“翻译”或从散文“扩写注水” |
| `prose_guidance` | 叙事节奏+玩家情绪落点+必须写清的信息 | GM 主动引导写法（而非电影镜头指令），禁慢镜头/特写/碎片化句式 |
| `dialogue_direction` | surface/subtext/conceal/body_language | 对话从"台词大意"变为有潜台词、有身体语言矛盾的深度场景 |

### Layer 2：叙事层（每轮）

- **写作四原则**：先把事说清楚 / 大白话优先 / 情绪直给 / 一个细节就够（v4.5.0 替代旧「创作三原则」）
- **硬性禁令**：禁精确物理数值/慢镜头分解/模糊指代，比喻与短段限量（详见 [叙事设计规范](narrative-design.md)）
- **对话写作原则**：利用多层对话指导写出有张力的对话场景
- **场景文风模块**：根据 scene_type 自动切换（战斗/对话/探索/恐怖/日常五种，每种给节奏/感受/禁忌）
- **文风圣经注入**：每轮从 session 读取，确保整个副本文风一致
- **意象追踪注入**：每轮从 session 读取，出现即可、一笔带过，不围着它写段
- **创作思考指南**：利用 thinking mode 在动笔前做5项事件驱动的决策
- **3个白话Few-shot示例**：入场/对话/战斗场景的可读叙事参考

### 数据流图

```
副本创建 ─┐
         ├→ 建筑师AI ─→ style_bible ──┐
         └→ motifs ──────────┐
                              ↓
每轮行动 ─┐               ┌──────────────────┐
         ├→ 导演AI ─→ narrative_seed + prose_guidance + dialogue_direction
         └→ 叙事AI ←── [style_bible + motifs + scene_style_module]
              └→ 流式叙事文本
```

## 节拍分档与章节档分段生成

为支撑「一个重大行动 = 小说一章（2000-3000字）」的体验，叙事长度由导演AI判定的**节拍分量**驱动，而非静态场景档或统一字数上限。

### 为何由导演AI分档（而非分类AI）

分档取决于**后果规模**（主线推进、tension、大事件连锁、关键揭示），这些都是**导演AI的输出**，分类AI在掷骰前看不到结果；且“分档”与“章节档解除克制”本质是同一个动作。故 v1 由导演AI单点决策。（v4.9.1 起 `player_choice_point` 不再作为 chapter 触发条件——它只标记“重大抉择点”供书记官选项节奏档使用，与叙事长度解耦）

### 三档定义

| 分档（`beat_scale`） | 触发情境 | 目标字数 | 渲染方式 |
|------|----------|----------|----------|
| `micro` | 普通对话 / 观察 / 日常琐事 | 200-600 | 单次生成 |
| `normal` | 探索推进 / 遭遇 / 支线事件 | 600-1200 | 单次生成 |
| `chapter` | 主线节点 / 战斗高潮(tension≥8) / 重大转折·关键揭示 / [推进型行动] | 1800-3000 | **分段生成** |

代码层 `AiCoordinatorService.ResolveNarrativeWordTarget` 按档位分层钳制字数（硬顶 800→3000）；未给 `beat_scale` 时回退到场景类型默认值。

### 章节档分段生成（逐分段调用）

chapter 档时 GM 额外输出 `beats`（4-8 个有序子节拍，每个含 `seed`/`beat_type`/`focus`，seed 写法同 narrative_seed）。`NarrativeAiService` 按分段逐段调用模型：

```
chapter 档：
  totalTarget = clamp(narrative_word_target, 1800, 3000)
  perBeat = max(300, totalTarget / beats.Count)
  for each beat_i in beats:
      messages = 突出本段细纲 seed + 附整章总细纲 + 已写正文（末尾≤1200字）
      stream/非stream 调用模型（目标 perBeat）
      逐 chunk yield，段间插入空行
```

- **连贯性**：前序正文作为上下文注入下一段，仅取末尾约 1200 字防止上下文膨胀。
- **零侵入**：Hub 与前端消费的仍是同一个流式 chunk 流，分段逻辑完全封装在 `NarrativeAiService` 内部，流式 / 非流式两条路径均支持。

### 章节档GM主动性（解除克制）

chapter 档允许 GM **主动制造大事件**（升级冲突 / 引入转折 / 驱动 NPC 重大行动），让「一章」真正有戏。保留两条底线：不替玩家做抉择；后果与判定结果/世界状态一致。

> 叙事字数参数详见 [叙事设计规范](narrative-design.md) 的「叙事长度控制」章节。

## 行动选项预计算机制（快进选择）

### 问题背景

每次玩家行动需经过 **分类AI → 骰子 → 前台导演AI → 叙事AI** 全链路（v4.0.0 后记账链与叙事并行，不计入等待）。玩家浏览完叙事后仍需等待下一次行动的推演，体验较差。

### 核心思路

**书记官AI**（v4.0.0 前为导演AI）每次记账时输出2个**建议行动选项**（`suggested_actions`），后台并行预计算这两个选项并缓存。预计算在**记账链尾启动**（仍在叙事流式期间），充分利用玩家阅读叙事的时间。

**预计算深度 = 导演层（L1，v4.1.0）**：预计算只跑到「分类AI → 骰子 → 前台导演AI」（DryRun），**不再预生成叙事正文**；书记官在 `ProcessPlayerActionAsync` 内 fire-and-forget 启动（`result.ScribeTask`），**保留句柄但不 await、不阻塞就绪**。就绪时刻从旧方案的「导演+叙事(~35s)」降到「导演(~15s)」，使下一轮选项在玩家读完本轮叙事时即已就绪（零等待）。玩家选择时：
- **点选预计算选项** → 秒推骰子结果 → **实时流式生成叙事**（`StreamNarrativeLiveAsync`）‖ 后台链 `await` 书记官回填后落库记账
- **输入自定义行动** → 丢弃缓存，走全链路

> **为何砍到导演层**：`NarrativeInput` 由前台导演产出、是叙事AI的必需输入，故导演层是「最小有用预计算深度」。再往下预生成叙事（旧 L3）虽能点选秒回放，但就绪晚 ~20s、且未选选项白烧整段叙事调用；砍到导演层后就绪达标，叙事延迟改由「点选后实时流式 + 玩家阅读」掩盖。

> **纯叙事轮也走完整记账（v4.3.0）**：书记官始终接收完整上下文并产出全部结构化字段，不设分类门控。若本轮确实无状态变化，书记官自然输出仅含 `summary` 的空 `world_state_changes`（只追加 change_history）；若存在信息获取类行动（查看新短信/阅读新文件），书记官可正确记录知识状态变更，避免叙事层与世界状态层脱节。

### 预计算深度演进（L3 → L1）

预计算「算到哪一层」历经三档，v4.1.0 定为 **L1（导演层）**：

| 档位 | 预计算深度 | 就绪时刻 | 点选后行为 | 未选选项浪费 |
|------|-----------|----------|-----------|--------------|
| L3（≤v3.6.0） | 导演 + **叙事全文** | ~35s | 秒回放缓存正文 | 整段叙事调用（章节档 4-8 次） |
| L2（备选） | 导演 + **书记官** | ~25s | 实时叙事 | 书记官 + 叙事 |
| **L1（v4.1.0）** | **仅导演层** | **~15s** | 实时流式叙事 + 后台 await 书记官 | 书记官（fire-and-forget 预跑，远小于叙事） |

**为何 L1 而非 L3/L2**：玩家零等待的条件是「下一轮选项就绪时刻 ≤ 玩家读完本轮叙事并想点的时刻」。预计算启动被「本轮记账完成」卡在 T≈28s（硬约束：下一轮分类AI门卫须读到本轮最新账本），故只能压缩**预计算深度**。L3 就绪 T≈63s、L2 T≈53s 均晚于玩家读完时刻（T≈35-45s），仍要干等；唯有 L1（导演完成即就绪，T≈43s）能贴上阅读节奏。

**章节档统一实时流式**：L1 下预计算不再产出任何叙事正文，`micro`/`normal`/`chapter` 三档**点选后一律走 `StreamNarrativeLiveAsync` 实时流式**（章节档内部仍按 `beats` 逐分镜续写，详见「章节档分段生成」）。旧 L3 的「章节档预取前 N 段 + 断点续写」机制（`ChapterPrefetchBeats` / `StreamChapterContinuationAsync` / `PrecomputedActionCache.NextBeatIndex`）**在 L1 下不再被点选路径调用——保留代码但不启用**。

### VIP模式（v4.9.0：按需 L3、预生成叙事秒回放）

L1 把叙事延迟交给「点选后实时流式 + 玩家阅读」掩盖，但首 token 仍有几秒延迟。**VIP模式** 是一个会话级手动开关（`IsVipMode`，前端 `StatusPanel` 下拉菜单 ⚡ 按钮切换，不持久化），开启后对 VIP 会话**按需把预计算深度从 L1 升回 L3**：预计算在导演层之后额外调 `NarrativeAiService.GenerateNarrativeAsync`（非流式，内部自动处理章节档整章生成）预生成叙事正文，写入 `PrecomputedActionCache.NarrativeText`（L1 改造后废弃但保留的字段）。点选时：

- **`cached.NarrativeText` 非空** → `StreamNarrativeAsync` 文本回放（首字延迟≈0，不消耗额外叙事 token）
- **为空（非VIP / VIP预生成未就绪 / 预生成失败 / 不可行短路）** → 自动降级 `StreamNarrativeLiveAsync` 实时流式（保证不卡住）

| 维度 | L1（默认） | VIP（按需 L3） |
|------|-----------|----------------|
| 预计算深度 | 仅导演层 | 导演 + 叙事全文 |
| 就绪时刻 | ~15s | ~35s |
| 点选后叙事 | 实时流式（首token几秒） | 文本回放（首字≈0） |
| 未选选项浪费 | 书记官（小） | 整段叙事调用（token约翻倍） |
| 未就绪时 | — | 降级实时流式 |

**与 L3 旧方案的区别**：VIP 不启用旧 L3 的章节档「预取前N段+断点续写」，而是用 `GenerateNarrativeAsync` 一次性生成整段正文后回放，逻辑更简单。开关透传链与 `IsAdultMode` 同构：经4个输入DTO透传，4处预计算入口（常规轮/缓存轮记账链尾、开场轮、重新开始轮）均带上 `IsVipMode`；`ProcessRestartSessionAsync` 签名加 `bool isVip`。**边界**：自由输入无预计算仍实时流式；不可行短路不预生成。

### 时序图（L1）

```
玩家输入行动A
  → 分类AI(思考OFF) → 骰子 → 前台导演AI(思考OFF，输出砍半)
  → 导演返回即开始叙事：
      ├─→ 主线：叙事AI(流式) → 推送叙事文本（玩家开始阅读）
      └─→ 副线：Task.Run 记账链（与流式并行）:
              await 书记官Task → 世界状态/NPC态度落库、回填 item_hints/suggested_actions
              → 若有 item_hints → 物资官记账落库 → 推送背包/情报更新
              → 启动预计算（分类AI门卫读到本轮最新账本）:
                    Task1: 选项1 → 分类AI→骰子→导演AI(DryRun) → 缓存结果1（书记官 fire-and-forget，不 await）
                    Task2: 选项2 → 分类AI→骰子→导演AI(DryRun) → 缓存结果2（书记官 fire-and-forget，不 await）
                    ※ 就绪 = 导演完成（~15s），不等叙事、不等书记官
  → 叙事流式完成，推送行动选项 [选项1] [选项2] + MarkOptionsShown(埋点①记时刻)
      若预计算已完成 → 按钮直接可点；否则先显示"加载中"，等完成后再切换为可点击
  → 玩家点击 [选项1]（埋点①算思考间隔）→ 缓存命中:
      ├─→ 秒推骰子结果
      ├─→ 主线：StreamNarrativeLiveAsync 实时流式叙事（埋点②记首token延迟）
      └─→ 副线：Task.Run 后台链（与流式并行）:
              await result.ScribeTask（回填 ScribeOutput/ItemHints/SuggestedActions，预跑大概率已完成）
              → ApplyCachedActionResultAsync 落库（世界状态/NPC态度/时段/交互计数，依赖 ScribeOutput）
              → 物资官记账 → 推送背包/情报 → 启动下一轮预计算
      叙事流完 → await 后台链 → 落库后读 session/character → 写叙事日志 → 推状态/选择点/时段/支线 → 推下一轮选项
  → 或输入自定义文本 → 丢弃缓存 → 走全链路
```

注：预计算 DryRun 内部不落库；书记官**不再在预计算阶段 await**（fire-and-forget 预跑，`ScribeTask` 句柄随缓存存活到点选）。点选路径在后台链**链首** `await result.ScribeTask` 回填状态字段后，才调 `ApplyCachedActionResultAsync` 落库（该方法依赖 `ScribeOutput`）。`RunScribeTaskAsync` 自建独立 DI scope 且自捕异常，句柄可安全跨请求存活。分类AI门卫与真实行动一视同仁地只读账本做三态判定；因记账已在预计算启动前完成，预计算读到的永远是含本轮变更的最新账本（根治"上轮获得的道具下轮选项被误判不可行"的死按钮问题）。

### DryRun模式

`ProcessActionInput` 新增 `DryRun` 布尔标志。`DryRun=true` 时：
- AI管线运行到**导演层**（分类、骰子、前台导演执行）；书记官 fire-and-forget 启动（`result.ScribeTask`）但**预计算不 await**；**叙事不生成**（L1 下改由点选后实时流式）。**VIP模式例外**（v4.9.0）：`isVip=true` 时在 DryRun 导演层之后额外调 `GenerateNarrativeAsync` 预生成叙事正文写入 `NarrativeText`（供点选秒回放），仍不落库
- **跳过所有数据库写入**：骰子记录、装备耐久扣除、世界状态变更、道具获取/消耗、NPC态度更新、时段推进、交互计数、紧张度（书记官输出仅回填到结果对象，不调 `ApplyChangesAsync`）
- 返回 `GameActionResult`（含 NarrativeInput、DiceResult、StateChanges、**ScribeTask 句柄**；ScribeOutput/ItemHints/SuggestedActions 待点选时 await 书记官后回填）
- `SkillCheckAsync` 新增 `dryRun` 参数，DryRun时计算骰子结果但不写DB、不扣装备耐久

### 缓存结构

使用 `ConcurrentDictionary` 做内存缓存（每个sessionId仅一个活跃玩家，无需分布式），TTL = 1 天（v4.2.0 由 1 小时延长，覆盖跨日挂起恢复）：

```csharp
public class PrecomputedActionCache
{
    public string ActionText { get; set; }        // 行动文本
    public string Hint { get; set; }              // 方向提示
    public GameActionResult? Result { get; set; } // 预计算结果（L1：含 NarrativeInput + ScribeTask 句柄）
    public bool IsFeasible { get; set; }          // 是否可行（不可行短路时=false，点选置灰）
    public string NarrativeText { get; set; }     // 【L1 废弃、VIP复用】非VIP恒为 ""；VIP模式预生成的叙事正文，点选时秒回放
    public int NextBeatIndex { get; set; }        // 【L1 废弃】恒为 -1（章节档统一实时流式）
    public DateTime CreatedAt { get; set; }       // 创建时间（1天TTL）
}

public class SessionActionCache
{
    public long SessionId { get; set; }
    public List<PrecomputedActionCache> Options { get; set; } // 恰好2个
    public bool IsReady { get; set; }              // 预计算是否全部完成（导演层就绪）
    public DateTime OptionsShownAt { get; set; }   // 埋点①：选项推送给前端的时刻
}
```

### 缓存命中回放逻辑（L1）

玩家点选缓存选项后，Hub执行 `SelectCachedAction`（`ProcessSelectCachedActionAsync`）：
0. **埋点①思考间隔**：入口读 `GetOptionsShownAt`，日志记录「点击时刻 − 选项推送时刻」(ms)（须在 `InvalidateCache` 前读）
1. **背包超重门卫**：与常规行动路径一致，负重 ≥100% 直接阻断（缓存路径不豁免）
2. 秒推骰子结果（如有）→ 尽早失效旧缓存（防重入）
3. **叙事实时流式（主线）**：`NarrativeInput != null` 时调 `StreamNarrativeLiveAsync` 实时生成并流式推送（埋点②记首 token 延迟）；不可行短路（`NarrativeInput == null`）时回放拒绝文案
4. **后台链（副线，`Task.Run` 与叙事并行）**：
   - 链首 `await result.ScribeTask` → 回填 `ScribeOutput`/`ItemHints`/`SuggestedActions`/`QuestProgress`
   - `ApplyCachedActionResultAsync` 落库（世界状态/NPC态度/时段/交互计数，依赖 `ScribeOutput`）
   - 若有 `item_hints` → 物资官依蓝图记账 → 推送背包/情报更新
   - 启动下一轮预计算（读最新账本）
5. 叙事流完 → `await` 后台链收尾 → **落库后**读 `session`/`character` → 写叙事日志（用落库后 `InteractionCount`）→ 构建下一轮选项 DTO（此时 `SuggestedActions` 已回填）
6. 推送游戏状态、`IsChoicePoint`、时段变化、支线任务 → 恢复输入
7. 推送下一轮建议选项 + `MarkOptionsShown`（埋点①记时刻）；若预计算已跑完则按钮直接可点，否则显示加载态

**缓存过期/未命中回退**：前端点击选项时随请求携带选项文本（`SelectCachedActionInput.ActionText`）。缓存过期（1天TTL）或未命中时，直接以该文本作为玩家输入走 `ProcessPlayerActionAsync` 常规全链路（等同手动输入，保留成人模式开关状态），不再使用占位行动；文本不可得（旧客户端）时提示“该行动选项已过期，请直接输入你的行动”。

> **段落保留修复**：缓存回放的分块器 `SplitNarrativeToChunks` 早期会 `Trim()` 掉 `\n\n`，导致章节档前缀回放时段落塌成一堵墙。已改为仅去除水平空白（空格/制表符）、不再按 `\n` 拆分，保留段落分隔（非章节档回放同样受益）。

### 挂起/断线恢复：选项持久化与分层降级（v4.2.0）

内存预计算缓存在服务重启或 TTL 过期后丢失，且挂起副本（`Status = 4`）不清缓存。为使玩家恢复副本时仍能续上「离开前最后一轮」的行动选项，新增持久化字段与分层降级：

- **持久化字段**：`GameDungeonSession.LastSuggestedActions`（`nvarchar(max)`，存最后一轮 2 个选项的 JSON）。常规轮/点选轮/开场轮在启动预计算前写入；放弃会话（`Status = 2`）与重新开始置空。
- **恢复跳过回归旁白（方案A）**：恢复副本时不再推送硬编码回归旁白，直接续上离开前最后一轮叙事；历史叙事由前端 `Lobby.resumeDungeon` 从 `checkActiveSession` HTTP 接口预先恢复（`BuildResumedResult` 的 `resumeNarrative` 置空）。
- **分层降级**（`CheckActiveSessionAsync` 第8步 + `ProcessSelectDungeonAsync` 步骤8）：
  - `ActiveSessionCheckOutput.SuggestedActions` 携带 DB 中的选项文本（不含可行性）；Hub 在 `DungeonReady` 后以文本推送（`IsComputing = false`、`IsFeasible` 默认 true），**不重跑预计算**（省 token）。
  - 玩家点选后由 `ProcessSelectCachedActionAsync` 自动分流：内存缓存命中（1天TTL内）→ 秒响应；未命中 → 以 `ActionText` 走常规全链路。
- **跨层依赖约束**：`DHY.Game.Core` 不引用 `DHY.Game.AI`，故 `CheckActiveSessionAsync`（Core 层）只读 DB 文本选项、不查内存缓存；可行性分流交由 Hub 层的 `ProcessSelectCachedActionAsync` 处理。

### 缓存回放持久化无分类门控

`ApplyCachedActionResultAsync` 的持久化不再受分类AI判定守卫（v4.3.0 移除门控，v4.4.0 删除 `GameActionResult.NeedsStateChange` 死字段）：书记官输出了 `WorldStateChanges` 就落库，时段推进由 `StateChanges.TimeAdvanced` 守卫，交互计数/紧张度每轮必更新。

### 书记官输出扩展

书记官每次记账必须输出 `suggested_actions`（恰好2个）：

```json
"suggested_actions": [
  {"action_text": "向酒馆老板打听消息", "hint": "社交互动方向"},
  {"action_text": "悄悄跟踪那个可疑的身影", "hint": "潜行探索方向"}
]
```

规则：每次记账必须输出恰好2个行动建议，方向应有差异性，文本简洁（15字内）。v4.3.0 起所有常规轮（含纯叙事轮）均走完整记账，书记官在产出选项的同时如实记录状态变更（无变化时仅输出 summary）。

### 全场景行动选项覆盖（v4.2.0 引入，v4.3.0 收窄）

为达成「每一轮都有行动选项」的 UX 一致性：
- **常规轮（含纯叙事轮）**：v4.3.0 起始终走完整书记官，自然产出 `suggested_actions` + 状态记账，不再使用 SuggestionsOnly。
- **无导演蓝图的场景**仍走 SuggestionsOnly 轻量模式（`ScribeInput.SuggestionsOnly = true`，模板 `scribe_suggestions_system.txt`），只产出 `suggested_actions`（恰好2个）、**不记账不落库**：

| 场景 | 既定事实（DirectorFacts）来源 | 接入点 |
|------|------------------------------|--------|
| 首次进入 / 同题异卷重玩 | 开场叙事 | `ProcessSelectDungeonAsync` 步骤9（`!IsResumed`） |
| 重新开始 | 开场叙事 | `ProcessRestartSessionAsync`（`DungeonReady` 后） |
| 不可行短路（`infeasible`） | 拒绝叙事 | `AiCoordinatorService` 短路分支塞 `ScribeTask` |

- **开场类场景**通过 `GenerateSuggestionsOnlyAsync(sessionId, playerAction, directorFacts, characterName)` 生成（自建 scope 调 `ScribeAsync`，失败返回 null 降级、不阻断副本启动）；装配由 `BuildSuggestionsOnlyInputAsync` 完成：以开场叙事为 `[本轮既定事实]`，保留主线目标引导选项朝主线推进，不暴露支线/隐藏内容避免剧透。生成后与常规轮一致：持久化 `LastSuggestedActions` + 启动预计算 + 推送选项。
- **不可行短路**复用 `RunScribeTaskAsync`（已有 `result` 对象），以拒绝叙事为既定事实产出「补救方向」选项；仅真实行动（`!DryRun`）时生成，预计算 DryRun 只关心可行性判定无需补救选项。

**成人轮（`IsAdult`）**：v4.6.0 起走完整链路，书记官（`scribe_adult_system` / `scribe_adult_suggestions_system`）照常产出2个选项并推送；**v4.8.0 起同样做预计算**（预计算继承会话级成人模式 `input.IsAdultMode`，用成人模型预掷，点选秒响应），与常规轮完全一致。切换成人模式后当前缓存选项按旧模式回放（最多一轮过渡），下一次预计算起切换。

**刻意无选项的场景**（设计接受，不补）：背包超载（≥100% 入口阻断）、缓存未命中且选项文本不可得（旧客户端）、副本结束态（完成/放弃/死亡）。

至此，除上述刻意场景外，**全场景每轮均产出2个行动选项**。

### 前端交互

- 叙事流式输出完成后显示2个选项按钮
- 预计算进行中时按钮显示加载指示器（禁用）
- 预计算完成后按钮变为可点击
- 点击按钮 → 调用 `SelectCachedAction`，秒级响应
- 输入框提交自定义文本 → 自动清除选项，走常规流程
- 选项在以下时机清除：新一轮行动开始、选择选项后、自定义输入后

### TOCTOU竞态防护

预计算完成后写入缓存前，检查缓存是否仍有效（玩家可能在预计算期间发起新行动触发 `InvalidateCache`）。失效则丢弃结果，避免陈旧数据写入。

### 风险与注意事项

1. **AI调用成本（L1）**：每次行动额外增加 2 选项 ×（分类 + 骰子 + 导演 + **书记官 fire-and-forget**）。叙事**不再预生成**（改点选后实时流式，只算被选中的那条），故相比旧 L3 省掉了 2 选项的叙事白烧；代价是未选选项的书记官 token 被浪费（方向 B，远小于叙事）
2. **缓存一致性**：预计算期间玩家输入已禁用，不存在状态漂移风险
3. **缓存过期**：1天TTL（v4.2.0 由 1 小时延长），过期后点选选项以文本走常规全链路；挂起/断线恢复另有持久化兜底（详见「挂起/断线恢复选项持久化」）
4. **异常降级**：预计算失败时不影响正常游戏流程，按钮保持禁用，玩家可手动输入
5. **书记官句柄跨请求存活**：预计算 fire-and-forget 启动的 `ScribeTask` 随缓存存活到点选，`RunScribeTaskAsync` 自建独立 DI scope + 自捕异常，不依赖预计算请求的 scope 生命周期
6. **点选后叙事延迟**：L1 下叙事改实时流式，首 token 延迟（埋点②）成为点选后的可感知等待；若实测偏大，评估 L1.5 备用方案

### 延迟埋点（v4.1.0）

为评估 L1 实效、并决定是否启用 L1.5，新增两个 `LogInformation` 埋点：

| 埋点 | 位置 | 记录内容 | 用途 |
|------|------|----------|------|
| ①玩家思考间隔 | 推送选项时 `MarkOptionsShown` 记 `OptionsShownAt`；点选入口 `GetOptionsShownAt` 算差值 | 「点击时刻 − 选项推送时刻」(ms) | 判断玩家读完叙事后是否仍需干等（间隔过短=选项未就绪即在等） |
| ②叙事首 token 延迟 | `StreamNarrativeLiveAsync` 首个 chunk | 「流式开始 → 首 token」(ms) + sessionId | L1 下点选后的可感知等待；评估 L1.5 收益 |

### L1.5 备用方案（暂不实施，留档）

**设想**：预计算在导演层就绪后，继续把被缓存选项送入叙事AI，**叙事首 token 出现即开放点选**；玩家点选后直接接管已缓冲的叙事流，同时启动书记官路径。

**物理事实**：C 选项叙事首 token 时刻 = C 导演完成 + 叙事首 token 延迟（~3s），预生成**不能让首字更早**，只能让它「点时已好」。

**收益/代价评估**：
- 收益上限 ≈ 叙事首 token 延迟（~3s），且**仅在玩家慢点**（读完 A 叙事时 C 首 token 尚未自然到达）时兑现；玩家快点时反而要等 C 首 token（就绪晚 ~3s）
- 代价：退回叙事预生成的 token 浪费 + 缓冲流接管的复杂度（已缓冲 chunk 重放、与实时流拼接）

**触发条件**：埋点数据显示「玩家思考间隔普遍偏长（慢点） **且** 叙事首 token 延迟明显 > 3-5s」时，再评估实施。当前先跑 L1 收集数据。

---

## 核心架构决策

1. **分类AI承担裁判职责** — 技能判定（DC/技能/优劣势）由分类AI输出，代码层掷骰，导演AI不再参与判定参数设定
2. **行动意图提炼稳定推演** — 分类AI将玩家变化多端的输入（口语/行动/混合）提炼为标准化意图（格式：`行动类别·动词：目标描述`），导演AI以意图为推演锡点同时保留原文供NPC对话反应
3. **导演AI知成败后推演** — 骰子结果在导演AI之前确定，导演的所有输出（narrative_seed/NPC行为/节奏决策）基于实际成败精准生成
4. **常规行动跳过掷骰，不跳过导演** — `is_routine=true` 仅使分类AI不输出 judgment（从而无掷骰），行动仍走完整导演→叙事‖书记官链路；导演侧不再接收该标记（v4.4.1）
5. **导演拆分为前台创作 + 后台记账**（v4.0.0） — 按**消费者**拆分而非按内容领域：叙事AI只消费前台字段，故前台导演只产出创作字段置于关键路径（输出砍半），状态账目交给与叙事并行的书记官；子导演按“NPC/文笔”等领域拆分的方案已否决（子导演仍堵关键路径）
6. **书记官失败不阻断叙事** — 重试1次后仍失败则本轮状态不变、叙事照常推送（玩家体验优先于账目完整，异常记 error 日志）
7. **常规轮始终完整记账（v4.3.0）** — 书记官不设分类门控，始终走完整模式（接收全量上下文、产出全部结构化字段）。纯叙事轮若确实无变化，书记官自然输出仅含 summary 的空变更（只追加 change_history）；信息获取类行动（查看新短信/阅读新文件）可被正确记账，根治叙事-状态脱节。SuggestionsOnly 仅保留给无导演蓝图的场景（开场轮/不可行短路）
8. **结构化JSON输出** — 确保输出可解析、可验证
9. **NPC档案卡仅限核心NPC** — 避免资源浪费
10. **100万token上下文窗口，不主动压缩历史** — 最大化叙事一致性
11. **局面快照统一世界状态** — 分类AI和导演AI共用结构化快照，分类AI读当前状态（无历史），导演AI读全量（含 change_history）
12. **书记官蓝图是资产变更的唯一来源** — 书记官依前台导演既定事实记账、叙事仅扩写；物资官依 `item_hints` 落库，不读叙事正文；无 hint 即无需记账（零成本门控）
13. **单门卫原则** — 分类AI是唯一综合门卫（只读账本 + 三态可行性 + 普通资源放过），预计算与真实行动一视同仁

## 关键路径与并行化（延迟治理）

玩家可感知的等待 = **分类AI + 掷骰 + 前台导演AI + 叙事首 token**，其余环节均应移出关键路径：

| 环节 | 是否在关键路径 | 说明 |
|------|----------------|------|
| 分类AI | 是 | 思考 OFF，轻量路由 |
| 掷骰（代码） | 是 | 毫秒级 |
| 前台导演AI | 是 | 思考 OFF + 输出砍半，本阶段优化重点 |
| 时段推进（代码） | 是 | 仅 DB 更新，毫秒级，留在前台 |
| 书记官AI | 否 | 与叙事流式并行，Hub 在播放期间 `await` |
| 物资官AI | 否 | 接在书记官之后，同为并行记账链 |
| 预计算 | 否 | 记账链尾启动，读最新账本；L1 只算到导演层（不等叙事/书记官） |

并行安全约束：
- 书记官 Task 使用**独立 DI scope**（`IServiceScopeFactory`），不依赖调用方 scope 生命周期
- Task 内部**自捕异常**，永不抛给调用方；Hub 读 `ItemHints`/`SuggestedActions`/`StateChanges` 前**必须 `await ScribeTask`**（await 的 happens-before 保证回填可见）
- 世界状态落库沿用**递增前捕获的轮次**（`applyRound`），与交互计数递增解耦

## 判定信息流详解

### 职责分层

| 环节 | 执行者 | 输出 |
|------|--------|------|
| 三态可行性门卫 + 是否需要检定 + 技能 + DC + 优劣势 | 分类AI | `feasibility` + `judgment` JSON |
| D20掷骰 + 调整值计算 + DC比较 | 代码层（规则引擎） | `GameDiceRollRecord` |
| 基于成败的剧情细纲 + 世界反应 + 节奏决策 | 前台导演AI（GM） | `DirectorOutput`（无状态字段） |
| 依既定事实产出状态账目 + 物资清单 + 建议选项 | 书记官AI | `ScribeOutput` |
| 依蓝图记账落库（背包/已知情报） | 道具AI（物资官） | `LedgerDelta` |
| 将细纲写成正文 | 叙事AI | 流式文本 |

### DC合法性校验

代码层对分类AI输出的DC进行运行时校验：`Dc > 0` 才执行掷骰。无效DC（LLM输出异常）记录告警日志并跳过本次检定。

### 世界难度修正

分类AI给出的DC为原始值，代码层自动叠加副本世界难度修正（`DifficultyModifier`）计算有效DC。分类AI设定DC时**不应考虑**副本难度等级，仅基于当前情境评估。

### 优劣势判定上下文

分类AI从**局面快照**中读取 `player_position`（玩家位置/姿态）、`environment`（环境条件）、`npc_states[].awareness`（NPC警觉度）等结构化信息判断优劣势。若无法从现有快照明确判断，默认设为 `false`（普通投掷）。

## 运行机制

### 输入处理

- 玩家输入经分类AI提炼为标准化行动意图（`action_intent`），导演AI同时接收意图和原文
- 意图为推演锡点，原文保留对话细节供NPC反应参考

### 模型配置

六角色模型与思考模式**独立配置**（`GameAiOptions.Models`），按职责差异化选型：

| 角色 | 模型 | 温度 | 思考模式（要求值） | 选型理由 |
|------|------|------|----------|----------|
| **Classifier** | `deepseek-v4-flash` | 0.85 | **false** | 轻量路由任务且处在关键路径，思考链无实质收益 |
| **Director**（前台） | `deepseek-v4-flash` | 0.7 | **false** | 关键路径上唯一的 LLM 大头；输出 schema 砍半后解码耗时大幅下降（`qwen3.7-max` 为备选，按实测质量定夺） |
| **Scribe** | `deepseek-v4-flash` | 0.3 | **false** | 纯结构化记账，低温保精确（任务名需精确匹配）；与叙事并行，延迟被阅读掩盖 |
| **Quartermaster** | `deepseek-v4-flash` | 0.3 | **false** | 纯记账数值补全，无需创作与推理 |
| **Narrative** | `deepseek-v4-flash` | 0.85 | true | 真·文学创作场景，思考链有实质收益；流式推送下延迟被玩家阅读掩盖 |
| **Architect** | `qwen3.7-max` | 0.8 | true | 一次性生成完整副本（耗时 120-150s 可接受），强规划能力有真实收益 |
| **AdultNarrative** | `grok-4-latest`（Poixe） | 0.85 | true | 成人叙事专用通道 |
| **AdultDirector**（前台） | `grok-4-latest`（Poixe） | 0.7 | true | 成人轮前台导演（v4.6.0），与 Director 同职责、仅模板/模型不同 |
| **AdultScribe** | `grok-4-latest`（Poixe） | 0.3 | true | 成人轮书记官（v4.6.0），完整记账与仅选项两模板共用此模型 |

- 思考模式取舍原则：**关键路径上的角色一律关思考**，仅叙事/建筑师等“延迟可被掩盖或离线”的角色保留思考链
- 缺少配置节点时 `AiModelFactory` 回退为 `qwen-plus` + 思考 ON，**新增角色必须同步补配置**（否则静默跑成高延迟路径）
- ⚠️ `GameAiOptions.json` 位于 `DHY.FrameWork.Application/Configuration/`，**不入 Git**（各环境各自维护）且可由后台「AI模型配置」在线改写——上表为**架构要求值**，做延迟实测前必须先核对目标环境的实际配置
- MaxTokens / ThinkingBudget 由服务端自主决定
- **不接受客户端配置**

### 前端交互

- 建筑师一次性生成时：前端显示"世界生成中"动画
- 每次行动时：前端显示"世界推演中"动画
- 叙事输出：流式推送

### 上下文策略

- 100万token窗口，不压缩历史
- **思考模式按角色配置**（v4.0.0）：关键路径角色（分类/前台导演/书记官/物资官）关思考，叙事与建筑师保留思考链（详见「模型配置」）

## NPC语言一致性

叙事AI前**强化注入NPC语言卡片**：

- 在调用叙事AI前，将当前场景涉及的NPC语言卡片注入Prompt
- 确保NPC说话风格始终一致

## 上下文注意力权重策略

关键信息置于上下文末尾（模型对末尾注意力最强）。

### 排列顺序（从头到尾）

1. 系统指令
2. 副本设定
3. 历史对话
4. NPC档案卡
5. **局面快照（分类AI无历史，导演AI含历史）**
6. 主线进度
7. 玩家背包
8. 蓝图
9. NPC语言约束

## 后处理验证层

### 性质

纯代码规则检查（**非AI**）

### 检查项

| 检查类型 | 说明 |
|----------|------|
| 规则违反 | 判定规则是否被正确执行 |
| 信息泄露 | 是否暴露了不该让玩家知道的信息 |
| 字数越界 | 是否超出场景对应的字数范围 |
| NPC矛盾 | 是否与NPC档案卡设定冲突 |
| 禁止表达 | 是否使用了禁止的表达方式 |

## 流式输出与状态更新分离

### 执行顺序

1. **先**流式推送叙事文字给玩家
2. **同时**后台记账链（书记官 → 物资官 → 推送 → 预计算）并行执行，叙事流完成后 `await` 收尾

### 设计目的

- 玩家即时看到叙事（低延迟感知）
- 状态变更不阻塞叙事输出；反之，叙事的播放时间反过来掩盖了记账与预计算的延迟

## 角色再定位机制

### 触发条件

- 每 5-10 个交互
- 时段切换
- 长休息后

### 动作

重新注入完整角色状态快照

### 目的

防止长会话中角色设定漂移

## 附加决策

- **禁用Inline XML状态标记** — 状态变更严格代码层处理
- **思考模式按角色差异化** — 关键路径角色 `EnableThinking = false`，叙事/建筑师 `= true`
- **Token配置服务端自决** — MaxTokens / ThinkingBudget不接受客户端配置
- **导演AI/书记官结构化输出规范** — 严格JSON字段约束，不允许自由文本

## 资产账本与道具生成（item_hints → 物资官记账）

### 信息流

```
分类AI门卫(只读账本,三态) → 掷骰 → 前台导演AI(推演，产出既定事实)
  → [ 叙事AI扩写流式
    ‖ 书记官(依既定事实产 item_hints[is_key]) → 道具AI依蓝图记账落库 → 预计算读新账本 ]
```

### 书记官 item_hints（权威蓝图）

书记官在本轮发生资产变更时输出 `item_hints` 数组（v4.0.0 前由导演AI输出），每条含：

| 字段 | 说明 |
|------|------|
| `change` | 变更类型：获得/消耗/失去/情报 |
| `category` | 物理道具 / 无形资产（情报、线索、号码、承诺等） |
| `name` | 资产名称 |
| `note` | 补充说明（如情报内容概要） |
| `is_key` | 是否关键剧情道具/重要资产 |

**hint 纪律**：书记官仅 hint **关键剧情道具/重要资产**（值得进背包/账本的）；普通易耗品、环境常见资源不 hint（交给叙事描写、由门卫宽松放行）；无变更输出 `[]`。书记官只能记前台导演已写明的资产变动，**不得自行发明道具**。

### 物资官记账（数值补全）

物资官逐条落实蓝图，为物理道具 AI 扩展出完整数值字段：

| 字段 | 说明 |
|------|------|
| `item_name` | 道具名称 |
| `item_type` | 武器/防具/消耗品/关键道具/杂物 |
| `weight` | 重量单位（匕首=1, 手枪=3, 防弹衣=5） |
| `attribute_bonus` | 属性加值（普通1-2，精良3-4，传说5+） |
| `linked_attribute` | 关联属性 (STR/DEX/CON/INT/WIS/CHA) |
| `max_uses` + `is_unlimited` | 使用次数设定（冷兵器无限；火器3-5；消耗品1） |

落库：物理道具调 `InventoryService`（获得/消耗/丢失）；无形资产调 `KnownAssetService`（登记/作废）。Hub 在记账后推送 `UpdateInventory` / `UpdateKnownAssets`（v4.0.0 起发生在叙事流式期间，而非叙事之前）。

### 账本生命周期

已知情报账本（`GameKnownAsset`）随会话生命周期清理：**重开副本**（保留世界/副本设定）时软删本会话账本，**放弃会话**时硬删（清理失败仅告警，不阻断放弃流程）；前端 `clearSession` / `clearNarrativeHistory` 同步清空线索区，避免展示旧周目线索。背包等其余会话数据沿用既有清理流程。

> 历史兼容：旧版导演输出 `acquired_items/consumed_items` 的反序列化容错保留（自动转为 item_hints），但内联应用链路已退役。

## DC标尺校准

分类AI设定DC时必须参考以下标尺（适用所有行动类型：战斗对抗、潜行渗透、社交说服、机关破解、环境生存等）：

| DC区间 | 难度描述 |
|--------|----------|
| 5-7 | 几乎无难度（条件极有利，无实质对抗/阻碍） |
| 8-10 | 轻度挑战（条件有利，阻碍微弱） |
| 11-13 | 普通难度（条件中性，难度与能力相当） |
| 14-16 | 偏难（条件略不利，障碍占优或情境复杂） |
| 17-20 | 极难（条件明显不利，多重不利因素叠加） |
| 21-24 | 近极限（凡人能力边界，需极致发挥+运气） |
| 25+ | 神话级（超越常理，基本只有nat20才有微弱可能） |

核心原则：DC反映"在当前情境下，该行动客观上有多难成功"，而非仅衡量对手战斗力。

### 情境调整因素

设定DC时应综合考虑以下情境因素（不限于战斗）：

- **装备/工具差距**：拥有合适工具或装备优势时DC降低2-4，反之升高2-4
- **信息优势**：掌握对方弱点、地形情报、目标心理时DC降低1-3
- **环境条件**：光线、噪音、天气、地形等有利/不利因素影响DC ±1-3
- **时间压力**：紧迫时限或需要安静等待的场景升高DC 1-3
- **对象态度/警觉**（社交/潜行）：友善/放松降低DC，敌对/高度警惕升高DC
- **多重因素叠加时**：DC可跨档位调整，但需合理说明

---

## 行动可行性判定（三态）

分类AI在每次行动时同时做三态可行性判定（`feasibility`），先做**引用识别**（玩家引用的是哪个具体资产），再对照**可用资产清单**（背包物理道具 + 已知情报/线索账本）与当前场景/世界观判定。

### 三态判据

| 态 | 判据 | 后续处置 |
|----|------|----------|
| `feasible` | 资产在清单中有明确条目；或场景/世界观有合理依据；或**普通资源放过**（火把、绳子等普通消耗品/环境可得资源，即使清单查无也放行） | 正常走标准流程 |
| `uncertain` | **仅限关键剧情道具/重要资产**：疑似此前剧情中获得/听闻（钥匙、凭证、纸条内容等）但清单查无 | 照常走导演流程，由导演AI结合完整剧情做叙事终审 |
| `infeasible` | 清单、场景、世界观中**均无任何依据**的凭空虚构（典型：凭空掏出炸弹） | 直接返回拒绝叙事，不进入导演流程 |

原则：优先 feasible/uncertain 放行、让导演AI处理细节，只有明显凭空虚构才判 infeasible。

### 短路逻辑

当 `feasibility=infeasible` 时，Coordinator 直接返回拒绝叙事，跳过导演AI、骰子、记账、状态变更等全部环节，节省大模型调用和数据库查询。**v4.2.0 起**：短路分支额外以 SuggestionsOnly 轻量模式（以拒绝叙事为既定事实）产出 2 个「补救方向」选项（仅 `!DryRun`），并修复了 `ProcessPlayerActionAsync` 此前未推送拒绝叙事的缺陷——玩家现在会看到「拒绝叙事 + 补救选项」而非无反馈。

### 设计考量

将可行性判定放在分类AI而非导演AI（**单门卫**）：
- **门卫原则**：在入口拦截荒诞行动，避免走完整个导演管线
- **成本节省**：分类AI是轻量模型，导演AI前还需查世界状态/NPC/叙事/背包
- **只读不写**：门卫只读账本做判定，不写库、不读叙事，预计算 DryRun 与真实行动一视同仁
- **宽松不误杀**：普通资源放过 + uncertain 交导演终审，只有明显凭空虚构才拒绝

---

## 局面快照（SituationSnapshot）

世界状态从旧的平铺JSON升级为结构化局面快照，统一服务于分类AI、前台导演AI与书记官AI。

### Schema

```json
{
  "world_setting": { "era": "...", "technology_level": "...", "culture": "...", "geography": "..." },
  "location": "醉金楼VIP包厢",
  "current_day": 1,
  "current_segment": "上午",
  "player_position": "坐在蛇哥对面",
  "player_status": "正常，腰间别着手枪（未暴露）",
  "environment": "昏暗灯光，烟雾弥漫，门外有一个马仔",
  "npc_states": [
    { "npc_id": "蛇哥", "awareness": "警觉但未敌对", "status": "正常", "attitude": "试探性" }
  ],
  "active_conditions": [],
  "flags": [],
  "change_history": [
    { "round": 0, "summary": "玩家前往安全屋，取走通讯器和备用物资" },
    { "round": 1, "summary": "玩家前往醉金楼，与蛇哥会面" }
  ]
}
```

### 各AI读取方式

| AI | 读取内容 | 说明 |
|----|----------|------|
| 分类AI | 快照（**过滤** `change_history`） | 只需当前状态判优劣势/可行性 |
| 前台导演AI | 快照（**保留** `change_history`） | 需历史推演下一步 |
| 书记官AI | 快照（当前状态）+ 前台导演既定事实 | 以当前快照为基准计算差量，只输出变化字段 |
| 叙事AI | `recentNarrative`（原始叙事文本） | 保持文风连贯性 |

### world_state_changes 输出规范

**书记官**每轮输出 `WorldStateChangesDto`（结构化对象，v4.0.0 前由导演AI输出），代码层合并到快照：

| 字段 | 说明 | nullable |
|------|------|----------|
| `location` | 位置变化 | 是 |
| `player_position` | 玩家位置/姿态变化 | 是 |
| `player_status` | 玩家状态变化 | 是 |
| `environment` | 环境条件变化 | 是 |
| `npc_states[]` | NPC状态变化（按npc_id合并） | 是 |
| `active_conditions` | 活跃状态效果（全量替换） | 是 |
| `flags` | 关键标记（全量替换） | 是 |
| `summary` | 本轮事件摘要（写入change_history） | **否** |

核心规则：仅变化的字段才输出，未变化保持上一轮值。`summary` 必出。落库时沿用**交互计数递增前捕获的轮次**（`applyRound`），避免并行记账与计数递增竞争导致 `change_history` 轮次错位。

---

## 版本历史

| 版本 | 日期 | 变更类型 | 说明 |
|------|------|----------|------|
| 4.9.1 | 2026-09-22 | **提示词调优** | `player_choice_point` 与 `beat_scale` 解耦 + 判定收紧（`director_front_system.txt` / `director_adult_front_system.txt`）：①删除「`player_choice_point=true`→强制 chapter」触发条件，`player_choice_point` 不再影响叙事分档，仅作书记官选项节奏档（`IsKeyMoment`）判据之一；`beat_scale` 分档依据回归内容分量（主线节点/tension≥8/重大转折·关键揭示/[推进型行动]）。②`player_choice_point` 语义明确为「有实质后果、改变剧情走向的重大抉择点」，默认 false，true 条件加不可逆后果限定，新增负面清单（普通寒暄询问/无分歧常规推进轮/`tension<6` 无转折普通轮/可自由输入常规行动轮一律 false）。背景：原「任一条件满足即 true」过宽，紧张度 5~6 的普通轮也频繁置 true 并顶成 chapter，使选项几乎恒为细粒度、粗粒度推进档形同虚设。代码层 `AiCoordinatorService.IsKeyMoment` 未改动 |
| 4.9.0 | 2026-09-19 | **性能/体验** | VIP模式（预计算阶段预生成叙事、点选秒回放）：新增会话级手动开关 `IsVipMode`（前端 `StatusPanel` 下拉菜单 ⚡ 按钮切换、`gameStore.isVipMode`，不持久化、刷新归零，与 `IsAdultMode` 同构）。开启后预计算在导演层之后额外调 `NarrativeAiService.GenerateNarrativeAsync`（非流式，内部自动处理章节档）预生成叙事正文，写入 `PrecomputedActionCache.NarrativeText`（复用 L1 改造后废弃字段），相当于对 VIP 会话按需把预计算深度从 L1 升回 L3。点选时 `ProcessSelectCachedActionAsync` 优先判 `cached.NarrativeText` 非空→`StreamNarrativeAsync` 文本回放（首字延迟≈0），为空（非VIP/未就绪/预生成失败/不可行短路）→降级 `StreamNarrativeLiveAsync` 实时流式。`PrecomputeAsync`/`PrecomputeSingleOptionAsync` 加 `bool isVip`；`IsVipMode` 经4个输入DTO（`PlayerActionInput`/`SelectCachedActionInput`/`SelectDungeonInput`/`RestartSessionInput`）透传，4处预计算入口（常规轮/缓存轮记账链尾、开场轮、重新开始轮）均带上，`ProcessRestartSessionAsync` 签名加 `bool isVip`。代价：叙事 token 约翻倍（2选项各生成一次只用1个）+ 就绪 ~15s→~35s。边界：自由输入无预计算仍实时流式。编译验证：后端 0 错误 / 30 既有警告未新增，前端 `vue-tsc` EXIT=0 |
| 4.8.0 | 2026-09-19 | **机制对齐** | 成人轮预计算继承会话级成人模式（对齐世界难度 override 的“下一轮预计算起生效”语义）：废除 v4.6.0“成人轮不做预计算”门控，`PrecomputeAsync`/`PrecomputeSingleOptionAsync` 新增 `bool isAdult` 参数透传到 `ProcessActionInput.IsAdultMode`；`GameSessionHub` 两处预计算启动点传 `input.IsAdultMode`，成人轮用成人模型（`AdultClassifier`→`AdultDirector`）预掷、缓存 `NarrativeInput.IsAdult=true` 蓝图与 `AdultScribe` 书记官任务，点选按 `IsAdult` 选 `AdultNarrative`；删除两处“成人轮直接推送选项”的 else if 分支；`LastSuggestedActions` 持久化仍跳过成人轮。切换后当前缓存选项按旧模式回放（最多一轮过渡），下一次预计算起全链路切换；开场轮固定传 `false` |
| 4.7.0 | 2026-09-18 | **机制重定义** | 玩家自由输入语义重定义（目标声明制）：前端自由输入框不再作为“本轮行动”进入分类/导演/叙事链路，而是作为“中长期目标声明”仅写库（不触发任何AI、不失效预计算缓存、不刷新当前选项）；下一轮书记官构造 `ScribeInput` 时读取 `GameDungeonSession.CurrentPlayerGoal` 填入 `PlayerGoal`，`ScribeAiService` 在 `[本轮既定事实]` 后、`[选项节奏档]` 前注入 `[玩家当前目标]` 消息对，让产出的 `suggested_actions` 围绕目标生成。后端：`GameDungeonSession` 新增 `CurrentPlayerGoal` 列（nvarchar(max) nullable）；`GameHubDtos` 新增 `SetPlayerGoalInput`；`GameSessionHub` 新增 `SetPlayerGoal` 方法（写库+200字兼底截断+推送 info 反馈）；`ScribeInput` 新增 `PlayerGoal`；`AiCoordinatorService` 常规路径与 `BuildSuggestionsOnlyInputAsync` 两处注入；4 个书记官模板各加一条“两档通用规则”；`DungeonReadyDto`/`ActiveSessionCheckOutput` 同步回填 `CurrentPlayerGoal` 支持断线续玩/跨设备一致性。前端：`types/game.ts` `DungeonReady`/`ActiveSessionResult` 加 `currentPlayerGoal`；`gameStore` 新增 `currentGoal` ref 接入 localStorage 持久化 + `setCurrentGoal`/`clearCurrentGoal` actions；`useSignalR` DungeonReady 回调从服务端回填；`useGameSession` 新增 `setGoal`/`clearGoal` 包装（乐观更新）；`PlayerInput.vue` 新增目标 chip（🎯 当前目标 {text} ×）+ 100 字硬上限 + placeholder 改为“表达你的意图或目标…”；`GameMain.vue` 自由输入回调改绑 `setGoal`（`sendAction` 仅供服务端推送的 choice 按钮使用）。**不新增 AI 节点、不新增链路**；Hub `PlayerAction` 方法保留仅供 `SelectCachedAction` 缓存未命中回退路径内部使用。编译验证：后端 0 错误 / 79 既有警告未新增，前端 `vue-tsc` EXIT=0 |
| 4.5.0 | 2026-09-18 | **框架重构** | 网文优先（去电影感）：前台导演提示词角色 TRPG导演(DM)→游戏主持人(GM)（代码组件名 `Director`/`director_front_system.txt` 不变，仅模型可见词变）；`narrative_seed` 文学场景速写→剧情细纲（时间顺序事件条目+NPC台词大意+玩家状态变化），叙事AI据细纲从零写正文而非扩写散文（根治散文种子扩写4倍必注水为描写）；`prose_guidance` 句式节奏+感官重点+文学手法→叙事节奏+玩家情绪落点+必须写清的信息，禁慢镜头/特写/碎片化；`beats` 分镜表→分段细纲；叙事AI创作三原则→写作四原则+可量化硬性禁令，并删除第7条书面腔词汇禁令；场景文风模块重写为节奏/感受/禁忌；建筑师 style_bible 禁用电影/文学流派当语调；整链（含书记官/物资官/分类AI/评测集）移除电影术语。代码侧：`NarrativeAiService`（BuildSceneStyleModule 重写、BuildChapterBeatMessages/BuildBlueprintText/BuildProseGuidanceText 标签改细纲）、`AiCoordinatorService.BuildDirectorFacts`、`DirectorAiService` 注入消息、`ScribeAiService`/`QuartermasterAiService` 标签、`DirectorSuite`/`DirectorOutput` 同步。所有 JSON 键名保留不变，代码解析/日志/评测兼容。详见 [叙事设计规范 v2.5.0](narrative-design.md) |
| 4.4.2 | 2026-09-16 | **提示词调优** | 书记官选项接住导演引导线索：`scribe_system.txt`「两档通用规则」新增一条——既定事实中的「引导线索」（`BuildDirectorFacts` 将导演 `narrative_hooks` 拼入的段落）是导演埋的方向暗示，两选项至少一个顺线索延伸，多线索时分别对应两条（细粒度档抉择点各线索对应不同岔路），单线索时另一选项给不同方向，禁止同跟一条。背景：导演模板 `player_choice_point` 规则早已要求“hooks 输出各选项的隐含暗示”，但书记官侧从未有读取指令，hooks 只以隐性路径影响选项。同批：删已废弃的 `director_system.txt`（含 5 份 bin 副本）；`director_front_system.txt`/`scribe_system.txt` 去重复行 |
| 4.4.1 | 2026-09-16 | **瘦身重构** | 前台导演提示词瘦身第二批（`director_front_system.txt` 132→123 行）：① 删 `[常规行动]` 规则——掷骰条件只看 `Judgment.Needed && Skill && Dc>0` 不看 IsRoutine，该标记可与 `[判定结果]` 共现而对撞，且“无[判定结果]时直接描述”已覆盖其语义；连带清 `DirectorInput.IsRoutine`、`DirectorAiService.routineTag`、`DirectorSuite` 透传、`DirectorInputCase.IsRoutine`、`director.json` 8 处 `is_routine` 键（分类侧 `ClassificationResult.IsRoutine` 保留）；② 删同义反复的 `tension_level反映当前剧情紧张程度` 与重复的 `只输出结构化JSON`；③ `主线进度利用规则` 并入 `主动引导规则`，“让导演自己数 change_history 连续3轮”改为响应 `[剧情推进提示]`（`DetectStagnationAsync` 已做同一检测，去双轨）；④ schema 内联说明与写作规则段去双写（narrative_seed/prose_guidance/dialogue_direction 内联只留一句定位，细则保留在规则段；`beat_scale` 内联尾句与 L3/「判定次序」重复，删）；⑤ 新增 `[支线任务清单]`/`[隐藏内容清单]` 使用规则（此前每轮注入但零规则；`DirectorAiService`/`DirectorInput` “供导演标记完成时精确匹配”的过时注释同步改正）。保留：`[推进型行动]` 三处强制表述（v4.3.x 为解决导演不升 chapter 有意加的冗余）、引号约束（`RepairUnescapedQuotes` 仅是启发式兜底） |
| 4.4.0 | 2026-09-16 | **瘦身重构** | 前台导演瘦身：整链拆除 `needs_state_change`。该字段在 v4.0.0 拆出书记官后失去记账字段排除的标的，v4.3.0 后又失去书记官分流的标的，全链路仅剩两处消费：导演 `[无需状态变更]` 标记（字段白名单漏列 `beat_scale`/`narrative_word_target`/`beats`，反而误导导演省略档位字段）与时段推进门控（导演模板自身已约束简单观察不推进，双重门控无增量保险）。移除：`classifier_system.txt` 「三、是否需要状态变更」与输出键（后续节重编号）；`ClassificationResult`/`DirectorInput`/`GameActionResult.NeedsStateChange`；`ActionClassifierService` 解析与日志；`DirectorAiService` 的 `stateChangeTag`；`director_front_system.txt` 的 `[无需状态变更]` 规则；`AiCoordinatorService` 时段推进改为 `if (directorOutput.TimeAdvance)`；AIEval `DirectorInputCase.NeedsStateChange` 与 `director.json` 的 8 处用例键。行为变化仅一处：`time_advance` 不再被上游分类误判吞掉，`[推进型行动]` 的时段推进得以稳定生效 |
| 4.3.0 | 2026-09-16 | **缺陷修复** | 书记官常规轮始终完整记账：移除纯叙事轮（`NeedsStateChange=false`）的 SuggestionsOnly 分流，所有经过导演蓝图的轮次一律走完整书记官（接收全量上下文、产出全部结构化字段）；`ApplyCachedActionResultAsync` 移除 `NeedsStateChange` 门控（改为只要 `ScribeOutput.WorldStateChanges != null` 就落库）；SuggestionsOnly 仅保留给无导演蓝图的场景（开场轮/不可行短路）。根治「信息获取类行动未记账 → 叙事层与世界状态层脱节 → 后续轮次导演覆盖前轮剧情」的连贯性 bug |
| 4.2.0 | 2026-09-16 | **机制升级** | 行动选项全场景覆盖：书记官新增 SuggestionsOnly 轻量模式统一覆盖纯叙事轮/开场轮（首次进入·重新开始·同题异卷重玩）/不可行短路，只产出 suggested_actions 不记账不落库；新增 `AiCoordinatorService.BuildSuggestionsOnlyInputAsync`/`GenerateSuggestionsOnlyAsync`，Hub `ProcessSelectDungeonAsync` 步骤9 + `ProcessRestartSessionAsync` 在 DungeonReady 后生成开场选项；不可行短路复用 `RunScribeTaskAsync` 产出补救选项并修复 `ProcessPlayerActionAsync` 拒绝叙事未推送缺陷（补 else if 分支）；新增挂起/断线恢复选项持久化（`GameDungeonSession.LastSuggestedActions` + 恢复跳过回归旁白续上离开前最后一轮 + `CheckActiveSessionAsync`/`ActiveSessionCheckOutput` 分层降级）；预计算缓存 TTL 1小时→1天 |
| 4.1.0 | 2026-09-13 | **性能优化** | 预计算深度下调到 L1（导演层）：`PrecomputeSingleOptionAsync` 不再预生成叙事、不 await 书记官（fire-and-forget 预跑保留 `ScribeTask` 句柄），就绪时刻 ~35s→~15s；点选路径 `ProcessSelectCachedActionAsync` 重构为「叙事实时流式（`StreamNarrativeLiveAsync`）‖ 后台链链首 await 书记官回填 → `ApplyCachedActionResultAsync` 落库 → 物资官记账 → 启动下一轮预计算」，session/character 改落库后读取、叙事日志与下一轮选项 DTO 后置构建；章节档统一实时流式（`ChapterPrefetchBeats`/`StreamChapterContinuationAsync`/`NextBeatIndex`/`NarrativeText` 闲置废弃）；`ActionPrecomputeService` 删除 `IOptions<GameAiOptions>`/`narrativeAi` 依赖、新增 `SessionActionCache.OptionsShownAt` + `MarkOptionsShown`/`GetOptionsShownAt`；新增两个延迟埋点（玩家思考间隔 / 叙事首 token 延迟，均 `LogInformation`）；L1.5（叙事首 token 开放）列为备用方案 |
| 4.0.0 | 2026-09-13 | **架构重构** | 导演管线重构（延迟优化）：新增**书记官AI**（`ScribeAiService` + `scribe_system.txt` + `Scribe` 模型配置）接管 `world_state_changes`（含quest_progress）/`item_hints`/`npc_attitude_changes`/`suggested_actions`，作为后台 Task 与叙事流式并行（`GameActionResult.ScribeTask`，独立 DI scope，Hub 读字段前 await）；导演模板拆出 `director_front_system.txt`（输出 schema 砍半、npc_actions 去 attitude_change）；物资官记账链整体移出关键路径（书记官→物资官→推送→预计算）；`NeedsStateChange=false` 轮次跳过书记官（纯叙事轮零成本，代价是无建议选项）；书记官失败重试1次、仍失败则状态不变且叙事不中断；分类AI/导演AI 关闭思考模式、导演模型回到 `deepseek-v4-flash`；预计算缓存存双输出（入库前 await 书记官并置 null）、`ApplyCachedActionResultAsync` 改读 `ScribeOutput`、AIEval `DirectorSuite` 适配 |
| 3.6.0 | 2026-07-25 | **健壮性加固** | 物资官记账失败降级：按结构化蓝图规则化保底落库（`RecordFromBlueprintAsync` 新增 blueprint 参数，物品默认重量0.5、is_key→关键道具），失败日志含会话/行动/蓝图摘要；缓存选项路径补齐背包超重门卫（≥100%阻断）；缓存过期/未命中改为以选项文本（协议新增 `ActionText`）走常规全链路，旧客户端兜底提示重新输入；重开副本软删、放弃会话硬删已知情报账本，前端清理函数同步清空线索区 |
| 3.5.0 | 2026-07-25 | **架构升级** | 资产账本方案（导演后记账 + 单门卫）：新增道具AI（物资官）于导演后、叙事前依导演蓝图 `item_hints`（含 is_key）记账落库（物理走 InventoryService、无形走 KnownAssetService）；分类AI升级为唯一综合门卫（三态可行性 feasible/uncertain/infeasible + 普通资源放过，只读账本）；叙事AI新增道具纪律（不得增删蓝图交付）；预计算启动时机改为记账后与叙事并行（根治死按钮）；背包/情报叙事前刷新；`acquired_items/consumed_items` 内联应用退役（仅保留反序列化容错） |
| 3.4.0 | 2026-07-21 | **性能优化** | 预计算机制按 `beat_scale` 分治：micro/normal 维持整段预生成秒开；chapter 不再整章预生成，仅预取首 N 段（`ChapterPrefetchBeats`，默认1），点选后秒回放前缀 + 从断点实时分段续写（边读边生成掩盖延迟），将章节档未选选项白烧从最多 16 次降到 2N 次；新增 `PrecomputedActionCache.NextBeatIndex`；修复缓存回放分块器 `Trim()` 掉 `\n\n` 导致的段落塌陷 bug |
| 3.3.0 | 2026-07-21 | **功能增强** | 导演AI新增节拍分档（beat_scale：micro/normal/chapter）+ 章节档分镜表（beats 4-8）；叙事字数硬顶 800→3000；章节档采用分段生成（逐分镜调用模型、前序正文作上下文、逐段流式拼接，Hub/前端零改动）；章节档解除导演克制（允许主动制造大事件）；新增服务端可配 MaxTokens |
| 3.2.0 | 2026-07-11 | **性能优化** | 导演AI模型升级为 qwen3.7-max + 关闭思考模式（延迟 30-72s → 3-6s）；预计算触发提前到导演返回后与叙事流式并行；叙事字数改为导演驱动混合制（narrative_word_target） |
| 3.1.0 | 2026-07-06 | **性能优化** | 新增「行动选项预计算机制」：导演AI输出suggested_actions，后台DryRun并行预计算并缓存，玩家点选秒级响应 |
| 3.0.0 | 2026-07-05 | **架构重构** | 新增「文学引擎」三层架构：导演AI输出升级为narrative_seed+prose_guidance+dialogue_direction；叙事AI升级为创作三原则+Few-shot+场景文风模块；建筑师AI新增文风圣经+意象系统 |
| 2.2.0 | 2026-06-19 | **功能增强** | 分类AI新增行动意图提炼（action_intent），导演AI同时接收意图+原文，提升推演稳定性 |
| 2.1.0 | 2026-06-16 | **架构升级** | 世界状态升级为结构化局面快照；分类/导演AI移除recentNarrative；world_state_changes改为结构化输出 |
| 2.0.0 | 2026-06-15 | **架构重构** | 判定信息流重构：Judgment从导演AI迁移至分类AI，骰子掷骰移至导演AI之前，导演AI知成败后推演 |
| 1.4.0 | 2026-06-14 | 补充说明 | DC标尺补充世界难度修正说明（导演AI给原始DC，系统叠加难度修正） |
| 1.3.0 | 2026-06-14 | 功能优化 | DC标尺通用化改造：从战斗导向改为多行动类型情境化评估 |
| 1.2.0 | 2026-06-14 | 机制迁移 | 行动可行性判定从导演AI迁移至分类AI（门卫原则+成本优化） |
| 1.1.0 | 2026-06-02 | 新增机制 | 导演AI新增acquired_items道具生成、DC标尺校准、装备对抗原则 |
| 1.0.0 | 2026-05-31 | 首版发布 | 从设计讨论整理归档 |
