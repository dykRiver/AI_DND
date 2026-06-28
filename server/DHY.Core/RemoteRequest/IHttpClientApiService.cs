using System.Net;
using System.Text;
using System.Text.Json;
using DHY.Core.Consts;
using Furion.EventBus;
using Furion.Logging.Extensions;
using Furion.RemoteRequest;

namespace DHY.Core.RemoteRequest
{
    /// <summary>
    /// 表示客户端HTTP请求代理接口
    /// </summary>
    [Client("default")]
    public interface IHttpClientApiService : IHttpDispatchProxy
    {
        public static string AccessToken = string.Empty;
        public static string RefreshToken = string.Empty;
        private const string RefreshTokenKey = "x-access-token";
        private const string AccessTokenKey = "access-token";
        private const string ClientId = "dhy-api-internal-limit-strategy";

        // 全局拦截，类中每一个方法都会触发
        [Interceptor(InterceptorTypes.Request)]
        static void OnRequest(HttpClient client, HttpRequestMessage req)
        {
            client.DefaultRequestHeaders.Add("X-ClientId", ClientId);

            //添加身份验证信息
            if (AccessToken.IsNullOrEmpty() == false)
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken}");
            }

            if (RefreshToken.IsNullOrEmpty() == false)
            {
                client.DefaultRequestHeaders.Add("X-Authorization", RefreshToken);
            }

            var logContent = new StringBuilder();
            logContent.AppendLine($"请求地址：{client.BaseAddress}{req.RequestUri}");
            logContent.AppendLine($"请求参数：{req.Content?.ReadAsStringAsync().Result}");
           //logContent.ToString().LogTrace();
        }

        // 全局拦截，类中每一个方法都会触发
        [Interceptor(InterceptorTypes.Response)]
        static void OnResponse(HttpClient client, HttpResponseMessage res)
        {
            if (res.Headers.Contains(RefreshTokenKey))
            {
                var refreshTokenKey = res.Headers.GetValues(RefreshTokenKey).First();
                RefreshToken = refreshTokenKey.IsNullOrEmpty() ? RefreshToken : refreshTokenKey;
            }

            if (res.Headers.Contains(AccessTokenKey))
            {
                var accessTokenKey = res.Headers.GetValues(AccessTokenKey).First();
                AccessToken = accessTokenKey.IsNullOrEmpty() ? AccessToken : accessTokenKey;
            }

            var responseContent = res.Content?.ReadAsStringAsync().Result;

            var logContent = new StringBuilder();
            logContent.AppendLine($"请求地址：{res.RequestMessage.RequestUri}");
            logContent.AppendLine($"请求响应：{responseContent}");
            //logContent.ToString().LogTrace();

            if (!string.IsNullOrEmpty(responseContent))
            {
                var returnResult =  JsonDocument.Parse(responseContent);
                //var returnType = returnResult.RootElement.GetProperty("type").GetString();
                var returnCode = returnResult.RootElement.GetProperty("code").GetInt32();

                if (returnCode == (int)HttpStatusCode.Unauthorized)
                {
                    MessageCenter.PublishAsync(ComponentConst.HttpApiUnauthorizedEventName).Wait();
                    AccessToken = string.Empty;
                    RefreshToken = string.Empty;
                    //这里抛出异常让应用层重试
                    throw new UnauthorizedAccessException("http 登录过期或未登录");
                }
                else if (returnCode != (int)HttpStatusCode.OK)
                {
                    logContent.ToString().LogError();
                    //throw new HttpRequestException(responseContent);
                }
            }
            else
            {
                logContent.ToString().LogError();
                //throw new HttpRequestException(logContent.ToString());
            }
        }

        // 全局拦截，类中每一个方法都会触发
        [Interceptor(InterceptorTypes.Exception)]
        static void OnError(HttpClient client, HttpResponseMessage res, string errors)
        {
            var logContent = new StringBuilder();
            logContent.AppendLine(res.RequestMessage.RequestUri.ToString());
            logContent.AppendLine($"错误信息：{(string.IsNullOrWhiteSpace(errors) ? "无响应内容" : errors)}");
            logContent.AppendLine($"请求响应：{res.Content?.ReadAsStringAsync().Result}");
            logContent.ToString().LogError();
            //throw new HttpRequestException(logContent.ToString());
        }
    }
}
