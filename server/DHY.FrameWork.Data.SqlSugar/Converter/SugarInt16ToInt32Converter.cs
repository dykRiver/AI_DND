using System.Data;
using SqlSugar;

public class SugarInt16ToInt32Converter : ISugarDataConverter
{
    public SugarParameter ParameterConverter<T>(object columnValue, int columnIndex)
    {
        var name = $"@Ushort{columnIndex}";

        if (columnValue == null)
        {
            return new SugarParameter(name, null);
        }

        var insertValue = Convert.ChangeType(columnValue, typeof(int));

        return new SugarParameter(name, insertValue);
    }

    public T QueryConverter<T>(IDataRecord dataRecord, int dataRecordIndex)
    {
        int? intValue = dataRecord.GetInt32(dataRecordIndex);

        return (T)ConvertHelper.ChangeType(intValue, typeof(T));
    }
}


public static class ConvertHelper
{
    public static object ChangeType(object obj, Type conversionType)
    {
        return ChangeType(obj, conversionType, Thread.CurrentThread.CurrentCulture);
    }
    public static object ChangeType(object obj, Type conversionType, IFormatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(obj);

        #region Nullable

        Type nullableType = Nullable.GetUnderlyingType(conversionType);

        if (nullableType != null)
        {
            return Convert.ChangeType(obj, nullableType, provider);
        }

        #endregion

        if (typeof(Enum).IsAssignableFrom(conversionType))
        {
            return Enum.Parse(conversionType, obj.ToString());
        }

        return Convert.ChangeType(obj, conversionType, provider);
    }
}
