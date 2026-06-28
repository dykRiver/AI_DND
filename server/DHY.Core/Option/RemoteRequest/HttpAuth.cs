namespace DHY.Core.Option.RemoteRequest
{
    /// <summary>
    /// HTTP验证
    /// </summary>
    public class HttpAuth
    {
        /// <summary>
        /// 验证类型
        /// jwt-JWT 验证
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 验证地址
        /// </summary>
        public string LoginUrl { get; set; }
        /// <summary>
        /// POST 数据
        /// </summary>
        public string Data { get; set; }

        public string User { get; set; }
        public string Password { get; set; }

        public int UserID { get; set; }
    }
}
