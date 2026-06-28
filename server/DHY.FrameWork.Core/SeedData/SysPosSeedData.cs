namespace DHY.Core;

/// <summary>
/// 系统职位表种子数据
/// </summary>
public class SysPosSeedData : ISqlSugarEntitySeedData<SysPos>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysPos> HasData()
    {
        return new[]
        {
            new SysPos{ Id=1300000000101, Name="党委书记", Code="dwsj", CreateTime=DateTime.Now, Remark="党委书记", TenantId=1300000000001 },
            new SysPos{ Id=1300000000102, Name="董事长", Code="dsz", CreateTime=DateTime.Now, Remark="董事长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000103, Name="副董事长", Code="fdsz", CreateTime=DateTime.Now, Remark="副董事长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000104, Name="总经理", Code="zjl", CreateTime=DateTime.Now, Remark="总经理", TenantId=1300000000001 },
            new SysPos{ Id=1300000000105, Name="副总经理", Code="fzjl", CreateTime=DateTime.Now, Remark="副总经理", TenantId=1300000000001 },
            new SysPos{ Id=1300000000106, Name="部门经理", Code="bmjl", CreateTime=DateTime.Now, Remark="部门经理", TenantId=1300000000001 },
            new SysPos{ Id=1300000000107, Name="部门副经理", Code="bmfjl", CreateTime=DateTime.Now, Remark="部门副经理", TenantId=1300000000001 },
            new SysPos{ Id=1300000000108, Name="主任", Code="zr", CreateTime=DateTime.Now, Remark="主任", TenantId=1300000000001 },
            new SysPos{ Id=1300000000109, Name="副主任", Code="fzr", CreateTime=DateTime.Now, Remark="副主任", TenantId=1300000000001 },
            new SysPos{ Id=1300000000110, Name="局长", Code="jz", CreateTime=DateTime.Now, Remark="局长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000111, Name="副局长", Code="fjz", CreateTime=DateTime.Now, Remark="副局长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000112, Name="科长", Code="kz", CreateTime=DateTime.Now, Remark="科长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000113, Name="副科长", Code="fkz", CreateTime=DateTime.Now, Remark="副科长", TenantId=1300000000001 },
            new SysPos{ Id=1300000000114, Name="财务", Code="cw", CreateTime=DateTime.Now, Remark="财务", TenantId=1300000000001 },
            new SysPos{ Id=1300000000115, Name="职员", Code="zy", CreateTime=DateTime.Now, Remark="职员", TenantId=1300000000001 },
            new SysPos{ Id=1300000000116, Name="其他", Code="qt", CreateTime=DateTime.Now, Remark="其他", TenantId=1300000000001 },
        };
    }
}