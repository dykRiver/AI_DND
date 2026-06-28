/// </summary>
[Client("default")]
public interface ILoginApiService : IHttpClientApiService
{
    /// <summary>
    /// 客户端登录
    /// 请求地址：BASEURL/sysAuth/login
    /// </summary>
    /// <param name="loginData"></param>
    /// <returns></returns>
    [Post("sysAuth/login")]
    Task<AdminResult<LoginOutput>> LoginAsync([Body] string loginData);
}