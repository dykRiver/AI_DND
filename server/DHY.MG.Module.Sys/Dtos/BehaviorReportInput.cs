namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// 行为上报请求DTO
    /// </summary>
    public class BehaviorReportInput
    {
        /// <summary>
        /// 用户标识
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 行为列表
        /// </summary>
        public List<BehaviorItem> Behaviors { get; set; } = new List<BehaviorItem>();
    }

    /// <summary>
    /// 单条行为数据
    /// </summary>
    public class BehaviorItem
    {
        /// <summary>
        /// 剧本编号
        /// </summary>
        public int ScriptMid { get; set; }

        /// <summary>
        /// 行为类型（0=Browse, 1=Click, 2=Play, 3=Win, 4=Lose, 5=Score, 6=Skip）
        /// </summary>
        public int BehaviorType { get; set; }

        /// <summary>
        /// 行为量化值（停留秒数/评分分数等）
        /// </summary>
        public decimal BehaviorValue { get; set; }

        /// <summary>
        /// 行为发生时间戳（毫秒）
        /// </summary>
        public long Timestamp { get; set; }
    }
}
