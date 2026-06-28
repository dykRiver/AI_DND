namespace DHY.Core;
public class RequestBase
{
    public ushort DeviceNo { get; set; }
    public long TaskNo { get; set; }
    public long DDCSPid { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
