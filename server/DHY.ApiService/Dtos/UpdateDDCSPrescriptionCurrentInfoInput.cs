namespace DHY.InternalApiService.Dtos
{
    public class UpdateDDCSPrescriptionCurrentInfoInput
    {
        /// <summary>
        /// ddcsid
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 先煎桶号
        /// </summary>
        public int? DecoctFirstContainerNo { get; set; }
        /// <summary>
        /// 后下桶号
        /// </summary>
        public int? DecoctLaterContainerNo { get; set; }
        /// <summary>
        /// 储液桶号
        /// </summary>
        public int? StorageContainerNo { get; set; }

        /// <summary>
        /// 储液桶所在煎药机号
        /// </summary>
        public int? StorageDecoctorNo { get; set; }
    }
}
