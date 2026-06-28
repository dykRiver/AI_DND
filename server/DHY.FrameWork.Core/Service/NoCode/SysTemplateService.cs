using DHY.FrameWork.Core.Service.NoCode.Dto;

namespace DHY.FrameWork.Core.Service.NoCode
{
    /// <summary>
    /// 前端界面配置功能服务
    /// </summary>
    [ApiDescriptionSettings(Order = 490, Groups = new[] { "OnlineDev", CommonConst.SysGroupName })]
    public class PageTemplateService : IDynamicApiController, ITransient
    {
        private readonly SqlSugarRepository<PageTemplate> _pageTemplate;
        private readonly SysMenuService _sysMenuService;
        public PageTemplateService(SqlSugarRepository<PageTemplate> pageTemplate, SysMenuService sysMenuService)
        {
            _pageTemplate = pageTemplate;
            _sysMenuService = sysMenuService;
        }

        /// <summary>
        /// 获取功能分页列表
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [DisplayName("获取功能分页列表")]
        public async Task<SqlSugarPagedList<PageTemplateOutput>> Page(PageTemplateInput input)
        {
            var newTemplate = await _pageTemplate.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(input.Code), u => u.Code.Contains(input.Code))
                .LeftJoin<SysMenu>((p, m) => p.MenuId == m.Id)
                .Select((p, m) => new PageTemplateOutput
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    CreateTime = p.CreateTime,
                    CreateUserName = p.CreateUserName,
                    CreateUserId = p.CreateUserId,
                    DataSroucesId2 = p.DataSroucesId2,
                    Description = p.Description,
                    Enabled = p.Enabled,
                    Icon = m.Icon,
                    IsDelete = p.IsDelete,
                    MenuId = p.MenuId,
                    OrderNo = p.OrderNo,
                    TemplateConfig = p.TemplateConfig,
                    UpdateTime = p.UpdateTime,
                    UpdateUserId = p.UpdateUserId,
                    UpdateUserName = p.UpdateUserName,
                    ParentMenuId = m.Pid,
                }).MergeTable()
                .OrderBy(u => u.CreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);

