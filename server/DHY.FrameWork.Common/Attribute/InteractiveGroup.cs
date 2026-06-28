namespace DHY.FrameWork.Common;

/// <summary>
/// 通信交互点
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InteractiveGroupAttribute : Attribute
{
    public InteractiveGroupEnum ProcessGroupEnum { get; init; }

    public InteractiveGroupAttribute(InteractiveGroupEnum processGroupEnum = InteractiveGroupEnum.Station)
    {
        ProcessGroupEnum = processGroupEnum;
    }
}