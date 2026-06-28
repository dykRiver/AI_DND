using DHY.MG.Module.Sys.Dtos;

namespace DHY.MG.Module.Sys.Hubs;

/// <summary>
/// DDBot Hub 强类型客户端接口
/// 定义服务端可调用的客户端方法（回调）
/// </summary>
public interface IDDBotHub
{
    /// <summary>中转采集消息（A → B）</summary>
    Task OnCollectedMessages(DDBotCollectedBatch batch);

    /// <summary>中转采集端状态（A → B）</summary>
    Task OnStatusUpdate(DDBotStatusUpdate status);

    /// <summary>中转控制指令（B → A）</summary>
    Task OnControlCommand(DDBotControlCommand command);

    /// <summary>中转配置同步（B → A）</summary>
    Task OnConfigSync(DDBotConfigSync config);

    /// <summary>对端上线通知</summary>
    Task OnPeerConnected(DDBotPeerInfo peer);

    /// <summary>对端离线通知</summary>
    Task OnPeerDisconnected(DDBotPeerInfo peer);

    /// <summary>注册确认</summary>
    Task OnRegistered(DDBotRegistrationResult result);
}
