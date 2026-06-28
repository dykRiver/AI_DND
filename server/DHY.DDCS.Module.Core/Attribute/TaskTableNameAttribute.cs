[AttributeUsage(AttributeTargets.Field)]
internal class TaskTableNameAttribute : Attribute
{
    internal string TaskTableName { get; set; }
    internal TaskTableNameAttribute(string taskTableName)
    {
        TaskTableName = taskTableName;
    }
}