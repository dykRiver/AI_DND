namespace DHY.MG.Module.Sys.Entities
{
    /// <summary>
    /// 通用药嘱条目匹配规则
    /// </summary>
    [SugarTable(null, "通用药嘱条目匹配规则")]
    public class ProductSerialNo : EntityTenant
    {
        public ProductSerialNo() { }
        public ProductSerialNo(string serialNo)
        {
            SerialNo = serialNo;
        }

        public ProductSerialNo(string serialNo, DateTime lastLoginTime, bool isOnline, string ip)
        {
            SerialNo = serialNo;
            LastLoginTime = lastLoginTime;
            IsOnline = isOnline;
            Ip = ip;
        }
        public string SerialNo { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsOnline { get; set; }
        public string Ip { get; set; }

        public bool IsUse { get; set; }

        public string CreatorName { get; set; }

    }

}
