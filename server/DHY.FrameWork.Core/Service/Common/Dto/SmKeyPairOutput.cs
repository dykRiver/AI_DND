namespace DHY.Core.Service;

/// <summary>
/// 国密公钥私钥对输出
/// </summary>
public class SmKeyPairOutput
{
    /// <summary>
    /// 私匙
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    /// 公匙
    /// </summary>
    public string PublicKey { get; set; }
}