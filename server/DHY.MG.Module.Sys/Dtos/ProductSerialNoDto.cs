
namespace DHY.MG.Module.Sys.Dtos;

public class ProductSerialNoDto
{
    public ProductSerialNoDto() { }

    public ProductSerialNoDto(string serialNo, string lastLoginTime, bool isOnline, string ip)
    {
        SerialNo = serialNo;
        LastLoginTime = lastLoginTime;
        IsOnline = isOnline;
        Ip = ip;
    }
    public string SerialNo { get; set; }
    public string LastLoginTime { get; set; }
    public bool IsOnline { get; set; }
    public string Ip { get; set; }

    public bool IsUse { get; set; }

}
