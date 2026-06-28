using Elasticsearch.Net;
using Nest;

namespace DHY.Core;

/// <summary>
/// ES服务注册
/// </summary>
public static class ElasticSearchSetup
{
    public static void AddElasticSearch(this IServiceCollection services)
    {
        var option = App.GetConfig<ElasticSearchOptions>("Logging:ElasticSearch");
        if (!option.Enabled) return;

        var uris = option.ServerUris.Select(u => new Uri(u));
        // 集群
        var connectionPool = new SniffingConnectionPool(uris);
        var connectionSettings = new ConnectionSettings(connectionPool).DefaultIndex(option.DefaultIndex);
        // 单连接
        //var connectionSettings = new ConnectionSettings(new SingleNodeConnectionPool(uris.FirstOrDefault())).DefaultIndex(option.DefaultIndex);

        // 认证类型
        if (option.AuthType == ElasticSearchAuthTypeEnum.Basic)// Basic 认证
        {
            connectionSettings.BasicAuthentication(option.User, option.Password);
        }
        else if (option.AuthType == ElasticSearchAuthTypeEnum.ApiKey) //ApiKey 认证
        {
            connectionSettings.ApiKeyAuthentication(option.ApiId, option.ApiKey);
        }
        else if (option.AuthType == ElasticSearchAuthTypeEnum.Base64ApiKey)// Base64ApiKey 认证
        {
            connectionSettings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(option.Base64ApiKey));
        }
        else return;

        // ES使用Https时的证书指纹
        if (!string.IsNullOrEmpty(option.Fingerprint))
        {
            connectionSettings.CertificateFingerprint(option.Fingerprint);
        }

        var client = new ElasticClient(connectionSettings);
        client.Indices.Create(option.DefaultIndex, u => u.Map<SysLogOp>(m => m.AutoMap()));

        services.AddSingleton(client); // 单例注册
    }
}