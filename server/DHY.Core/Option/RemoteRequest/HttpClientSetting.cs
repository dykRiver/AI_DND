namespace DHY.Core.Option.RemoteRequest
{
    /// <summary>
    /// HTTP客户端配置
    /// </summary>
    public class HttpClientSetting
    {
        /// <summary>
        /// 客户端名称
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// 请求根地址
        /// </summary>
        public string BaseUrl { get; set; }
        /// <summary>
        /// 验证方式，如果为空则不验证
        /// </summary>
        public HttpAuth Auth { get; set; }


        /*

{
"ClientName": "default",
"BaseUrl": "http://192.168.80.30:20243/api/", //一定要以/结尾
"Auth": {
"Type": "jwt", //身份验证类型，目前只支持JWT身份验证，为空表示可匿名访问
"LoginUrl": "sysAuth/login",
"PostData": "{\"account\":\"superadmin\",\"password\":\"4dc05e2e631865cf75ee7c881cb2f824a7c9be4af64c929d83c253bf000a922bcc0ee4bcaae6301f4b6798beeb7d33d3b64905e0bc60fccb13763971b6116bbf58dc93da3e32900f6abd04eb46609fe73d5a2f558fec9dbc33946b9ec24891666299b40a43bb\",\"code\":\"\",\"codeId\":0}\"",

}
}
*/
    }
}
