using System.ComponentModel.DataAnnotations;

public class PrescriptionInfoDetailOutput
{
    /// <summary>
    /// 关联处方ID
    /// </summary>
    /// <example>14029</example>
    public long Pid { get; set; }

    /// <summary>
    /// 药品Id，与煎药系统对应
    /// </summary>
    /// <example>7211</example>

    public long DrugId { get; set; }

    /// <summary>
    /// 本厂药品编码
    /// </summary>
    /// <example>220</example>

    public string Code { get; set; } = "123";

    /// <summary>
    /// 本厂药品名称
    /// </summary>
    /// <example>金樱子</example>
    [Required, MaxLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// 药品规格
    /// </summary>
    /// <example>统</example>

    public string? Specification { get; set; }

    /// <summary>
    /// 药品单位
    /// </summary>
    /// <example>g</example>

    public string Unit { get; set; }

    /// <summary>
    ///单剂量；单位：g
    /// </summary>
    /// <example>10</example>

    public decimal SingleDosage { get; set; }

    /// <summary>
    /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
    /// </summary>
    /// <example>1</example>

    public ContainerTypeEnum DecoctionType { get; set; }

    /// <summary>
    /// 总剂量；单位：g
    /// </summary>
    /// <example>70</example>

    public decimal Weight { get; set; }

    /// <summary>
    /// 吸水比
    /// </summary>
    public decimal WaterAbsorptionRatio => GetWaterAbsorptionRatio(WaterAbsorptionRatioCiphertext);

    public string WaterAbsorptionRatioCiphertext { get; set; }

    /// <summary>
    /// 加水量
    /// </summary>
    public int? WaterAmount { get; set; }

    /// <summary>
    /// 泡药时间；单位：分钟
    /// <example>30</example>
    /// </summary>
    public int? SoakWaterTime { get; set; }

    /// <summary>
    /// 煎煮时间；单位：分钟
    /// <example>45</example>
    /// </summary>
    public int? DecoctionTime { get; set; }

    /// <summary>
    /// 药品脚注
    /// </summary>
    public string DrugDescription { get; set; }

    public static decimal GetWaterAbsorptionRatio(string waterAbsorptionRatioCiphertext)
    {
        decimal result;
        bool success = decimal.TryParse(EncryptDecryptHelper.AESDecrypt2(waterAbsorptionRatioCiphertext), out result);
        if (success)
        {
            return result;
        }
        else
        {
            return 0;
        }
    }
}