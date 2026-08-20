# DHY.Game.AIEval — AI 质量回归评测工具

修改 Prompt 模板（`DHY.Game.AI/Prompts/Templates/*.txt`）或更换模型配置后，跑一条命令即可量化回答：**这次改动让 AI 质量变好还是变坏了**。

## 快速开始

```bash
# 全量评测（4 个评测集 + LLM-as-judge），输出报告到 bin/.../reports/
dotnet run --project DHY.Game.AIEval -- --suite all

# 日常改 prompt 后先跑规则层（免 judge 调用，秒级费用更低）
dotnet run --project DHY.Game.AIEval -- --suite all --judge off

# 只跑某个评测集 / 单个用例调试
dotnet run --project DHY.Game.AIEval -- --suite classifier
dotnet run --project DHY.Game.AIEval -- --suite quartermaster --case qm_lost_01 --judge off

# 以本次结果为基线（确认当前质量可接受后执行）
dotnet run --project DHY.Game.AIEval -- --suite all --update-baseline
```

## CLI 参数

| 参数 | 取值 | 默认 | 说明 |
|------|------|------|------|
| `--suite` | `classifier` / `director` / `quartermaster` / `narrative` / `all` | `all` | 要运行的评测集 |
| `--judge` | `on` / `off` | `on` | 是否启用 LLM-as-judge（主观维度评审） |
| `--case` | 用例 id | 无 | 只跑单个用例（调试用） |
| `--concurrency` | 1-8 | 2 | 并发数（防模型限流） |
| `--config` | 文件路径 | 自动定位 | 显式指定 `GameAiOptions.json` |
| `--update-baseline` | 开关 | 关 | 运行后把本次结果写入基线 |

**退出码**：`0`=全部通过且无回归；`1`=存在失败用例或回归；`2`=致命错误/参数错误。可直接作为 CI 门禁（CI 接入需先解决 API Key 机密管理）。

## 四个评测集

| 评测集 | 案例数 | 判定方式 | 关注点 |
|--------|--------|----------|--------|
| classifier | 14 | 纯规则 | 三态可行性（误杀/漏放）、常规行动分类、成人判定、DC 落区间、技能匹配 |
| director | 8 | 规则 + judge | JSON 结构完整（种子/文风指导/对话四层/分镜表）、建议选项纪律（恰好2个≤15字）、hint 纪律、成败一致性 |
| quartermaster | 6 | 纯规则 | 蓝图逐条落实（不丢不多）、数值补全（类型非空且weight≥0，关键道具weight=0是模板允许的设计）、情报/道具分流 |
| narrative | 4 | 规则 + judge | 字数区间、禁用陈词、信息泄露（forbidden_facts）、文学品质三维 rubric、**玩家名保真**（judge审查NPC称呼，第二人称正文不出现玩家名属正常） |

judge 层用 Director 同款模型：叙事评审三维 1-5 分（文风遵循/种子一致性/感官节奏，通过线均分≥4 且单项≥3）+ 玩家名保真审查（NPC称呼仅限角色名，禁止编造昵称）；导演种子做成败一致性审查。

## 判定分层

- **规则层**：免费、确定性（字段完整、字数区间、名称包含匹配、禁用词扫描）。每次必跑。
- **judge 层**：LLM 评审主观维度，`--judge off` 可跳过。模型输出存在随机性，边缘用例单次抖动属正常现象，判读回归时以多次运行趋势为准。

## 配置与隔离

- 模型配置复用线上 `GameAiOptions.json`（自动定位：运行目录 > `DHY.FrameWork.Web.Entry/Configuration` > `DHY.FrameWork.Application/Configuration`，向上 8 级；或 `--config` 指定）。
- Prompt 模板通过 csproj Content 链接与 `DHY.Game.AI` **同源**，改模板后无需手工同步。
- 评测使用本地 SQLite 空库 + 伪会话 ID（`9000000001`）：**不触碰任何真实游戏数据**；物资官落库因查无角色自动跳过，只验证记账增量。AI 调用日志照常写入 `GameAiCallLog`（SessionId=伪会话）。
- 报告输出到 `bin/<配置>/net8.0/reports/`：`eval_{时间戳}.md`（人读）+ `.json`（机器读）+ `baseline.json`（基线）。注意基线跟随构建产物，`dotnet clean` 后需重新 `--update-baseline`。

