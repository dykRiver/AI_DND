using Furion.JsonSerialization;
using System.Reflection;

namespace DHY.Core;

public static class CommonlyExtension
{
    static readonly char[] HexdumpTable = new char[256 * 4];
    static CommonlyExtension()
    {
        char[] digits = "0123456789ABCDEF".ToCharArray();
        for (int i = 0; i < 256; i++)
        {
            HexdumpTable[i << 1] = digits[(int)((uint)i >> 4 & 0x0F)];
            HexdumpTable[(i << 1) + 1] = digits[i & 0x0F];
        }
    }

    /// <summary>
    /// Json字符串反序列化成对象
    /// </summary>
    /// <param name="json"></param>
    /// <param name="returnType"></param>
    /// <returns></returns>
    public static object ToObject(this string json, Type returnType)
    {
        return JSON.GetJsonSerializer().Deserialize(json, returnType);
    }

    /// <summary>
    /// 将object转换成ushort
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static ushort ParseToUShort(this object obj)
    {
        _ = ushort.TryParse(obj?.ToString(), out var result);

        return result;
    }

    /// <summary>
    /// 将object转换成byte
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static byte ParseToByte(this object obj)
    {
        _ = byte.TryParse(obj?.ToString(), out var result);

        return result;
    }

    /// <summary>
    /// 将object转换成uint
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static uint ParseToUInt(this object obj)
    {
        _ = uint.TryParse(obj?.ToString(), out var result);

        return result;
    }

    /// <summary>
    /// 将object转换成int
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static int ParseToInt(this object obj)
    {
        if (obj is string str)
        {
            var decimalPoint = str.IndexOf('.');
            if (decimalPoint > 1)
            {
                str = str.Substring(0, decimalPoint);
            }

            _ = int.TryParse(str, out var result);
            return result;
        }

        try
        {
            return Convert.ToInt32(obj);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 判断数组是否为空 只要有一位数不为0就不是空数组
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static bool IsEmpty(this byte[] buffer) => !buffer.Any(x => x != 0);
    public static bool IsNullOrEmpty(this byte[] buffer) => !buffer?.Any(x => x != 0) == false;
    public static int IndexOf(this ArraySegment<byte> buffer, byte firstValue)
    {
        return buffer.ToArray().IndexOf(firstValue);
    }

    public static int IndexOf(this byte[] buffer, byte firstValue)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == firstValue)
            {
                return i;
            }
        }

        return -1;
    }

    public static int LastIndexOf(this ArraySegment<byte> buffer, byte lastValue)
    {
        return buffer.ToArray().LastIndexOf(lastValue);
    }

    public static int LastIndexOf(this byte[] buffer, byte lastValue)
    {
        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (buffer[i] == lastValue)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 16进制字符串转16进制数组
    /// </summary>
    /// <param name="hexString"></param>
    /// <returns></returns>
    public static byte[] ToHexBytes(this string hexString)
    {
        hexString = hexString.Replace(" ", "").Replace(",", "");
        byte[] buf = new byte[hexString.Length / 2];
        ReadOnlySpan<char> readOnlySpan = hexString.AsSpan();

        for (int i = 0; i < hexString.Length; i++)
        {
            if (i % 2 == 0)
            {
                buf[i / 2] = Convert.ToByte(readOnlySpan.Slice(i, 2).ToString(), 16);
            }
        }

        return buf;
    }

    /// <summary>
    /// 16进制数组转16进制字符串
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string ToHexString(this byte[] source)
    {
        int endIndex = 0 + source.Length;
        var buf = new char[source.Length << 1];
        int srcIdx = 0;
        int dstIdx = 0;
        var hexBuffer = new List<char[]>(16);

        for (; srcIdx < endIndex; srcIdx++, dstIdx += 2)
        {
            var sorceIndex = (source[srcIdx] & 0xFF) << 1;
            hexBuffer.Add([HexdumpTable[sorceIndex], HexdumpTable[sorceIndex + 1]]);
        }

        return string.Join(",", hexBuffer.Select(b => new string(b)));
    }

    /// <summary>
    /// 16进制数组转16进制字符串
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static string ToHexString(this ArraySegment<byte> source)
    {
        if (source == null || source.Array == null || source.Count == 0)
        {
            return string.Empty;
        }

        return source.ToArray().ToHexString();
    }

    public static bool HasAttribute<TAttribute>(this object obj) where TAttribute : Attribute
    {
        return obj.GetType().GetCustomAttribute<TAttribute>() != null;
    }
}
