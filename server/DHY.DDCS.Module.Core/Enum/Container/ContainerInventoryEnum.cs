namespace DHY.DDCS.Module.Core.Enum.Container
{
    /// <summary>
    /// 桶在位置
    /// </summary>
    public  enum ContainerInventoryEnum
    {
        Unknown=0,
        /// <summary>
        /// 调剂区工作区
        /// </summary>
        Dispensing=10,
        /// <summary>
        /// 浸泡区-运输线
        /// </summary>
        SoakingTransport=12,
        /// <summary>
        /// 浸泡区-巷道内线体
        /// </summary>
        SoakingLineBody=15,
        /// <summary>
        /// 煎煮区-煎药机工位
        /// </summary>
        Decocting=20,
        /// <summary>
        /// 清洗区
        /// </summary>
        Cleaning=30
    }
}
