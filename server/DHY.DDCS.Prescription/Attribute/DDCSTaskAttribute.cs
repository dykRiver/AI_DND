[AttributeUsage(AttributeTargets.Field)]
internal class DDCSTaskAttribute : Attribute
{
    public Type DDCSTaskType { get; private set; }
    public bool SkipQueryStep { get; set; }
    public DDCSTaskAttribute(Type dDCSTaskType)
    {
        DDCSTaskType = dDCSTaskType;
    }
}