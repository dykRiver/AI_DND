using System.ComponentModel.DataAnnotations;

namespace DHY.DDCS.Module.Core.Dto.Common;

public class SysTisaneMethodDto
{
    public int MethodId { get; set; }

    /// <summary>
    /// 煎煮时长
    /// </summary>
    [Required]
    public int DecoctionTime { get; set; }

    /// <summary>
    /// 群药/一煎时间
    /// </summary>
    public int? Time1 { get; set; }

    /// <summary>
    /// 二煎时间
    /// </summary>
    public int? Time2 { get; set; }

    /// <summary>
    /// 先煎时间
    /// </summary>
    public int? TimePre { get; set; }

    /// <summary>
    /// 后下煎药时间
    /// </summary>
    public int? TimePost { get; set; }
}
