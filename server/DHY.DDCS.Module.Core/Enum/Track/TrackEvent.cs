public enum TrackEvent : long
{
    #region 原始处方
    Prescription = 1010000000,
    PrescriptionPush = 1010100000,
    PrescriptionPushNew = 1010101000,
    PrescriptionRePush = 1010102000,
    PrescriptionPushDenied = 1010103000,
    #endregion

    #region 拆方中间状态
    PrescriptionSeparate = 1010200000,
    PrescriptionSeparateResult = 1010201000,
    PrescriptionSeparateConfig = 1010202000,
    #endregion

    #region 拆方
    DDCSPrescriptionSave = 1010300000,
    DDCSPrescriptionCreateTaskNo = 1010301000,
    DDCSPrescriptionCreateTask = 1010302000,
    #endregion
    ContainerBind = 1020000000,

    Dispensing = 1030000000,
    DispensingPush = 1030100000,
    DispensingWorkFlow = 1030200000,

    Decoction = 1040000000,
    DecoctionWorkFlow = 1040100000,
    DecoctionWorkFlowCoverStep = 1040200000,
    DecoctionWorkFlowCleanStep = 1040300000,
    DecoctionPush = 1040400000,
    DecoctionTemperatureSave = 1040500000,
    DecoctorCoverCompleted = 1040600000,
    DecoctorDecoctCompleted = 1040700000,
    DecoctorCleanCompleted = 1040800000,
    DecoctionSoak = 1040900000,
    DecoctionFillWater = 1041000000,

    Packing = 1050000000,
    PackingWorkFlow = 1050100000,
    PackingWorkFlowCoverStep = 1050200000,
    PackingWorkFlowLabelStep = 1050300000,
    PackingWorkFlowCanPackingStep = 1050400000,
    PackingWorkFlowCleanStep = 1050500000,
    PackerCleanCompleted = 1050600000,
    PackerCoverCompleted = 1050700000,
    PackerPackStarted = 1050800000,
    PackerPackCompleted = 1050900000,

    Task = 1060000000,
    Device = 1070000000,
    WorkFlow = 1080000000,
    ExchangeChannel = 1090000000,

    RgvAction = 1100000000,
    PublishToVehicleCreate = 1110000000,
    Vehicle = 1120000000,
    VehicleTask = 1120100000,

    Common = 1130000000,
    CommonService = 1140000000,

    OpcUaRcvHandle = 1150000000,
    PlcHandle = 1160000000,

    ManagementSystemPush = 1170000000,
}