            return newTemplate;
        }

        /// <summary>
        /// 增加功能
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [UnitOfWork]
        [ApiDescriptionSettings(Name = "Add"), HttpPost]
        [DisplayName("增加功能")]
        public async Task AddTemplate(AddTemplateInput input)
        {
            var isExist = await _pageTemplate.AsQueryable().Filter(null, true).AnyAsync(u => u.Code == input.Code);
            if (isExist) throw Oops.Oh("功能重复");

            var template = input.Adapt<PageTemplate>();


            //同步添加菜单项
            var menuItem = new AddMenuInput()
            {
                Title = template.Name,
                Component = "/system/dynamicPage/index",
                Path = $"/onlineDev/{template.Code}",
                Type = MenuTypeEnum.Menu,
                Pid = input.ParentMenuId,
                Name = template.Code,
                Icon = input.Icon,
                Remark = $"在线功能开发-{template.Name}创建菜单项"
            };
            var funMenuItem = await _sysMenuService.AddMenu(menuItem);

            template.MenuId = funMenuItem.Id;

            var newTemplate = await _pageTemplate.AsInsertable(template).ExecuteReturnEntityAsync();
        }

        /// <summary>
        /// 删除功能
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [UnitOfWork]
        [ApiDescriptionSettings(Name = "Delete"), HttpPost]
        [DisplayName("删除功能")]
        public async Task DeleteTemplate(BaseIdInput input)
        {
            var template = await _pageTemplate.GetFirstAsync(u => u.Id == input.Id);
            if (template == null)
                throw Oops.Oh("功能不能为空");

            if (template.MenuId > 0)
            {
                await _sysMenuService.DeleteMenu(new DeleteMenuInput() { Id = template.MenuId });
            }

            await _pageTemplate.DeleteAsync(template);


        }

        /// <summary>
        /// 更新功能基本信息
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiDescriptionSettings(Name = "update"), HttpPost]
        [DisplayName("更新功能基本信息")]
        [UnitOfWork]
        public async Task<int> UpdateTemplate(UpdateTemplateInput input)
        {
            var tp = input.Adapt<PageTemplate>();
            var existItem = _pageTemplate.AsQueryable().First(s => s.Id == tp.Id);

            if (existItem == null)
            {
                throw Oops.Oh(ErrorCodeEnum.D1002);
            }

            //禁用时删除相应的菜单
            if (!input.Enabled)
            {
                await _sysMenuService.DeleteMenu(new DeleteMenuInput { Id = input.MenuId });
            }
            //没有禁用有可能是重新启用的，检查菜单是否存在，不存在重新添加
            else
            {
                var itemMenu = await _sysMenuService.GetById(new DeleteMenuInput() { Id = input.MenuId });

                if (itemMenu == null)
                {
                    //同步添加菜单项
                    var menuItem = new AddMenuInput()
                    {
                        Title = tp.Name,
                        Component = "/system/dynamicPage/index",
                        Path = $"/onlineDev/{input.Code}",
                        Type = MenuTypeEnum.Menu,
                        Pid = input.ParentMenuId,
                        Name = tp.Code,
                        Icon = input.Icon,
                        Remark = $"在线功能开发-{input.Name}创建菜单项"
                    };

                    var funMenuItem = await _sysMenuService.AddMenu(menuItem);

                    tp.MenuId = funMenuItem.Id;
                }
                else
                {
                    itemMenu.Title = tp.Name;
                    itemMenu.Path = $"/onlineDev/{tp.Code}";
                    itemMenu.Pid = input.ParentMenuId;
                    itemMenu.Name = tp.Code;
                    itemMenu.Icon = input.Icon;

                    await _sysMenuService.UpdateMenu(itemMenu.Adapt<UpdateMenuInput>());
                }

            }

            string targetPath = GetTemplateConfigTargetPath(tp.Name);
            File.WriteAllText(targetPath, tp.TemplateConfig, Encoding.UTF8);

            return await _pageTemplate.AsUpdateable(tp)
                .IgnoreColumns(true).ExecuteCommandAsync();
        }

        /// <summary>
        /// 设置生成json文件路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetTemplateConfigTargetPath(string name)
        {
            var backendPath = Path.Combine(new DirectoryInfo(App.WebHostEnvironment.ContentRootPath).Parent.FullName, "DHY.FrameWork.Core", "TemplateConfig");
            if (!Directory.Exists(backendPath))
                Directory.CreateDirectory(backendPath);
            return Path.Combine(backendPath, name + ".json");
        }

        /// <summary>
        /// 更新功能配置
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiDescriptionSettings(Name = "updatetemplate"), HttpPost]
        [DisplayName("更新功能配置")]
        public async Task<int> UpdateTemplateConfig(UpdateTemplateInput input)
        {
            var tp = input.Adapt<PageTemplate>();
            return await _pageTemplate.AsUpdateable(tp)
                .UpdateColumns(u => new { u.TemplateConfig }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据Id号获取功能
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiDescriptionSettings(Name = "getById"), HttpPost]
        [DisplayName("根据Id获取功能")]
        [HttpPost]
        public async Task<PageTemplate> GetTemplateById([FromBody] GetTemplateByIdInPut input)
        {
            var template = await _pageTemplate.GetFirstAsync(u => u.Id == input.Id);
            if (template == null)
                throw Oops.Oh("功能不能为空");
            return template;

        }

        /// <summary>
        /// 根据Code获取功能
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ApiDescriptionSettings(Name = "getByCode"), HttpPost]
        [DisplayName("根据Code获取功能")]
        [HttpPost]
        public async Task<PageTemplate> GetTemplateByCode([FromBody] GetTemplateByCodeInPut input)
        {
            var template = await _pageTemplate.GetFirstAsync(u => u.Code == input.Code);
            if (template == null)
                throw Oops.Oh("功能不能为空");
            return template;

        }

    }
}
