namespace DHY.Core
{
    /// <summary>
    /// 原生应用的程序启动入口点
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NativeApplicationEntryAttribute : Attribute
    {
        public bool RunNewThread { get; set; }

        public int Order { get; set; } = 100;
    }
}
