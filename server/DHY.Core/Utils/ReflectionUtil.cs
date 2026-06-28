using System.Reflection;
using Furion.FriendlyException;

namespace DHY.Core;

/// <summary>
/// 反射工具类
/// </summary>
public static class ReflectionUtil
{
    /// <summary>
    /// 获取字段特性
    /// </summary>
    /// <param name="field"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetDescriptionValue<T>(this FieldInfo field) where T : Attribute
    {
        // 获取字段的指定特性，不包含继承中的特性
        object[] customAttributes = field.GetCustomAttributes(typeof(T), false);

        // 如果没有数据返回null
        return customAttributes.Length > 0 ? (T)customAttributes[0] : null;
    }

    /// <summary>
    /// 通过反射创建实例
    /// </summary>
    /// <param name="typeName">类型名</param>
    /// <param name="assemblyString">程序集</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public static object CreateInstance(string typeName, string assemblyString)
    {
        Assembly myAssembly = Assembly.Load(assemblyString);
        if (myAssembly == null) { throw Oops.Oh("加载程序集{0}失败", assemblyString); }
        Type t = myAssembly.GetType(typeName);
        if (t == null) { throw Oops.Oh("创建实例对象{0}失败", typeName); }

        //创建实例对象
        return t.Assembly.CreateInstance(typeName);

    }
    public static object? GetPropertyValue(Type propertyType, object v)
    {
        object obj = null;
        switch (propertyType.Name)
        {
            case "Boolean":
                obj = Convert.ToBoolean(v);
                break;
            case "Byte":
               obj=Convert.ToByte(v);
                break;
            case "Int16":
                obj = Convert.ToInt16(v);
                break;
            case "UInt16":
                obj = Convert.ToUInt16(v);
                break;
            case "Int32":
                obj = Convert.ToInt32(v);
                break;
            case "UInt32":
                obj= Convert.ToUInt32(v);
                break;
            case "UInt64":
                obj = Convert.ToUInt64(v);
                break;
            case "Int64":
                obj = Convert.ToInt64(v);
                break;
            case "Single":
                obj=Convert.ToSingle(v);
                break;
            case "Double":
                obj=Convert.ToDouble(v);
                break;
            
            default:
                obj = v;
                break;
        }

        return obj;
    }

}