using System.Diagnostics;
using DHY.DDCS.Module.Common.Entity;

namespace DHY.DDCS.Module.Prescription.Dtos
{
    /// <summary>
    /// 处方推送入参
    /// </summary>
    [DebuggerDisplay("处方Id:{Pid} 处方号:{PrescriptionNo} 贴数:{Dosage}")]
    public sealed class PrescriptionInfoPushInput : AddPrescriptionInfoDto
    {

    }

    public sealed class PrescriptionDiscardInput
    {
        public long? Id { get; set; }
        public string? PrescriptionNo { get; set; }
    }

    /// <summary>
    /// 原始处方DTO
    /// </summary>
    public sealed class PrescriptionInfoPageQueryInput : BasePageInput
    {
        public long Id { get; set; }
        public string? PrescriptionNo { get; set; }
        public string? PatientName { get; set; }
        public byte? State { get; set; }
    }

    public sealed class PrescriptionInfoQueryInput
    {
        public string? PrescriptionNo { get; set; }
        public string? PatientName { get; set; }
        public PrescriptionManageStatusEnum? State { get; set; }
    }
    public sealed class UpdatePrescriptionInfoStatusInput
    {
        public long Pid { get; set; }
        public PrescriptionManageStatusEnum State { get; set; }
    }

    public sealed class UpdatePrescriptionPriorityInput
    {
        public long Pid { get; set; }
        public PriorityEnum Priority { get; set; }
    }

    /// <summary>
    /// 添加处方DTO
    /// </summary>
    public class AddPrescriptionInfoDto
    {
        /// <summary>
        /// 处方id，与煎药系统对应
        /// </summary>
        /// <example>14029</example>
        public long Pid { get; set; }
        /// <summary>
        /// 外部处方号
        /// </summary>
        /// <example>202404100003</example>
  
        public string PrescriptionNo { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        /// <example>魏晖明</example>

        public string PatientName { get; set; } = "匿名";

        /// <summary>
        /// 处方状态
        /// </summary>
        /// <example>1</example>

        public byte State { get; set; }

        /// <summary>
        /// 贴数/剂数
        /// </summary>
        /// <example>7</example>
       
        public int Dosage { get; set; }

        /// <summary>
        /// 服用次数
        /// </summary>
        /// <example>3</example>

        public int Frequency { get; set; }

        /// <summary>
        /// 服用方式
        /// </summary>
        /// <example>10</example>
     
        public int Usage { get; set; }

        /// <summary>
        /// 煎药方案
        /// </summary>
        /// <example>1</example>
 
        public int DecoctionScheme { get; set; }

        /// <summary>
        /// 先煎药加水量
        /// </summary>
        public int? PreWater { get; set; }

        /// <summary>
        /// 群药加水量
        /// </summary>
        public int? GroupWater { get; set; }

        /// <summary>
        /// 包装量
        /// </summary>
        /// <example>200</example>
   
        public int PackageNum { get; set; }

        /// <summary>
        /// 先煎泡药时间；单位：分钟
        /// </summary>
        public int? PreSoakWaterTime { get; set; }

        /// <summary>
        /// 群药泡药时间；单位：分钟
        /// </summary>
        public int GroupSoakWaterTime { get; set; }

        /// <summary>
        /// 原始处方JSON，包含患者信息和药品信息
        /// </summary>
        public string PrescriptionJson { get; set; }

        /// <summary>
        /// 作废标志，1作废，0不作废
        /// </summary>
        
        public bool Cancellation { get; set; }

        /// <summary>
        /// 处方明细
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(Pid))]
        public List<PrescriptionInfoDetailOutput> Details { get; set; }

        /// <summary>
        /// 群药一煎时间；单位：分钟
        /// </summary>
        public int? GroupFirstDecoctionTime { get; set; }

        /// <summary>
        /// 群药二煎时间；单位：分钟
        /// </summary>
        public int? GroupSecondDecoctionTime { get; set; }

        public int HospitalId { get; set; }

        public byte? DeliveryMethodId { get; set; }

        ///// <summary>
        ///// 服用方式（1内服、2外用等）
        ///// </summary>
        //public int? TakeMethod { get; set; }

    }

    /// <summary>
    /// 配置原始对象映射
    /// </summary>
    public class PrescriptionInfoMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.ForType<PrescriptionInfo, AddPrescriptionInfoDto>()
                .Map(t => t.Pid, o => o.Id);
            config.ForType<PrescriptionInfo, PrescriptionInfoPushInput>()
                .Map(t => t.Pid, o => o.Id);
            config.ForType<AddPrescriptionInfoDto, PrescriptionInfo>()
                .Map(t => t.Id, o => o.Pid);
            config.ForType<PrescriptionInfoPushInput, PrescriptionInfo>()
                .Map(t => t.Id, o => o.Pid);
        }
    }
}
