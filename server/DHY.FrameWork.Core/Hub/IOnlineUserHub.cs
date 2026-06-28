using DHY.Core.EventBus;

namespace DHY.Core;

public interface IOnlineUserHub
{
    /// <summary>
    /// 在线用户列表
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task OnlineUserList(OnlineUserList context);

    /// <summary>
    /// 强制下线
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task ForceOffline(object context);

    /// <summary>
    /// 发布站内消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicNotice(SysNotice context);

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task ReceiveMessage(object context);
    /// <summary>
    /// 发送定向事件消息
    /// </summary>
    /// <param name="eventSource"></param>
    /// <returns></returns>
    Task PublishDirectionalMessage(DirectionalEventSource eventSource);

    /// <summary>
    /// 发布包装机开盖完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicPackerCoverOpenCompleteMessage(object context);

    /// <summary>
    /// 发布包装机关盖完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicPackerCoverCloseCompleteMessage(object context);

    /// <summary>
    /// 发布包装机清洗完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicPackerCleanCompleteMessage(object context);

    /// <summary>
    /// 发布煎药机开盖完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicDecoctorCoverOpenCompleteMessage(object context);

    /// <summary>
    /// 发布煎药机关盖完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicDecoctorCoverCloseCompleteMessage(object context);

    /// <summary>
    /// 发布包装翻倒完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicPackerOverturnCompleteMessage(object context);

    /// <summary>
    /// 发布包装翻倒桶到消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicPackerOverturnBucketReachMessage(object context);

    /// <summary>
    /// 发布组桶工位桶到消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicDirtyRefluxBucketReachMessage(object context);

    /// <summary>
    /// 发布沥液完成消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicLiftCompleteMessage(object context);

    /// <summary>
    /// 发布沥液工位上传体积消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicLiftVolumeReachMessage(object context);

    /// <summary>
    /// 发布沥液工位桶到消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicLiftBucketReachMessage(object context);

    /// <summary>
    /// 发布Rgv取桶确认消息
    /// </summary>
    /// <param name="context"></param>
    Task PublicRgvPullComfirmMessage(object context);

    /// <summary>
    /// 发布Rgv放桶确认消息
    /// </summary>
    /// <param name="context"></param>
    Task PublicRgvPutComfirmMessage(object context);

    /// <summary>
    /// 发布Rgv搬运完成消息
    /// </summary>
    /// <param name="context"></param>
    Task PublicRgvCompletedMessage(object context);

    /// <summary>
    /// 发布日志消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicLogMessage(object context);

    /// <summary>
    /// 发布错误日志消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicErrorLogMessage(object context);

    /// <summary>
    /// 发布警告日志消息
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task PublicWarningLogMessage(object context);

    /// <summary>
    /// 发布处方接收消息
    /// </summary>
    /// <returns></returns>
    Task PublicReceivePrescriptionNotice();

    /// <summary>
    /// 发布包装完成消息
    /// </summary>
    /// <returns></returns>
    Task PublicPackingCompletedNotice();

    /// <summary>
    /// 发布主页刷新通知
    /// </summary>
    /// <returns></returns>
    Task PublicHomepageRefreshNotice();
}