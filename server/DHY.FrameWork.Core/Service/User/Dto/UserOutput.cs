namespace DHY.Core.Service;

public class UserOutput : SysUser
{
    /// <summary>
    /// 机构名称
    /// </summary>
    public string OrgName { get; set; }

    /// <summary>
    /// 职位名称
    /// </summary>
    public string PosName { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; }
}