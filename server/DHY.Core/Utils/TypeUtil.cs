using System.ComponentModel;
using System.Reflection;
using Furion.JsonSerialization;

namespace DHY.Core;
public static class TypeUtil
{
    /// <summary>
    /// 提取类的字段值和描述信息
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<TypeDescriptionInfo> GetTypeDescriptionInfo(Type type)
    {
        if (!type.IsClass)
            throw new ArgumentException("Type '" + type.Name + "' is not an class.");

        FieldInfo[] fields = type.GetFields();

        List<TypeDescriptionInfo> classInfo = new List<TypeDescriptionInfo>();

        foreach (var field in fields)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = null;
            if (attributes.Length > 0)
            {
                description = attributes[0].Description;
            }
            classInfo.Add(new TypeDescriptionInfo { Describe = description ?? field.ToString(), Value = field.Name });
        }

        return classInfo;
    }

    public static TData ObjectToClass<TData>(object data)
    {
        var json = JSON.Serialize(data);
        return JSON.Deserialize<TData>(json);
    }
}

/// <summary>
/// 描述信息对象
/// </summary>
public class TypeDescriptionInfo
{
    /// <summary>
    /// 描述
    /// </summary>
    public string Describe { set; get; }

    /// <summary>
    /// 字段值
    /// </summary>
    public string Value { set; get; }
}
