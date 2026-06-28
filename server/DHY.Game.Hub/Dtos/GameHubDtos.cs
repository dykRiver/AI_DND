namespace DHY.Game.Hub.Dtos;

#region 服务端→客户端 推送DTO

/// <summary>
/// 叙事文本片段
/// </summary>
public class NarrativeChunkDto
{
    /// <summary>文字片段</summary>
    public string Text { get; set; } = "";

    /// <summary>块类型: narrative/action_result/scene_transition</summary>
    public string ChunkType { get; set; } = "narrative";

    /// <summary>是否最后一块</summary>
    public bool IsLast { get; set; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 骰子判定结果
/// </summary>
public class DiceResultDto
{
    /// <summary>技能名称</summary>
    public string SkillName { get; set; } = "";

    /// <summary>D20骰子点数</summary>
    public int D20Roll { get; set; }

    /// <summary>调整值</summary>
    public int Modifier { get; set; }

    /// <summary>总计</summary>
    public int Total { get; set; }

    /// <summary>难度等级（分类AI原始DC）</summary>
    public int DC { get; set; }

    /// <summary>世界难度修正值</summary>
    public int WorldDifficultyModifier { get; set; }

    /// <summary>有效DC（原始DC + 世界难度修正）</summary>
    public int EffectiveDC { get; set; }

    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>是否自然20</summary>
    public bool IsNatural20 { get; set; }

    /// <summary>是否自然1</summary>
    public bool IsNatural1 { get; set; }

    /// <summary>叙事提示</summary>
    public string? NarrativeHint { get; set; }
}

/// <summary>
/// 游戏状态
/// </summary>
public class GameStateDto
{
    /// <summary>当前HP</summary>
    public int CurrentHp { get; set; }

    /// <summary>最大HP</summary>
    public int MaxHp { get; set; }

    /// <summary>HP百分比</summary>
    public int HpPercent { get; set; }

    /// <summary>状态: 正常/轻伤/重伤/濒死</summary>
    public string Status { get; set; } = "正常";

    /// <summary>当前天数</summary>
    public int CurrentDay { get; set; }

    /// <summary>当前时段: 上午/下午/傍晚/夜间</summary>
    public string CurrentSegment { get; set; } = "上午";

    /// <summary>紧张度 (1-10)</summary>
    public int TensionLevel { get; set; }

    /// <summary>是否疲劳</summary>
    public bool IsFatigued { get; set; }

    /// <summary>是否处于战斗</summary>
    public bool IsInCombat { get; set; }
}

/// <summary>
/// 时段转换过渡
/// </summary>
public class TimeTransitionDto
{
    /// <summary>过渡叙事文本</summary>
    public string Narrative { get; set; } = "";

    /// <summary>新时段名</summary>
    public string NewSegment { get; set; } = "";

    /// <summary>新天数</summary>
    public int NewDay { get; set; }
}

/// <summary>
/// 玩家选择点
/// </summary>
public class PlayerChoiceDto
{
    /// <summary>选择提示</summary>
    public string Prompt { get; set; } = "";

    /// <summary>选项列表</summary>
    public List<string> Choices { get; set; } = new();

    /// <summary>是否必须选择</summary>
    public bool IsRequired { get; set; }
}

/// <summary>
/// 副本生成进度
/// </summary>
public class GeneratingProgressDto
{
    /// <summary>阶段: 世界生成中/世界推演中</summary>
    public string Phase { get; set; } = "";

    /// <summary>进度百分比 0-100</summary>
    public int ProgressPercent { get; set; }

    /// <summary>消息</summary>
    public string? Message { get; set; }
}

/// <summary>
/// 副本会话结束
/// </summary>
public class SessionEndDto
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>结束原因: completed/died/abandoned</summary>
    public string EndReason { get; set; } = "";

    /// <summary>评分等级</summary>
    public string? ScoreLevel { get; set; }

    /// <summary>叙事退出文本</summary>
    public string? ExitNarrative { get; set; }

    /// <summary>后日谈</summary>
    public string? Epilogue { get; set; }

    /// <summary>精简评语</summary>
    public string? Comment { get; set; }
}

/// <summary>
/// 系统消息
/// </summary>
public class SystemMessageDto
{
    /// <summary>类型: info/warning/error</summary>
    public string Type { get; set; } = "info";

    /// <summary>消息内容</summary>
    public string Message { get; set; } = "";

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 高风险行动
/// </summary>
public class DangerousActionDto
{
    /// <summary>行动ID</summary>
    public string ActionId { get; set; } = "";

    /// <summary>行动描述</summary>
    public string Description { get; set; } = "";

    /// <summary>警告信息</summary>
    public string Warning { get; set; } = "";
}

/// <summary>
/// 副本世界信息（背景+主线任务+支线任务）
/// </summary>
public class WorldInfoDto
{
    /// <summary>副本名称</summary>
    public string DungeonName { get; set; } = "";

    /// <summary>世界背景描述</summary>
    public string WorldBackground { get; set; } = "";

