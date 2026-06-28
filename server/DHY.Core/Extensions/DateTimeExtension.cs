/*
 * @Author weihuiming
 * @Date 2024年4月2日15:02:39
 * @Description 时间扩展类
 * 
 */
using Furion.DependencyInjection;

[SuppressSniffer]
public static class DateTimeExtension
{
    /// <summary>
    /// 格式化起始时间
    /// </summary>
    /// <param name="date">时间</param>
    /// <param name="format">默认输出格式 2024-4-2 00:00:00</param>
    /// <returns></returns>
    public static string FormatBeginDateTimeString(this DateTime date, string format = "yyyy-MM-dd HH:mm:ss")
    {
        //return DateOnly.FromDateTime(date).ToLongDateString();
        return date.Date.ToString(format);
    }

    /// <summary>
    /// 格式化结束时间
    /// </summary>
    /// <param name="date">时间</param>
    /// <param name="format">默认输出格式 2024-4-2 23:59:59</param>
    /// <returns></returns>
    public static string FormatEndDateTimeString(this DateTime date, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return date.Date.AddDays(1).AddMilliseconds(-1).ToString(format);
    }

    /// <summary>
    /// 取开始时间
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime ToBeginDate(this DateTime date)
    {
        return date.Date;
    }

    /// <summary>
    /// 取结束时间
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime ToEndDate(this DateTime date)
    {
        return date.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// 清除分秒
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTime ClearTime(this DateTime dateTime)
    {
        return Convert.ToDateTime(dateTime.ToString("yyyy-MM-dd HH:00:00"));
    }

    /// <summary>
    /// 清除时分秒
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTime ClearHourTime(this DateTime dateTime)
    {
        return Convert.ToDateTime(dateTime.ToString("yyyy-MM-dd 00:00:00"));
    }

    /// <summary>
    /// 可空的Datetime类型增加指定小时数
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="hours"></param>
    /// <returns></returns>
    public static DateTime? AddHours(this DateTime? dateTime, double hours)
    {
        return dateTime.HasValue ? dateTime.Value.AddHours(hours) : null;
    }
}