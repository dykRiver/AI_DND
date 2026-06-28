namespace DHY.FrameWork.Core.Service.NoCode.Dto
{
    /// <summary>
    /// 添加功能模块DTO
    /// </summary>
    public class AddTemplateInput
    {
        [Required(ErrorMessage = "功能名称不能为空")]
        public string Name { get; set; }
        [Required(ErrorMessage = "功能代码不能为空")]

        public string Code { get; set; }

        public long ParentMenuId { get; set; }
        /// <summary>
        /// ICON
        /// </summary>
        public string Icon { get; set; }
        public string Description { get; set; }

        public long DataSroucesId { get; set; }

        public string TemplateConfig { get; set; } = "{}";




    }
}
