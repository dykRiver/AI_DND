namespace DHY.Core.Interfaces
{
    /// <summary>
    /// 可控制接口
    /// </summary>
    public interface IControlAble
    {
        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
        bool Start();

        /// <summary>
        /// 暂停
        /// </summary>
        /// <returns></returns>
        bool Pause();

        /// <summary>
        /// 继续
        /// </summary>
        /// <returns></returns>
        bool Resume();

        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        bool ShutDown();

        /// <summary>
        /// 重启
        /// </summary>
        /// <returns></returns>
        bool ReStart();
    }
}
