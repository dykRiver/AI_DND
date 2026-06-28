using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Dtos
{
    public class CheckPresDto
    {
        public PrescriptionDto Prescription { get; set; }

    }

    public class PrescriptionDto
    {
        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public SexType? Sex { get; set; }
        /// <summary>
        /// 药品
        /// </summary>
        public List<Drug> Details { get; set; }
    }

    public class Drug
    {
        public string DrugName { get; set; }
    }
}
