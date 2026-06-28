using DHY.DDCS.Module.Common.Entity;
using DHY.DDCS.Module.Prescription.Dtos;
using DHY.DDCS.Module.Prescription.Option;
using Furion.DynamicApiController;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;

namespace DHY.DDCS.Module.Prescription.Services;

/// <summary>
/// DDCS处方管理服务 📰
/// </summary>
[ApiDescriptionSettings(Order = 1000)]
public class PrescriptionInfoService(
    IOptions<PrescriptionOptions> prescriptionOptions,
    SimpleRepository<PrescriptionInfo> prescriptionInfoRepository,
    SimpleRepository<DDCSPrescription> ddcsPrescriptionRepository,
    SimpleRepository<PrescriptionDetail> ddcsDrug,
    ILogger<PrescriptionInfoService> logger,
    IHubContext<OnlineUserHub, IOnlineUserHub> onlineUserHubContext,
    SimpleRepository<SysKeyValue> sysKeyValueService
    ) : IDynamicApiController, ITransient
{
    private readonly PrescriptionOptions _prescriptionOptions = prescriptionOptions.Value;
    private readonly SimpleRepository<PrescriptionInfo> _prescriptionInfoRepository = prescriptionInfoRepository;
    private readonly SimpleRepository<DDCSPrescription> _ddcsPrescriptionRepository = ddcsPrescriptionRepository;
    private readonly SimpleRepository<PrescriptionDetail> _ddcsDrug = ddcsDrug;
    private readonly ILogger<PrescriptionInfoService> _logger = logger;
    private static readonly SemaphoreSlim _pushLock = new SemaphoreSlim(1, 1);
    private readonly IHubContext<OnlineUserHub, IOnlineUserHub> _onlineUserHubContext = onlineUserHubContext;

    /// <summary>
    /// 获取原始处方分页列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取原始处方分页列表")]
    public async Task<IEnumerable<PrescriptionInfoOutput>> Page(BaseIdInput input)
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
                    .LeftJoin<SysKeyValue>((p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
                    .LeftJoin<SysKeyValue>((p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
                    .WhereIF(!string.IsNullOrWhiteSpace(input.Id.ToString()), p => p.Id == input.Id)
                    .Select<PrescriptionInfoOutput>("p.*,s.KName as TakeMethod,s1.KName as Decscheme")
                    .ToListAsync();

        return result;
    }

    /// <summary>
    /// 获取处方页面树状列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取处方页面树状列表")]
    [ApiDescriptionSettings(Name = "TreeList"), HttpPost]
    public async Task<IEnumerable<TreeViewModel>> QueryTreeListAsync(PrescriptionInfoQueryInput input)
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.PrescriptionNo), p => p.PrescriptionNo == input.PrescriptionNo)
            .WhereIF(!string.IsNullOrWhiteSpace(input.PatientName), p => p.PatientName == input.PatientName)
            .WhereIF(input.State.HasValue, p => p.State == input.State)
            //.Where(p => p.CreateTime.ToDateTime().Date == DateTime.Today)
            .LeftJoin<SysKeyValue>((p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
            .LeftJoin<SysKeyValue>((p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> dDCSPrescriptions)
        {
            var stateGroupedResult = dDCSPrescriptions.GroupBy(p => GetPrescriptionInfoTaskGroup(p.DDCSStatus)).ToList();
            foreach (var stateGroup in stateGroupedResult)
            {
                //key is state
                var treeModelState = new TreeViewModel
                {
                    Label = stateGroup.Key,
                    Children = new()
                };
                var patientGroupResult = stateGroup.GroupBy(p => p.PatientName);

                foreach (var prescriptionPatientGroup in patientGroupResult)
                {
                    //key is patientName
                    var patientTreeNode = new TreeViewModel
                    {
                        Label = prescriptionPatientGroup.Key,
                        Children = new()
                    };
                    treeModelState.Children.Add(patientTreeNode);

                    foreach (var item in prescriptionPatientGroup)
                    {
                        // 原始处方节点
                        var prescriptionNode = new TreeViewModel
                        {
                            Id = item.Id,
                            Label = item.PrescriptionNo,
                            Children = item.DDCSPrescriptions?.Any() == true ? new List<TreeViewModel>() : null,
                        };

                        patientTreeNode.Children.Add(prescriptionNode);

                        //拆方节点
                        if (item.DDCSPrescriptions?.Any() == true)
                        {
                            if (item.DDCSPrescriptions.Count > 1)
                            {
                                patientTreeNode.Label += "【拆】";
                            }
                            foreach (var splitItem in item.DDCSPrescriptions)
                            {
                                prescriptionNode.Children.Add(new()
                                {
                                    Id = splitItem.Id,
                                    Label = $"{splitItem.Index}-{splitItem.Pid}-{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescriptionPatientGroup.Key} {item.PrescriptionNo})",
                                });
                            }
                        }
                    }
                }

                yield return treeModelState;
            }

            //var treeModel = new TreeViewModel
            //{
            //    Label = "今日处方",
            //    Children = new()
            //};

            //var todayPatientGroupResult = result.Where(a => a.CreateTime.ToDateTime().Date == DateTime.Today).GroupBy(p => p.PatientName);

            //foreach (var prescriptionPatientGroup in todayPatientGroupResult)
            //{
            //    //key is patientName
            //    var patientTreeNode = new TreeViewModel
            //    {
            //        Label = prescriptionPatientGroup.Key,
            //        Children = new()
            //    };
            //    treeModel.Children.Add(patientTreeNode);

            //    foreach (var item in prescriptionPatientGroup)
            //    {
            //        // 原始处方节点
            //        var prescriptionNode = new TreeViewModel
            //        {
            //            Id = item.Id,
            //            Label = item.PrescriptionNo,
            //            Children = item.DDCSPrescriptions?.Any() == true ? new List<TreeViewModel>() : null,
            //        };

            //        patientTreeNode.Children.Add(prescriptionNode);

            //        //拆方节点
            //        if (item.DDCSPrescriptions?.Any() == true)
            //        {
            //            if (item.DDCSPrescriptions.Count > 1)
            //            {
            //                patientTreeNode.Label += "【拆】";
            //            }
            //            foreach (var splitItem in item.DDCSPrescriptions)
            //            {
            //                prescriptionNode.Children.Add(new()
            //                {
            //                    Id = splitItem.Id,
            //                    Label = $"{splitItem.Index}-{splitItem.Pid}-{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescriptionPatientGroup.Key} {item.PrescriptionNo})",
            //                });
            //            }
            //        }
            //    }
            //}

            //yield return treeModel;
        }
    }

    /// <summary>
    /// 获取处方页面树状列表，药嘱使用
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取处方页面树状列表，药嘱使用")]
    [ApiDescriptionSettings(Name = "TreeListNew"), HttpPost]
    public async Task<IEnumerable<TreeViewModel>> QueryTreeListNewAsync(PrescriptionInfoQueryInput input)
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.PrescriptionNo), p => p.PrescriptionNo == input.PrescriptionNo)
            .WhereIF(!string.IsNullOrWhiteSpace(input.PatientName), p => p.PatientName == input.PatientName)
            .WhereIF(input.State.HasValue, p => p.State == input.State)
            //.Where(p => p.CreateTime.ToDateTime().Date == DateTime.Today)
            .LeftJoin<SysKeyValue>((p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
            .LeftJoin<SysKeyValue>((p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> dDCSPrescriptions)
        {
            var stateGroupedResult = dDCSPrescriptions.GroupBy(p => GetPrescriptionInfoTaskGroup(p.State)).OrderBy(n => n.Key);

            foreach (var stateGroup in stateGroupedResult)
            {
                //key is state
                var treeModelState = new TreeViewModel
                {
                    Label = stateGroup.Key,
                    Children = new()
                };
                var patientGroupResult = stateGroup.GroupBy(p => p.PatientName);

                foreach (var prescriptionPatientGroup in patientGroupResult)
                {
                    //key is patientName
                    var patientTreeNode = new TreeViewModel
                    {
                        Label = prescriptionPatientGroup.Key,
                        Children = new()
                    };
                    treeModelState.Children.Add(patientTreeNode);

                    foreach (var item in prescriptionPatientGroup)
                    {
                        // 原始处方节点
                        var prescriptionNode = new TreeViewModel
                        {
                            Id = item.Id,
                            Label = item.PrescriptionNo,
                            Children = item.DDCSPrescriptions?.Any() == true ? new List<TreeViewModel>() : null,
                        };

                        patientTreeNode.Children.Add(prescriptionNode);

                    }
                }

                yield return treeModelState;
            }

        }
    }

    /// <summary>
    /// 获取调剂信息页面树状列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取调剂信息页面树状列表")]
    [ApiDescriptionSettings(Name = "DispensingTreeList"), HttpGet]
    public async Task<IEnumerable<TreeViewModel>> QueryDispensingTreeListAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(a => a.State > PrescriptionManageStatusEnum.审核)
            .Where(a => a.CreateTime.Value.Date >= DateTime.Today)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> prescriptionInfos)
        {
            foreach (var prescription in prescriptionInfos)
            {
                //原始处方节点
                var originalPrescriptionNode = new TreeViewModel
                {
                    Label = prescription.PatientName + "-" + prescription.PrescriptionNo + "[代]",
                    Children = new()
                };

                //拆方节点
                if (prescription.DDCSPrescriptions?.Any() == true)
                {
                    foreach (var splitItem in prescription.DDCSPrescriptions)
                    {
                        originalPrescriptionNode.Children.Add(new()
                        {
                            Id = splitItem.Id,
                            Label = $"{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}-{splitItem.Id}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescription.PrescriptionNo} {prescription.PatientName})",
                        });
                    }
                }
                yield return originalPrescriptionNode;
            }
        }
    }

    /// <summary>
    /// 获取煎煮信息页面树状列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取煎煮信息页面树状列表")]
    [ApiDescriptionSettings(Name = "DecoctionTreeList"), HttpGet]
    public async Task<IEnumerable<TreeViewModel>> QueryDecoctionTreeListAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(a => a.State > PrescriptionManageStatusEnum.调剂)
            .Where(a => a.CreateTime.Value.Date >= DateTime.Today)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> prescriptionInfos)
        {
            foreach (var prescription in prescriptionInfos)
            {
                //原始处方节点
                var originalPrescriptionNode = new TreeViewModel
                {
                    Label = prescription.PatientName + "-" + prescription.PrescriptionNo + "[代]",
                    Children = new()
                };

                //拆方节点
                if (prescription.DDCSPrescriptions?.Any() == true)
                {
                    foreach (var splitItem in prescription.DDCSPrescriptions)
                    {
                        originalPrescriptionNode.Children.Add(new()
                        {
                            Id = splitItem.Id,
                            Label = $"{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}-{splitItem.Id}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescription.PrescriptionNo} {prescription.PatientName})",
                        });
                    }
                }
                yield return originalPrescriptionNode;
            }
        }
    }

    /// <summary>
    /// 获取包装信息页面树状列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取包装信息页面树状列表")]
    [ApiDescriptionSettings(Name = "PackingTreeList"), HttpGet]
    public async Task<IEnumerable<TreeViewModel>> QueryPackingTreeListAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(a => a.State > PrescriptionManageStatusEnum.煎药)
            .Where(a => a.CreateTime.Value >= DateTime.Today)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> prescriptionInfos)
        {
            foreach (var prescription in prescriptionInfos)
            {
                //原始处方节点
                var originalPrescriptionNode = new TreeViewModel
                {
                    Label = prescription.PatientName + "-" + prescription.PrescriptionNo + "[代]",
                    Children = new()
                };

                //拆方节点
                if (prescription.DDCSPrescriptions?.Any() == true)
                {
                    foreach (var splitItem in prescription.DDCSPrescriptions)
                    {
                        originalPrescriptionNode.Children.Add(new()
                        {
                            Id = splitItem.Id,
                            Label = $"{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}-{splitItem.Id}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescription.PrescriptionNo} {prescription.PatientName})",
                        });
                    }
                }
                yield return originalPrescriptionNode;
            }
        }
    }

    /// <summary>
    /// 获取容器运行状态信息页面树状列表
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取容器运行状态信息页面树状列表")]
    [ApiDescriptionSettings(Name = "ContainerViewTreeList"), HttpGet]
    public async Task<IEnumerable<TreeViewModel>> QueryTreeListByContainerNoAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(a => a.State > PrescriptionManageStatusEnum.审核 && a.State <= PrescriptionManageStatusEnum.包装)
            .Where(a => a.CreateTime.Value >= DateTime.Today.AddDays(-1))
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return CreateTreeList(result);

        IEnumerable<TreeViewModel> CreateTreeList(List<PrescriptionInfo> prescriptionInfos)
        {
            foreach (var prescription in prescriptionInfos)
            {
                //原始处方节点
                var originalPrescriptionNode = new TreeViewModel
                {
                    Id = prescription.Id,
                    Label = prescription.PatientName + "-" + prescription.PrescriptionNo + "[代]",
                    Children = new()
                };

                //拆方节点
                if (prescription.DDCSPrescriptions?.Any() == true)
                {
                    foreach (var splitItem in prescription.DDCSPrescriptions)
                    {
                        originalPrescriptionNode.Children.Add(new()
                        {
                            Id = splitItem.Id,
                            Label = $"{splitItem.DecoctionType.GetAttributeOfType<DescriptionAttribute>()?.Description ?? "未知"}-{splitItem.Id}　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　({prescription.PrescriptionNo} {prescription.PatientName})",
                        });
                    }
                }
                yield return originalPrescriptionNode;
            }
        }
    }

    /// <summary>
    /// 根据处方状态码获取煎药系统处方状态
    /// </summary>
    /// <param name="state">处方状态</param>
    /// <returns></returns>
    [DisplayName("根据处方状态码获取煎药系统处方状态")]
    [ApiDescriptionSettings(Name = "StatusDescript")]
    public string GetPrescriptionInfoStateDescript(byte state) => ((PrescriptionManageStatusEnum)state).ToString();

    /// <summary>
    /// 获取页面大分组状态
    /// 拆方提供接口通过任务修改处方状态，在绑桶前都算是未开始，到成品复核前都算是进行中
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    private string GetPrescriptionInfoTaskGroup(PrescriptionStatusEnum state) => (int)state switch
    {
        <= 1 => "未开始",
        >= 2 => "已完成药嘱",
    };

    /// <summary>
    /// 获取页面大分组状态
    /// 拆方提供接口通过任务修改处方状态，在绑桶前都算是未开始，到成品复核前都算是进行中
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    private string GetPrescriptionInfoTaskGroup(PrescriptionManageStatusEnum state) => (int)state switch
    {
        <= 1 => "未开始",
        >= 2 => "已完成药嘱",
    };

    /// <summary>
    /// 增加一个处方
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加一个处方")]
    public async Task<long> AddPriscriptionInfoAsync(AddPrescriptionInfoDto input)
    {
        var isExist = await _prescriptionInfoRepository.IsAnyAsync(p => p.PrescriptionNo == input.PrescriptionNo);

        if (isExist)
        {
            throw Oops.Oh(PrescriptionErrorCodeEnum.P1000, input.PrescriptionNo);
        }

        var prescription = input.Adapt<PrescriptionInfo>();

        var result = await _prescriptionInfoRepository.Context.InsertNav(prescription).Include(p => p.Details, new InsertNavOptions() { OneToManyIfExistsNoInsert = true }).ExecuteReturnEntityAsync();
        return result.Id;

    }

    [NonAction]
    public async Task<bool> AddPriscriptionInfoNavAsync(PrescriptionInfo prescription)
    {
        return await _prescriptionInfoRepository.Context.InsertNav(prescription)
            .Include(sp => sp.DDCSPrescriptions).ThenInclude(sp => sp.Details)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 查看指定ID处方信息
    /// </summary>
    /// <returns>包含处方明细的指定ID处方信息</returns>
    [DisplayName("查看指定ID处方信息")]
    [ApiDescriptionSettings(Name = "Detail"), HttpPost]
    public async Task<PrescriptionInfoOutput> QueryPrescriptionInfoAsync(BaseIdInput input)
    {
        var prescriptionInfo = await _prescriptionInfoRepository.AsQueryable()
          .LeftJoin<SysKeyValue>((p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
          .LeftJoin<SysKeyValue>((p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
          .LeftJoin<SysKeyValue>((p, s, s1, d) => p.DeliveryMethodId.ToString() == d.Kvalue && d.KType == 10)
          .WhereIF(!string.IsNullOrWhiteSpace(input.Id.ToString()), p => p.Id == input.Id)
          .Select<PrescriptionInfoOutput>("p.*,s.KName as TakeMethod,s1.KName as Decscheme,d.KName as DeliveryMethod").FirstAsync();

        return prescriptionInfo.Adapt<PrescriptionInfoOutput>();
    }

    /// <summary>
    /// 查看指定ID处方信息,用于App端展示
    /// </summary>
    /// <returns>包含处方明细的指定ID处方信息</returns>
    [DisplayName("查看指定ID处方信息,用于App端展示")]
    [ApiDescriptionSettings(Name = "AppDetail"), HttpPost]
    [AllowAnonymous]
    public async Task<PrescriptionInfoAppDto> QueryPrescriptionInfoForAppAsync(PrescriptionInfoAppInput input)
    {
        var prescriptionInfo = await _prescriptionInfoRepository.AsQueryable()
                .Includes(n => n.Details)
                .WhereIF(!input.PrescriptionNo.IsNullOrEmpty(), p => p.PrescriptionNo == input.PrescriptionNo)
                .FirstAsync();

        return prescriptionInfo.Adapt<PrescriptionInfoAppDto>();
    }

    /// <summary>
    /// 获取拆分处方信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取拆分处方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "DetialWithDDCSPrescription")]
    public async Task<PrescriptionInfoOutput> QueryDDCSPrescriptionDetialAsync(BaseIdInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable()
                    .LeftJoin<PrescriptionInfo>((sp, p) => sp.Pid == p.Id)
                    .LeftJoin<SysKeyValue>((sp, p, s) => p.Usage.ToString() == s.Kvalue && s.KType == 7)
                    .LeftJoin<SysKeyValue>((sp, p, s, s1) => p.DecoctionScheme.ToString() == s1.Kvalue && s1.KType == 8)
                    .LeftJoin<SysKeyValue>((sp, p, s, s1, d) => p.DeliveryMethodId.ToString() == d.Kvalue && d.KType == 10)
                    .WhereIF(!string.IsNullOrWhiteSpace(input.Id.ToString()), sp => sp.Id == input.Id)
                    .Select<PrescriptionInfoOutput>("sp.Id as Id,p.PrescriptionNo,p.PatientName,p.State,p.Dosage,p.Frequency,p.GroupWater,p.PackageNum,p.GroupSoakWaterTime,p.Cancellation,s.KName as TakeMethod,s1.KName as Decscheme,p.DDCSStatus,sp.WaterAmount,d.KName as DeliveryMethod,sp.Priority,sp.TargetVolumn,sp.BrothWeight,sp.ContainerNo")
                    .FirstAsync();

        return result;
    }


    /// <summary>
    /// 根据拆方Id获取原始处方信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("根据拆方Id获取原始处方信息"), HttpPost]
    [ApiDescriptionSettings(Name = "QueryPrescriptionInfoByDDCSPid")]
    public async Task<PrescriptionInfo> QueryPrescriptionInfoByDDCSPidAsync(BaseIdInput input)
    {
        var result = await _ddcsPrescriptionRepository.AsQueryable()
            .Where(sp => sp.Id == input.Id)
                    .LeftJoin<PrescriptionInfo>((sp, p) => sp.Pid == p.Id)
                    .Select((sp, p) => p)
                    .FirstAsync();

        return result;
    }

    /// <summary>
    /// 获取处方药品信息
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取处方药品信息")]
    [ApiDescriptionSettings(Name = "SubDetails"), HttpPost]
    public async Task<IEnumerable<PrescriptionDetail>> QueryDetailsAsync(BaseIdInput input)
    {
        var drugInfoList = await _ddcsDrug.AsQueryable().Where(u => u.Pid == input.Id).ToListAsync();

        return drugInfoList.Adapt<List<PrescriptionDetail>>();
    }

    private int CalcTargetVolumn(ref readonly PrescriptionInfo prescriptionInfo, int discardPackNum)
    {
        return prescriptionInfo.Dosage * prescriptionInfo.Frequency * prescriptionInfo.PackageNum + discardPackNum * prescriptionInfo.PackageNum;
    }




    /// <summary>
    /// 获取兼容煎药系统的处方状态
    /// </summary>
    /// <param name="prescriptionStatus"></param>
    /// <returns></returns>
    [NonAction]
    public PrescriptionManageStatusEnum CompatibleManagementSystem(PrescriptionStatusEnum prescriptionStatus) => prescriptionStatus switch
    {
        PrescriptionStatusEnum.SentContainer => PrescriptionManageStatusEnum.调剂,
        PrescriptionStatusEnum.BindContainer => PrescriptionManageStatusEnum.调剂,
        PrescriptionStatusEnum.Dispensing => PrescriptionManageStatusEnum.调剂,
        PrescriptionStatusEnum.Replenish => PrescriptionManageStatusEnum.调剂,
        PrescriptionStatusEnum.Recheck => PrescriptionManageStatusEnum.调剂,
        PrescriptionStatusEnum.FillWater => PrescriptionManageStatusEnum.泡药,
        PrescriptionStatusEnum.Soak => PrescriptionManageStatusEnum.泡药,
        PrescriptionStatusEnum.Decoction => PrescriptionManageStatusEnum.煎药,
        PrescriptionStatusEnum.Packing => PrescriptionManageStatusEnum.包装,
        _ => PrescriptionManageStatusEnum.未知,
    };

    /// <summary>
    /// 修改原始处方状态
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UpdateStatus"), HttpPost]
    [DisplayName("修改原始处方状态")]
    public async Task UpdatePrescriptionInfoStatus(UpdatePrescriptionInfoStatusInput input)
    {
        var prescriptionInfo = await _prescriptionInfoRepository.GetByIdAsync(input.Pid);
        if (prescriptionInfo != null)
        {
            await _prescriptionInfoRepository.AsUpdateable(new PrescriptionInfo { Id = input.Pid, State = input.State })
            .UpdateColumns(p => new { p.State })
            .ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 修改处方优先级
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UpdatePriority"), HttpPost]
    [DisplayName("修改处方优先级")]
    public async Task UpdatePrescriptionPriorityAsync(UpdatePrescriptionPriorityInput input)
    {
        var prescriptionInfo = await _prescriptionInfoRepository.GetByIdAsync(input.Pid);

        if (prescriptionInfo != null)
        {
            try
            {
                await _prescriptionInfoRepository.Context.Ado.BeginTranAsync();
                await _prescriptionInfoRepository.AsUpdateable(new PrescriptionInfo { Id = input.Pid, Priority = input.Priority })
                .UpdateColumns(p => new { p.Priority })
                .ExecuteCommandAsync();

                var ddcsPrescription = await _ddcsPrescriptionRepository.AsQueryable().Where(x => x.Pid == input.Pid).ToListAsync();
                ddcsPrescription.ForEach(detail => detail.Priority = input.Priority);
                await _ddcsPrescriptionRepository.Context.Updateable(ddcsPrescription).ExecuteCommandAsync();
                await _prescriptionInfoRepository.Context.Ado.CommitTranAsync();
            }
            catch
            {
                await _prescriptionInfoRepository.Context.Ado.RollbackTranAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// 删除处方的拆方和拆方明细及映射
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [NonAction]
    public async Task<bool> DeleteByIdAsync(long id)
    {
        return await _prescriptionInfoRepository.Context.DeleteNav<PrescriptionInfo>(p => p.Id == id)
            .Include(d => d.DDCSPrescriptions, new DeleteNavOptions()
            {
                ManyToManyIsDeleteA = false,
                ManyToManyIsDeleteB = true,
            }).ThenInclude(s => s.Details)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 查询当天拆分的处方信息（拆分数量大于1的处方）包括处方号/姓名/桶号/快递类型
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "SplitPrescriptionInfo"), HttpPost]
    [DisplayName("查询当天拆分的处方信息")]
    public async Task<IEnumerable<PrescriptionInfoOutput>> QuerySplitPrescriptionInfoAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(p => p.CreateTime.ToDateTime().Date == DateTime.Today)
            .Where(p => p.DDCSPrescriptions.Count() > 1)
            .LeftJoin<SysKeyValue>((p, s) => p.DeliveryMethodId.ToString() == s.Kvalue && s.KType == 10)
            .OrderByDescending(p => p.CreateTime)
            .Select<PrescriptionInfoOutput>("p.*,s.KName as DeliveryMethod")
            .ToListAsync();

        var ddcsPrescriptions = _ddcsPrescriptionRepository.AsQueryable()
            .Where(x => result.Any(y => y.Id == x.Pid) && x.ContainerNo != null).ToList();


        result.ForEach(item =>
                    item.ContainerNos = string.Join(",", ddcsPrescriptions
                    .Where(x => x.Pid == item.Id)
                    .Select(x => x.ContainerNo.ToString())
                    .ToList())
        );
        return result;
    }

    /// <summary>
    /// 查询当天拆分的处方患者姓名（拆分数量大于1的处方）
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetSplitPatientNames"), HttpPost]
    [DisplayName("查询当天拆分的处方患者姓名（拆分数量大于1的处方）")]
    public async Task<string> QuerySplitPatientNamesAsync()
    {
        var result = await _prescriptionInfoRepository.AsQueryable()
            .Where(p => p.CreateTime.ToDateTime().Date == DateTime.Today)
            .Where(p => p.DDCSPrescriptions.Count() > 1)
            .Includes(p => p.DDCSPrescriptions)
            .OrderByDescending(p => p.CreateTime)
            .ToListAsync();

        return string.Join("、", result.Select(x => x.PatientName + "(" + x.DDCSPrescriptions.Count + ")"));
    }

    /// <summary>
    /// 主页-获取处方按状态完成情况统计信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("主页-获取处方按状态完成情况统计信息")]
    [ApiDescriptionSettings(Name = "PrescriptionInfoStatistics"), HttpGet]
    public async Task<IEnumerable<PrescriptionCompletionStatusInfoOutput>> QueryPrescriptionCompletionStatusStatisticsAsync()
    {
        //总量50/未完成量40/完成率20%
        var todayPrescriptionInfoList = await _prescriptionInfoRepository.AsQueryable().Where(p => p.CreateTime.ToDateTime().Date == DateTime.Today).ToListAsync();

        var result = new List<PrescriptionCompletionStatusInfoOutput>
        {
            //今日处方50/未完成10
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.包装).Count(),
            },
            //已调剂/未调剂
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Where(p => p.State >= PrescriptionManageStatusEnum.调剂).Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.调剂).Count(),
            },
            //已复核/未复核
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Where(p => p.State >= PrescriptionManageStatusEnum.复核).Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.复核).Count(),
            },
            //已浸泡/未浸泡
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Where(p => p.State >= PrescriptionManageStatusEnum.泡药).Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.泡药).Count(),
            },
            //已煎煮/未煎煮
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Where(p => p.State >= PrescriptionManageStatusEnum.煎药).Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.煎药).Count(),
            },
            //已包装/未包装
            new()
            {
                CompletedNum = todayPrescriptionInfoList.Where(p => p.State >= PrescriptionManageStatusEnum.包装).Count(),
                UnfinishedNum = todayPrescriptionInfoList.Where(p => p.State < PrescriptionManageStatusEnum.包装).Count(),
            }
        };
        return result;
    }



}
