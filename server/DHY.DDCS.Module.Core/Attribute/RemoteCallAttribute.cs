namespace DHY.DDCS.Module.Core;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RemoteCallAttribute:Attribute
{
    public string Description { get; init; }

    public RemoteCallAttribute(string description)
    {
        Description = description;
    }
}
