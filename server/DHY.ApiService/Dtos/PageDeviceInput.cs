namespace DHY.InternalApiService.Dtos
{
    /// <summary>
    /// 分页查询输入Dto
    /// </summary>
    //public class PageDeviceInput : BasePageInput
    //{
    //    /// <summary>
    //    /// 工位名称
    //    /// </summary>
    //    public string Name { get; set; }
    //    /// <summary>
    //    /// 通道Id（工位的指令信息属于哪个驱动）
    //    /// </summary>
    //    public long ChannelId { get; set; }
    //    /// <summary>
    //    /// 工位类型
    //    /// </summary>
    //    public int DeviceType { get; set; }
    //    /// <summary>
    //    /// 通讯类型？
    //    /// </summary>
    //    public int CommunicateType { get; set; }
    //}
    public class PageStationInput : BasePageInput
    {
        /// <summary>
        /// 工位名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 通道Id（工位的指令信息属于哪个驱动）
        /// </summary>
        public long ChannelId { get; set; }
        /// <summary>
        /// 工位类型
        /// </summary>
        public StationTypeEnum StationType { get; set; }
        /// <summary>
        /// 通讯类型？
        /// </summary>
        public CommunicateTypeEnum CommunicateType { get; set; }
        /// <summary>
        /// 数据块
        /// </summary>
        public int DataBlock { get; set; }
    }
}
