namespace DHY.Core.EventBus
{
    public sealed class RpcEventBusMessageIdPair
    {
        public string RequestMessageId { get; set; }

        public string ResponseMessageId { get; set; }

        /// <summary>
        /// 通用注册消息ID
        /// </summary>
        public string RegisterMessageId { get; set; }

    }
}
