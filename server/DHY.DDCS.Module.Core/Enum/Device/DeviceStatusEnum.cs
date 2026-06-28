namespace DHY.DDCS.Module.Common.Enum.Device
{
    public enum DeviceStatusEnum
    {
        Unknown = 0,
        /// <summary>
        /// 空闲
        /// </summary>
        Ready = 0x02,
        /// <summary>
        /// 繁忙
        /// </summary>
        Busy = 0x04,
        /// <summary>
        /// 离线
        /// </summary>
        Offline = 0x08,
        /// <summary>
        /// 关机
        /// </summary>
        PowerOff = 0x0E,
        /// <summary>
        /// 低电量
        /// </summary>
        LowBattery = 0x1A,
    }
}
