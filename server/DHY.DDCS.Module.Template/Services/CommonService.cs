/// <summary>
/// 模块服务说明 💥
/// </summary>
[ApiDescriptionSettings(Order = 100)]
public class CommonService(SimpleRepository<SysKeyValue> sysKeyValueService) : IDynamicApiController, ITransient
{
    private readonly SimpleRepository<SysKeyValue> _sysKeyValueService = sysKeyValueService;

    /// <summary>
    /// 分页查询字典表列表
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("分页查询字典表列表")]
    public async Task<SqlSugarPagedList<SysKeyValue>> Page(SysKeyValuePageQueryInput input)
    {
        return await _sysKeyValueService.AsQueryable()
             .WhereIF(input.ParentID.HasValue, s => s.ParentID == input.ParentID)
             .OrderBy(u => new { u.OrderNo })
             .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 增加一个数据字典
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("增加一个数据字典")]
    public virtual async Task<long> AddAsync(SysKeyValue input)
    {
        var isExist = await _sysKeyValueService.AsQueryable().AnyAsync(s => s.KName == input.KName);

        if (isExist)
        {
            throw Oops.Oh(ErrorCodeEnum.S4000, input.KName);
        }

        //var data = input.Adapt<SysKeyValue>();

        return await _sysKeyValueService.AsInsertable(input).ExecuteReturnBigIdentityAsync();
    }

    /// <summary>
    /// 更新一个数据字典
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("更新一个数据字典")]
    public virtual async Task UpdateAsync(SysKeyValue input)
    {
        var exist = await _sysKeyValueService.AsQueryable().FirstAsync(u => u.Id == input.Id);
        if (exist == null)
        {
            throw Oops.Oh(ErrorCodeEnum.S4001, input.Id);
        }

        await _sysKeyValueService.AsUpdateable(input).IgnoreColumns(true).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除一个指定Id的数据字典
    /// </summary>
    /// <param name="input"></param>
    /// <returns>void</returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("删除一个指定ID的数据字典")]
    public virtual async Task DeleteAsync(BaseIdInput input)
    {
        await _sysKeyValueService.DeleteAsync(s => s.Id == input.Id);
    }

    /// <summary>
    /// 查看指定Id信息
    /// </summary>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Get")]
    [DisplayName("查看指定ID信息")]
    public virtual async Task<SysKeyValue> DetailAsync(long id)
    {
        return await _sysKeyValueService.AsQueryable().FirstAsync(u => u.Id == id);
    }

}
