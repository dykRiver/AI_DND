namespace DHY.DDCS.Module.Prescription.Dtos;

public class PrescriptionCompletionStatusInfoOutput
{
    /// <summary>
    /// 当前状态已完成数量
    /// </summary>
    public int CompletedNum { get; set; }
    /// <summary>
    /// 当前状态未完成数量
    /// </summary>
    public int UnfinishedNum { get; set; }
    /// <summary>
    /// 当前状态完成率
    /// </summary>
    public decimal CompletedRate => CalculateCompletionRate(CompletedNum, UnfinishedNum);

    public decimal CalculateCompletionRate(int completedNum, int unfinishedNum)
    {
        if (completedNum == 0)
        {
            return 0;
        }
        else
        {
            return (decimal)completedNum / (completedNum + unfinishedNum);
        }
    }
}