## 案例编写规范

案例为 JSON，位于 `Cases/{suite}/*.json`，字段一律 **snake_case**。公共字段：

```json
{ "id": "套件前缀_主题_序号", "desc": "一句话说明测什么（含历史缺陷模式标注）" }
```

### classifier（ClassifierCase）

```json
{
  "scenario": "局面快照：时间/地点/环境/NPC状态",
  "inventory": "背包摘要，空则写（无道具）",
  "npc_profiles": "NPC档案摘要，空则写（无NPC在场）",
  "player_input": "玩家原始输入",
  "expect": {
    "feasibility": "feasible|uncertain|infeasible",
    "is_routine": true,
    "is_adult": false,
    "judgment_needed": true,
    "dc_range": [11, 18],
    "skill_hint": "隐匿"
  }
}
```

`expect` 内字段均可选，只校验写出的字段。`skill_hint` 为包含匹配，技能名须出自分类器 16 项标准技能。

### director（DirectorCase）

`input` 13 个字段与 `DirectorInput` 一一对应（`judgment_outcome` 写判定文本如 `技能：巧手，DC 13，掷骰结果：17，判定成功`，无需检定时为 `null`）。`expect` 支持：

- `judgment_success`：本拍成败语义（judge 审查种子走向一致性）
- `expect_hint_names` / `forbid_hint_names`：hint 纪律（包含匹配）
- `expect_beat_scale`：`micro`/`normal`/`chapter` 精确匹配
- `expect_dialogue`：社交场景要求输出对话指导

### quartermaster（QuartermasterCase）

`item_hints_text` 为导演蓝图原文（喂给 LLM），`blueprint` 为同内容的结构化版本（LLM 失败时保底）。每条蓝图：`{ "name", "category": "物品|情报", "change": "获得|消耗|失去", "note", "is_key" }`。情报失效用 `category=情报 + change=失去`。`expect` 五组名称清单均可选（包含匹配）；`forbid_extra`/`require_item_numeric`/`info_not_as_item` 默认开启。

### narrative（NarrativeCase）

`input` 含 `scene_type`（explore/dialogue/combat/horror/daily）、`word_target`、`character_name`、`style_bible`（内含禁用陈词清单）、`motif_tracker`、`world_context`、`npc_language_cards`、`blueprint`（DirectorOutput 完整结构）。`expect`：

- `word_range: [min, max]`：建议按 word_target ±30% 设置
- `forbidden_words`：禁用陈词（出现即失败）
- `must_mention`：必含文本（通用检查；玩家名保真不用此项——产品为第二人称叙事，正文不出现玩家名是设计使然，改由 judge 层审查 NPC 称呼）
- `forbidden_facts`：禁止泄露的事实关键词（NPC 隐瞒内容等）
- `judge`：是否参与 LLM 评审（默认 true）

## 如何加新用例

1. 在对应 `Cases/{suite}/` 下新增或追加 JSON（文件名排序即加载顺序）；
2. `id` 全局唯一，命名 `{套件缩写}_{主题}_{两位序号}`；
3. 先单例调试：`--suite {suite} --case {id}`；
4. 稳定后重跑 `--update-baseline` 纳入基线。

**优先收录历史缺陷模式**：玩家名丢失/替换、可行性误杀或漏放、账本漏记/混记、禁用陈词出现、建议选项超数超长、隐瞒信息泄露。

## 成本预估

全量（judge 开）约 46 次模型调用（生成 32 + judge 14）；`--judge off` 约 32 次且均为规则判定。建议：日常改 prompt 先跑规则层，合入前跑全量。

## 边界说明

- 不引入 xUnit 等测试框架——评测本质是"调真实模型打分"，非单元测试。
- 不修改任何生产服务代码。
- 成人叙事（AdultNarrative 模型路径）暂不纳入评测集，后续单列。
