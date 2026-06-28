namespace DHY.Core.Service;

/// <summary>
/// 系统常量服务 💥
/// </summary>
[ApiDescriptionSettings(Order = 280, Groups = [CommonConst.SysGroupName])]
[AllowAnonymous]
public class SysConstService : IDynamicApiController, ITransient
{
    private readonly SysCacheService _sysCacheService;

    public SysConstService(SysCacheService sysCacheService)
    {
        _sysCacheService = sysCacheService;
    }

    /// <summary>
    /// 获取所有常量列表 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取所有常量列表")]
    public async Task<List<ConstOutput>> GetList()
    {
        var key = $"{CacheConst.KeyConst}list";
        var constlist = _sysCacheService.Get<List<ConstOutput>>(key);
        if (constlist == null)
        {
            var typeList = GetConstAttributeList();
            constlist = typeList.Select(u => new ConstOutput
            {
                Name = u.CustomAttributes.ToList().FirstOrDefault()?.ConstructorArguments.ToList().FirstOrDefault().Value?.ToString() ?? u.Name,
                Code = u.Name,
                Data = GetData(Convert.ToString(u.Name))
            }).ToList();
            _ = _sysCacheService.Set(key, constlist);
        }
        return await Task.FromResult(constlist);
    }

    /// <summary>
    /// 根据类名获取常量数据 🔖
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    [DisplayName("根据类名获取常量数据")]
    public async Task<List<ConstOutput>> GetData([Required] string typeName)
    {
        var key = $"{CacheConst.KeyConst}{typeName.ToUpper()}";
        var constlist = _sysCacheService.Get<List<ConstOutput>>(key);
        if (constlist == null)
        {
            var typeList = GetConstAttributeList();
            var type = typeList.FirstOrDefault(u => u.Name == typeName);

            var isEnum = type.BaseType.Name == "Enum";
            constlist = type.GetFields()?
                .Where(isEnum, u => u.FieldType.Name == typeName)
                .Select(u => new ConstOutput
                {
                    Name = u.Name,
                    Code = isEnum ? (int)u.GetValue(BindingFlags.Instance) : u.GetValue(BindingFlags.Instance)
                }).ToList();
            _ = _sysCacheService.Set(key, constlist);
        }
        return await Task.FromResult(constlist);
    }

    /// <summary>
    /// 获取常量特性类型列表
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Type> GetConstAttributeList()
    {
        var asmblieTypes = App.Assemblies.SelectMany(u => u.GetTypes());

        foreach (var item in asmblieTypes)
        {
            if (item.GetCustomAttribute<ConstAttribute>() != null)
            {
                yield return item;
            }
        }
    }
}