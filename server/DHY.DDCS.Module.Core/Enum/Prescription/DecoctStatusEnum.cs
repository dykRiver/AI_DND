/// <summary>
/// 煎药类型枚举(10 煎药开始 20 一煎开始 30 一煎结束 40 二煎开始 50 二煎结束 60 煎药完成 70 出药开始 80 出药结束 610 先煎  611 先煎结束 620 另煎  621 另煎结束 630 后下 631 后下结束)
/// </summary>
public enum DecoctStatusEnum
{
    /// <summary>
    /// 煎药开始
    /// </summary>
    DecoctionBegins = 10,
    /// <summary>
    /// 一煎开始
    /// </summary>
    StartFrying = 20,
    /// <summary>
    /// 一煎结束
    /// </summary>
    EndOfFrying = 30,
    /// <summary>
    /// 二煎开始
    /// </summary>
    SecondFryingBegins = 40,
    /// <summary>
    /// 二煎结束
    /// </summary>
    SecondFryingOver = 50,
    /// <summary>
    /// 煎药完成
    /// </summary>
    DecoctionCompleted = 60,
    /// <summary>
    /// 出药开始
    /// </summary>
    DrugDispensingBegins = 70,
    /// <summary>
    /// 出药结束
    /// </summary>
    EndMedicationDispensing = 80,
    /// <summary>
    /// 先煎
    /// </summary>
    FryFirst = 610,
    /// <summary>
    /// 先煎结束
    /// </summary>
    FryFirstFinish = 611,
    /// <summary>
    /// 另煎
    /// </summary>
    FrySeparately = 620,
    /// <summary>
    /// 另煎结束
    /// </summary>
    FinishFryingAgain = 621,
    /// <summary>
    /// 后下
    /// </summary>
    BackDown = 630,
    /// <summary>
    /// 后下结束
    /// </summary>
    EndLater = 631
}