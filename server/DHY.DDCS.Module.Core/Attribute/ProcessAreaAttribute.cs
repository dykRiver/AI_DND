namespace DHY.DDCS.Module.Core;

public class ProcessAreaAttribute: Attribute
{
    public ProcessAreaEnum ProcessArea { get; init; }

    public ProcessAreaAttribute(ProcessAreaEnum processArea)
    {
        ProcessArea = processArea;
    }
}
