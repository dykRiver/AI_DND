using DHY.Game.Hub.Dtos;

namespace DHY.Game.Hub.Hubs;

/// <summary>
/// 服务端→客户端推送方法定义
/// </summary>
public interface IGameSessionHub
{
    /// <summary>流式叙事文字推送</summary>
    Task ReceiveNarrative(NarrativeChunkDto chunk);

    /// <summary>骰子判定结果</summary>
    Task ReceiveDiceResult(DiceResultDto result);

    /// <summary>游戏状态更新</summary>
    Task UpdateGameState(GameStateDto state);

    /// <summary>时段转换过渡</summary>
    Task SendTimeTransition(TimeTransitionDto transition);

    /// <summary>玩家选择点提示</summary>
    Task RequestPlayerChoice(PlayerChoiceDto choices);

    /// <summary>副本世界生成进度</summary>
    Task DungeonGenerating(GeneratingProgressDto progress);

    /// <summary>副本会话结束</summary>
    Task SessionEnded(SessionEndDto result);

    /// <summary>系统消息/错误</summary>
    Task ReceiveSystemMessage(SystemMessageDto message);

    /// <summary>副本世界信息推送</summary>
    Task ReceiveWorldInfo(WorldInfoDto worldInfo);

    /// <summary>副本就绪通知（后台生成完成后推送）</summary>
    Task DungeonReady(DungeonReadyDto ready);

    /// <summary>高风险行动确认请求</summary>
    Task RequestDangerousActionConfirm(DangerousActionDto action);

    /// <summary>背包更新推送（获得道具/装备变更/使用次数变化）</summary>
    Task UpdateInventory(InventoryUpdateDto inventory);

    /// <summary>支线任务状态更新（导演AI标记任务完成时推送）</summary>
    Task UpdateSideQuests(SideQuestUpdateDto update);
}
