namespace DHY.Core.Interfaces
{
    /// <summary>
    /// 表示一个可订阅对象
    /// </summary>
    public interface IOpcUASubscribeAble {
        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="key"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool AddSubscriber(string key, string[] tag,Action<string,object> messageHandler);
       
        /// <summary>
        /// 移除订阅
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveSubscriber(string key);
    }

    /// <summary>
    /// 可配置接口
    /// </summary>
    public interface ISetupAble
    {
        /// <summary>
        /// 配置参数
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        bool Setup(params object[] para);
    }
}
