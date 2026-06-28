
using DHY.MG.Module.Sys.Dtos;
using DHY.MG.Module.Sys.Entities;
using DHY.MG.Module.Sys.Enum;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// 服药禁忌规则
/// </summary>
public class ContraindicationMatchRuleService(
    SimpleRepository<ContraindicationMatchRule> contraindicationMatchRuleRepository,
    SimpleRepository<CommonMatchRule> commonMatchRuleRepository,
    SimpleRepository<MedicationGuidance> medicationGuidanceRepository) : IDynamicApiController, ITransient
{
    /// <summary>
    /// 增加服药禁忌规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("增加服药禁忌规则")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task AddAsync(ContraindicationMatchRuleInput input)
    {
        if (contraindicationMatchRuleRepository.AsQueryable().Any(n => n.DrugName == input.DrugName))
        {
            //友好提示
            throw Oops.Oh("该饮片禁忌已存在，请勿重复添加");
        }
        var contraindicationMatchRule = input.Adapt<ContraindicationMatchRule>();
        await contraindicationMatchRuleRepository.AsInsertable(contraindicationMatchRule).ExecuteCommandAsync();

        await AddCommonMatchRule(contraindicationMatchRule);
    }

    private async Task AddCommonMatchRule(ContraindicationMatchRule data)
    {
        await DeletCommonMatchRule(data);

        var commonMatchRules = new List<CommonMatchRule>();
        if (!data.Contraindication.IsNullOrEmpty()) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, data.DrugName, data.Contraindication)); }
        if (data.IsGestationForbid) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsGestationForbid)), sex: SexType.Woman,ageMin:14)); }
        if (data.IsGestationCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsGestationCautious)), sex: SexType.Woman, ageMin: 14)); }
        if (data.IsLactationCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsLactationCautious)), sex: SexType.Woman, ageMin: 14)); }
        if (data.IsMenstruationCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsMenstruationCautious)), sex: SexType.Woman, ageMin: 14)); }
        if (data.IsOldForbid) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsOldForbid)), ageMin: 60)); }
        if (data.IsOldCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsOldCautious)), ageMax: 60)); }
        if (data.IsOldNoLongUse) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsOldNoLongUse)), ageMax: 60)); }
        if (data.IsChildForbid) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsChildForbid)), ageMax: 14)); }
        if (data.IsChildCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsChildCautious)), ageMax: 14)); }
        if (data.IsChildNoLongUse) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsChildNoLongUse)), ageMax: 14)); }
        if (data.IsLandKForbid) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsLandKForbid)))); }
        if (data.IsLandKCautious) { commonMatchRules.Add(new CommonMatchRule(data.DrugName, $"{data.DrugName}", data.GetDescription(nameof(data.IsLandKCautious)))); }

        await commonMatchRuleRepository.InsertRangeAsync(commonMatchRules);
    }

    private async Task DeletCommonMatchRule(ContraindicationMatchRule data)
    {
        var commonMatchRuleIds = await commonMatchRuleRepository.AsQueryable()
            .Where(n => n.DrugName == data.DrugName && n.GuidanceType == data.GuidanceType).Select(n => n.Id).ToListAsync();
        if (commonMatchRuleIds.Count > 0)
        {
            await commonMatchRuleRepository.DeleteByIds(commonMatchRuleIds.Cast<dynamic>().ToArray());
        }
    }

    /// <summary>
    /// 更新服药禁忌规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("更新服药禁忌规则")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task UpdateAsync(UpdateContraindicationMatchRuleInput input)
    {
        if (contraindicationMatchRuleRepository.AsQueryable().Any(n => n.DrugName == input.DrugName && n.Id != input.Id))
        {
            throw Oops.Oh("该饮片禁忌已存在，请勿重复添加");
        }
        var contraindicationMatchRule = input.Adapt<ContraindicationMatchRule>();
        await contraindicationMatchRuleRepository.AsUpdateable(contraindicationMatchRule).ExecuteCommandAsync();

        await AddCommonMatchRule(contraindicationMatchRule);
    }

    /// <summary>
    /// 查询服药禁忌规则
    /// </summary>
    /// <param name="dDCSTask"></param>
    /// <returns></returns>
    [DisplayName("查询服药禁忌规则")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<SqlSugarPagedList<ContraindicationMatchRule>> QueryPageAsync(ContraindicationMatchRuleQuery Query)
    {
        return await contraindicationMatchRuleRepository.AsQueryable()
            .WhereIF(!Query.DrugName.IsNullOrEmpty(), n => n.DrugName.Contains(Query.DrugName))
            .OrderByDescending(n => n.UpdateTime == null ? n.CreateTime : n.UpdateTime)
            .ToPagedListAsync(Query.Page, Query.PageSize);
    }

    /// <summary>
    /// 删除服药禁忌规则
    /// </summary>
    /// <param name="input"></param>
    /// <returns>void</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除服药禁忌规则")]
    public async Task<bool> DeleteAsync(BaseIdInput input)
    {
        var data = await contraindicationMatchRuleRepository.GetByIdAsync(input.Id);
        await DeletCommonMatchRule(data);
        return await contraindicationMatchRuleRepository.DeleteByIdAsync(input.Id);
    }


    /// <summary>
    /// 查看服药禁忌规则
    /// </summary>
    /// <returns></returns>
    [DisplayName("查看服药禁忌规则")]
    [ApiDescriptionSettings(Name = "Detail"), HttpPost]
    public async Task<ContraindicationMatchRule> DetailAsync(BaseIdInput input)
    {
        return await contraindicationMatchRuleRepository.AsQueryable().FirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 保存药嘱信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("保存药嘱信息")]
    [ApiDescriptionSettings(Name = "AddMedicationGuidance"), HttpPost]
    public async Task AddMedicationGuidanceAsync(MedicationGuidanceInput input)
    {
        var medicationGuidanceIds = await medicationGuidanceRepository.AsQueryable().Where(n=> n.PrescriptionNo == input.PrescriptionNo).Select(n=> n.Id).ToListAsync();
        await medicationGuidanceRepository.DeleteByIds(medicationGuidanceIds.Cast<dynamic>().ToArray());

        var medicationGuidances = input.MedicationGuidance.Select(n=> n.Adapt<MedicationGuidance>()).ToList();
        foreach (var item in medicationGuidances)
        {
            if (string.IsNullOrEmpty(item.Content)) { item.Content = "无。"; }
        }
        await medicationGuidanceRepository.InsertRangeAsync(medicationGuidances);


    }


    /// <summary>
    /// 删除药嘱信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("删除药嘱信息")]
    [ApiDescriptionSettings(Name = "DeleteMedicationGuidance"), HttpPost]
    public async Task DeleteMedicationGuidanceAsync(MedicationGuidanceInput input)
    {
        var medicationGuidanceIds = await medicationGuidanceRepository.AsQueryable().Where(n => n.PrescriptionNo == input.PrescriptionNo).Select(n => n.Id).ToListAsync();
        await medicationGuidanceRepository.DeleteByIds(medicationGuidanceIds.Cast<dynamic>().ToArray());
    }

    /// <summary>
    /// 获取药嘱信息
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取药嘱信息")]
    [ApiDescriptionSettings(Name = "MedicationGuidanceInfo"), HttpPost]
    [AllowAnonymous]
    public async Task<List<MedicationGuidance>> MedicationGuidanceInfoAsync(MedicationGuidanceInput input)
    {
        var datas = await medicationGuidanceRepository.AsQueryable().Where(n => n.PrescriptionNo == input.PrescriptionNo).ToListAsync();
        datas.ForEach(n => { n.Content = n.Content.Replace("/", string.Empty); });

        return datas;
    }
}