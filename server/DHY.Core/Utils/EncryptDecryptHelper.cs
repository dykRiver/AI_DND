using DHY.Core;
using Furion;
using System.Security.Cryptography;
using System.Text;

public class EncryptDecryptHelper
{
    /// <summary>
    /// 默认 加解密的秘钥
    /// </summary>
    protected static string defaultKey = App.GetConfig<string>("Cryptogram:ParameKey");
    /// <summary>
    /// 
    /// </summary>
    static EncryptDecryptHelper()
    {
        if (defaultKey.IsNullOrEmpty())
        {
            defaultKey = "1==2==3==a==b==c,";
        }
    }

    #region 方法: AES加密 2.0 返回16 进制加密

    /// <summary>
    /// aes  2.0 返回16 进制加密
    /// </summary>
    /// <param name="toEncrypt">原文:要加密的内容,不能为空</param>
    /// <param name="key">秘钥, 如果不传,默认取 web.config中appsetting的 Parame_KEY </param>
    /// <returns></returns>
    public static string AESEncrypt2(string toEncrypt, string key = "")
    {
        StringBuilder sb = new StringBuilder();

        if (toEncrypt.IsNullOrEmpty())
        {
            if (key.IsNullOrEmpty())
            {
                key = defaultKey;
            }

            if (key.Length > 32)
            {
                key = key.Substring(0, 32);
            }

            key = key.PadRight(32, '@');
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);


            foreach (byte b in resultArray)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            // return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        return sb.ToString();
    }
    #endregion

    #region 方法: AES解密 2.0  由16进制解密
    /// <summary>
    /// aes解密 2.0  由16进制解密
    /// </summary>
    /// <param name="toDecrypt">密文:要解密的内容</param>
    /// <param name="key">秘钥, 如果不传,默认取 web.config中appsetting的 Parame_KEY </param>
    /// <returns></returns>
    public static string AESDecrypt2(string toDecrypt, string key = "")
    {
        try
        {
            if (!toDecrypt.IsNullOrEmpty())
            {
                //秘钥, 如果没传传,默认取 web.config中appsetting的 Parame_KEY 
                if (key.IsNullOrEmpty())
                {
                    key = defaultKey;
                }

                if (key.Length > 32)
                {
                    key = key.Substring(0, 32);
                }
                key = key.PadRight(32, '@');
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
                //  byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                int halfInputLength = toDecrypt.Length / 2;
                byte[] toEncryptArray = new byte[halfInputLength];
                for (int x = 0; x < halfInputLength; x++)
                {
                    int i = (Convert.ToInt32(toDecrypt.Substring(x * 2, 2), 16));
                    toEncryptArray[x] = (byte)i;
                }

                RijndaelManaged rDel = new();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                toDecrypt = UTF8Encoding.UTF8.GetString(resultArray);
            }
        }
        catch (Exception ex)
        {
            toDecrypt = ex.Message;
        }
        return toDecrypt;
    }
    #endregion
}