    /// <summary>主线任务目标</summary>
    public string MainQuestObjective { get; set; } = "";

    /// <summary>主线关键节点</summary>
    public List<string> MainQuestNodes { get; set; } = new();

    /// <summary>关键地点</summary>
    public List<string> KeyLocations { get; set; } = new();

    /// <summary>支线任务列表</summary>
    public List<SideQuestInfoDto> SideQuests { get; set; } = new();
}

/// <summary>
/// 支线任务信息
/// </summary>
public class SideQuestInfoDto
{
    /// <summary>支线任务名称</summary>
    public string Name { get; set; } = "";

    /// <summary>支线任务描述</summary>
    public string Description { get; set; } = "";

    /// <summary>是否已完成</summary>
    public bool IsCompleted { get; set; }
}

/// <summary>
/// 支线任务状态更新推送
/// </summary>
public class SideQuestUpdateDto
{
    /// <summary>已完成的支线任务名列表</summary>
    public List<string> CompletedSideQuests { get; set; } = new();

    /// <summary>本轮新解锁的隐藏支线任务</summary>
    public List<SideQuestInfoDto> UnlockedSideQuests { get; set; } = new();
}

/// <summary>
/// 背包更新推送（获得道具/装备变更/使用次数变化时推送）
/// </summary>
public class InventoryUpdateDto
{
    /// <summary>道具列表</summary>
    public List<InventoryItemDto> Items { get; set; } = new();

    /// <summary>当前总重量</summary>
    public decimal CurrentWeight { get; set; }

    /// <summary>容量上限</summary>
    public int MaxWeight { get; set; }

    /// <summary>重量百分比</summary>
    public double WeightPercent { get; set; }

    /// <summary>是否超重(>=100%)</summary>
    public bool IsOverloaded { get; set; }

    /// <summary>是否负重(>=70%)</summary>
    public bool IsEncumbered { get; set; }

    /// <summary>已装备武器ID</summary>
    public long? EquippedWeaponId { get; set; }

    /// <summary>已装备防具ID</summary>
    public long? EquippedArmorId { get; set; }
}

/// <summary>
/// 道具信息（背包推送用）
/// </summary>
public class InventoryItemDto
{
    public long Id { get; set; }
    public string ItemName { get; set; } = "";
    public string ItemType { get; set; } = "杂物";
    public string? Description { get; set; }
    public decimal Weight { get; set; }
    public int AttributeBonus { get; set; }
    public string? LinkedAttribute { get; set; }
    public int MaxUses { get; set; }
    public int CurrentUses { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsEquipped { get; set; }
    public bool IsKeyItem { get; set; }
    public int Quantity { get; set; }
}

#endregion

#region 副本就绪推送

/// <summary>
/// 副本就绪通知（后台生成完成后推送给客户端）
/// </summary>
public class DungeonReadyDto
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>副本名称</summary>
    public string DungeonName { get; set; } = "";

    /// <summary>世界信息</summary>
    public WorldInfoDto WorldInfo { get; set; } = new();

    /// <summary>初始游戏状态</summary>
    public GameStateDto GameState { get; set; } = new();
}

#endregion

#region 客户端→服务端 输入DTO

/// <summary>
/// 玩家行动输入
/// </summary>
public class PlayerActionInput
{
    /// <summary>行动文本</summary>
    public string ActionText { get; set; } = "";

    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>成人模式开关（前端玩家手动切换，开启后跳过分类AI直接走成人叙事）</summary>
    public bool IsAdultMode { get; set; }
}

/// <summary>
/// 选择副本输入
/// </summary>
public class SelectDungeonInput
{
    /// <summary>模板ID</summary>
    public long TemplateId { get; set; }

    /// <summary>角色名称</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>力量</summary>
    public int Strength { get; set; }

    /// <summary>敏捷</summary>
    public int Dexterity { get; set; }

    /// <summary>体质</summary>
    public int Constitution { get; set; }

    /// <summary>智力</summary>
    public int Intelligence { get; set; }

    /// <summary>感知</summary>
    public int Wisdom { get; set; }

    /// <summary>魅力</summary>
    public int Charisma { get; set; }
}

/// <summary>
/// 确认时段推进输入
/// </summary>
public class ConfirmTimeAdvanceInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>选择: continue / rest</summary>
    public string Choice { get; set; } = "";
}

/// <summary>
/// 加班选择输入
/// </summary>
public class ChooseOvertimeInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>选择: overtime / rest</summary>
    public string Choice { get; set; } = "";
}

/// <summary>
/// 确认高风险行动输入
/// </summary>
public class ConfirmDangerousActionInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>行动ID</summary>
    public string ActionId { get; set; } = "";

    /// <summary>是否确认</summary>
    public bool Confirmed { get; set; }
}

/// <summary>
/// 重新开始输入（保留世界/副本，清除历史记录）
/// </summary>
public class RestartSessionInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
}

#endregion
