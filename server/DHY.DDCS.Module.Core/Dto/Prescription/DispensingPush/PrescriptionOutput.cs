using Newtonsoft.Json;

public class PrescriptionOutput
{
    /// <summary>
    /// 原始处方Json串
    /// </summary>
    [JsonProperty("PrescriptonJson")]
    public string PrescriptionJson { get; set; }
}