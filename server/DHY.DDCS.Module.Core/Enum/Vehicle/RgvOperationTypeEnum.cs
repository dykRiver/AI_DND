namespace DHY.DDCS.Module.Core;

public enum RgvOperationTypeEnum
{
    /// <summary>
    /// 未定义
    /// </summary>
    UnKnown = -1,
    /// <summary>
    /// 任务下发
    /// </summary>
    Send = 0,
    /// <summary>
    /// 取桶确认
    /// </summary>
    Pull = 1,
    /// <summary>
    /// 放桶确认
    /// </summary>
    Put = 2,
    /// <summary>
    /// 完成搬运
    /// </summary>
    Complete = 3,
}
