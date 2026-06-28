using DHY.DDCS.Module.Common.Entity;
using DHY.DDCS.Module.Prescription.Dtos;
using DHY.InternalApiService.Dtos;
using Furion.DynamicApiController;

/// <summary>
/// DDCS拆方服务 📚
/// </summary>
[ApiDescriptionSettings(Order = 1000)]
public class DDCSPrescriptionService(
    SimpleRepository<DDCSPrescription> ddcsPrescriptionRepository,
    SimpleRepository<DDCSPrescriptionDetail> ddcsPrescriptionDetailRepository) : IDynamicApiController, ITransient
{
    private readonly SimpleRepository<DDCSPrescription> _ddcsPrescriptionRepository = ddcsPrescriptionRepository;
    private readonly SimpleRepository<DDCSPrescriptionDetail> _ddcsPrescriptionDetailRepository = ddcsPrescriptionDetailRepository;

    /// <summary>
    /// 获取拆分处方分页列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取拆分处方分页列表")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<IEnumerable<DDCSPrescription>> PageAsync(BaseIdInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable()
                    .LeftJoin<PrescriptionInfo>((sp, p) => sp.Pid == p.Id)
                    .LeftJoin<SysKeyValue>((sp, p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
                    .LeftJoin<SysKeyValue>((sp, p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
                    .WhereIF(!string.IsNullOrWhiteSpace(input.Id.ToString()), sp => sp.Id == input.Id)
                    .Select<DDCSPrescription>("sp.Id as Id,p.PrescriptionNo,p.PatientName,p.State,p.Dosage,p.Frequency,p.GroupWater,p.PackageNum,p.PreSoakWater,p.GroupSoakWater,p.Cancellation,p.PreWater,s.KName as TakeMethod,s1.KName as Decscheme")
                    .ToListAsync();

        return result;
    }

    [NonAction]
    public async Task<bool> AddDDCSPriscriptionInfoAsync(List<DDCSPrescription> ddcsPrescriptions)
    {
        return await _ddcsPrescriptionRepository.Context.InsertNav(ddcsPrescriptions).Include(sp => sp.Details).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据Id获取拆分处方信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("根据Id获取拆分处方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "Detail")]
    public async Task<DDCSPrescriptionOutput> DetailAsync(BaseIdInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable()
            .Includes(p => p.Details)
            .FirstAsync(sp => sp.Id == input.Id);

        return result.Adapt<DDCSPrescriptionOutput>();
    }

    /// <summary>
    /// 更新DDCS处方使用的桶号
    /// </summary>
    /// <returns></returns>
    [DisplayName("更新DDCS处方使用的桶号"), HttpPost]
    [ApiDescriptionSettings(Name = "UpdateContainerNo")]
    public async Task<bool> UpdateContainerNoAsync(UpdateDDCSPrescriptionContainerNoInput input)
    {
        var result = await _ddcsPrescriptionRepository.UpdateAsync(d => new DDCSPrescription { ContainerNo = input.ContainerNo }, sp => sp.Id == input.DDCSPid);

        return result;
    }


    [DisplayName("更新DDCS处方当前信息")]
    [ApiDescriptionSettings(Name = "UpdateCurrentInfo"), HttpPost]
    public async Task<bool> UpdateCurrentInfoAsync(UpdateDDCSPrescriptionCurrentInfoInput input)
    {
        var rt = await _ddcsPrescriptionRepository.AsUpdateable()
             .SetColumnsIF(input.DecoctFirstContainerNo != null, a => a.DecoctFirstContainerNo == input.DecoctFirstContainerNo)
             .SetColumnsIF(input.DecoctLaterContainerNo != null, a => a.DecoctLaterContainerNo == input.DecoctLaterContainerNo)
             .SetColumnsIF(input.StorageContainerNo != null, a => a.StorageContainerNo == input.StorageContainerNo)
             .SetColumnsIF(input.StorageDecoctorNo != null, a => a.StorageDecoctorNo == input.StorageDecoctorNo)
             .Where(a => a.Id == input.Id)
             .ExecuteCommandAsync();
        return rt > 0;

    }
    /// <summary>
    /// 查看拆分的处方信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("查看拆分的处方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "List")]
    public async Task<IEnumerable<DDCSPrescriptionOutput>> QueryDDCSPrescriptionVMAsync(BaseIdInput input)
    {
        var prescriptionInfoList = await _ddcsPrescriptionRepository
            .AsQueryable()
            .Includes(p => p.Details)
            .Where(u => u.Pid == input.Id)
            .ToListAsync();

        return prescriptionInfoList.Adapt<IEnumerable<DDCSPrescriptionOutput>>();
    }

    /// <summary>
    /// 获取拆分的处方药品信息
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取拆分的处方药品信息"), HttpPost]
    [ApiDescriptionSettings(Name = "SubDetails")]
    public async Task<IEnumerable<DDCSPrescriptionDetail>> QueryDDCSPrescriptionDetailsAsync(BaseIdInput input)
    {
        var drugInfoList = await _ddcsPrescriptionDetailRepository.AsQueryable().Where(u => u.DDCSPid == input.Id).ToListAsync();

        return drugInfoList.Adapt<IEnumerable<DDCSPrescriptionDetail>>();
    }

    /// <summary>
    /// 根据处方Id更新处方目标得液量
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据处方Id更新处方目标得液量"), HttpPost]
    [ApiDescriptionSettings(Name = "UpdateVolumnById")]
    public async Task<bool> UpdateVolumnByIdAsync(UpdateDDCSPrescriptionInput input)
    {
        var result = await ddcsPrescriptionRepository.AsUpdateable()
            .SetColumnsIF(input.TargetVolumn.HasValue, u => u.TargetVolumn == input.TargetVolumn)
            .SetColumnsIF(input.RealVolumn.HasValue, u => u.BrothWeight == input.RealVolumn)
            .Where(it => it.Id == input.DDCSPid)
            .ExecuteCommandAsync();
        return result > 0;
    }

    /// <summary>
    /// 根据处方Id更新处方对应的包装机设备号
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据处方Id更新处方对应的包装机设备号量"), HttpPost]
    [ApiDescriptionSettings(Name = "UpdatePackagingNoById")]
    public async Task<bool> UpdatePackagingNoByIdAsync(UpdateDDCSPrescriptionInput input)
    {
        var result = await _ddcsPrescriptionDetailRepository.Context.Updateable<DDCSPrescription>()
                       .SetColumns(it => new DDCSPrescription() { PackagingNo = input.PackagingNo })
                       .Where(it => it.Id == input.DDCSPid)
                       .ExecuteCommandAsync();
        return result > 0;
    }

    /// <summary>
    /// 获取同一处方下不包含当前拆方信息并且包装机设备号为null的其他拆方信息
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取同一处方下不包含当前拆方信息并且包装机设备号为null的其他拆方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryDDCSPrescriptionInfo")]
    public async Task<List<DDCSPrescriptionOutput>> QueryDDCSPrescriptionInfoAsync(DDCSPrescriptionQuertInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable().Where(s => s.Pid == input.Pid && s.Id != input.Id && s.PackagingNo == null).ToListAsync();
        return result.Adapt<List<DDCSPrescriptionOutput>>();
    }

    /// <summary>
    /// 根据Pid，包装机设备号不为null条件获取处方信息
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据Pid，包装机设备号不为null条件获取处方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryDDCSPrescriptionInfoByPid")]
    public async Task<DDCSPrescriptionOutput> QueryDDCSPrescriptionInfoByPidAsync(DDCSPrescriptionQuertInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable().Where(s => s.Pid == input.Pid && s.PackagingNo != null).FirstAsync();
        return result.Adapt<DDCSPrescriptionOutput>();
    }

    /// <summary>
    /// 根据Pid，查询处方
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("根据Pid查询处方列表"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryDDCSPrescriptionInfosByPid")]
    public async Task<List<DDCSPrescriptionOutput>> QueryDDCSPrescriptionInfosByPidAsync(DDCSPrescriptionQuertInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable().Where(s => s.Pid == input.Pid).ToListAsync();

        return result.Adapt<List<DDCSPrescriptionOutput>>();
    }
    /// <summary>
    /// 查询当前调剂区工作桶数
    /// </summary>
    /// <returns></returns>
    [DisplayName("查询当前调剂区工作桶数"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryCurrentDispensingContainerCount")]
    public async Task<int> QueryCurrentDispensingContainerCountAsync()
    {
         return await _ddcsPrescriptionRepository.AsQueryable().Where(s=>
         s.DDCSTaskStatus> PrescriptionStatusEnum.SentContainer&&
         s.DDCSTaskStatus<PrescriptionStatusEnum.FillWater&&
         s.ContainerNo!=null

         ).CountAsync();
    }
    /// <summary>
    /// 查询当前煎煮区工作桶数
    /// </summary>
    /// <returns></returns>
    [DisplayName("查询当前煎煮区工作桶数"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryCurrentDecoctingContainerCount")]
    public async Task<int> QueryCurrentDecoctingContainerCountAsync()
    {
        return await _ddcsPrescriptionRepository.AsQueryable().Where(s =>
        s.DDCSTaskStatus >= PrescriptionStatusEnum.FillWater &&
        s.DDCSTaskStatus < PrescriptionStatusEnum.Completed &&
        s.ContainerNo != null

        ).CountAsync();
    }

    /// <summary>
    /// 查询今日绑桶的拆方记录
    /// </summary>
    /// <returns></returns>
    [DisplayName("查询今日绑桶的拆方记录"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryBindingContainerDDCSPrescriptionToday")]
    public async Task<IEnumerable<BindingContainerDDCSPrescriptionOutput>> QueryBindingContainerDDCSPrescriptionTodayAsync()
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable()
             .Where(d => d.CreateTime.ToDateTime().Date == DateTime.Today && d.ContainerNo != null)
             .LeftJoin<PrescriptionInfo>((d, p) => d.Pid == p.Id)
             //.LeftJoin<SysKeyValue>((d, p,s) => p.DeliveryMethodId.ToString() == s.Kvalue && s.KType == 10)
             .OrderByDescending(d => d.CreateTime)
             .Select<BindingContainerDDCSPrescriptionOutput>("p.PatientName,p.PrescriptionNo,d.WaterAmount,d.ContainerNo,d.CreateTime,d.DecoctionType")
             .ToListAsync();

        return result;
    }

    [NonAction]
    public async Task<List<DDCSPrescription>> QueryDDCSPrescriptionDetailsByPidAsync(long pid)
    {
        return await _ddcsPrescriptionRepository.AsQueryable().Where(s => s.Pid == pid).Includes(s => s.Details).ToListAsync();
    }

    [NonAction]
    public async Task<List<long>> QueryDDCSPrescriptionIdsByPidAsync(long pid)
    {
        return await _ddcsPrescriptionRepository.AsQueryable().Where(d => d.Pid == pid).Select(d => d.Id).ToListAsync();
    }
}
