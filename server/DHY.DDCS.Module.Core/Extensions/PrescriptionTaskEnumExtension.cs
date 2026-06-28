public static class PrescriptionTaskEnumExtension
{
    public static string GetTaskName(this PrescriptionStatusEnum prescriptionStatusEnum)
    {
        var descriptAttr = prescriptionStatusEnum.GetDescription();
        return string.IsNullOrWhiteSpace(descriptAttr) ? "未知" : descriptAttr;
    }

    public static string GetTaskTableName(this PrescriptionStatusEnum prescriptionStatusEnum)
    {
        var taskTableAttr = prescriptionStatusEnum.GetAttributeOfTypeEx<TaskTableNameAttribute>();

        if (taskTableAttr == null)
        {
            return string.Empty;
        }

        return taskTableAttr.TaskTableName;
    }

    public static string ToTaskTableName(this PrescriptionTaskTypeEnum taskTypeEnum)
    {
        var pEnum = taskTypeEnum switch
        {
            PrescriptionTaskTypeEnum.Dispensing => PrescriptionStatusEnum.Dispensing,
            PrescriptionTaskTypeEnum.Replenish => PrescriptionStatusEnum.Replenish,
            PrescriptionTaskTypeEnum.Recheck => PrescriptionStatusEnum.Recheck,
            PrescriptionTaskTypeEnum.FillWater => PrescriptionStatusEnum.FillWater,
            PrescriptionTaskTypeEnum.Decoction => PrescriptionStatusEnum.Decoction,
            PrescriptionTaskTypeEnum.Packing => PrescriptionStatusEnum.Packing,
            _ => PrescriptionStatusEnum.Packing
        };

        return pEnum.GetTaskTableName();
    }

    public static PrescriptionTaskTypeEnum ToTaskTypeEnum(this PrescriptionStatusEnum prescriptionStatusEnum) => prescriptionStatusEnum switch
    {
        PrescriptionStatusEnum.SentContainer => PrescriptionTaskTypeEnum.Dispensing,
        PrescriptionStatusEnum.BindContainer => PrescriptionTaskTypeEnum.Dispensing,
        PrescriptionStatusEnum.Dispensing => PrescriptionTaskTypeEnum.Dispensing,
        PrescriptionStatusEnum.Replenish => PrescriptionTaskTypeEnum.Replenish,
        PrescriptionStatusEnum.Recheck => PrescriptionTaskTypeEnum.Recheck,
        PrescriptionStatusEnum.FillWater => PrescriptionTaskTypeEnum.FillWater,
        PrescriptionStatusEnum.Soak => PrescriptionTaskTypeEnum.Decoction,
        PrescriptionStatusEnum.Decoction => PrescriptionTaskTypeEnum.Decoction,
        PrescriptionStatusEnum.Packing => PrescriptionTaskTypeEnum.Packing,
        _ => PrescriptionTaskTypeEnum.SelfCheck,
    };
}