namespace DHY.FrameWork.Core.Service.NoCode.Dto
{
    public class UpdateTemplateInput : AddTemplate
    {
        public long ParentMenuId { get; set; }
        /// <summary>
        /// ICON
        /// </summary>
        public string Icon { get; set; }
    }
}
