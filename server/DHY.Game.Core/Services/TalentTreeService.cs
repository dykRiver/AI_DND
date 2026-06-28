namespace DHY.Game.Core.Services;

/// <summary>
/// 天赋树系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class TalentTreeService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameTalentNode> _nodeRep;
    private readonly SqlSugarRepository<GamePlayerMeta> _metaRep;
    private readonly ISqlSugarClient _db;

    public TalentTreeService(
        SqlSugarRepository<GameTalentNode> nodeRep,
        SqlSugarRepository<GamePlayerMeta> metaRep,
        ISqlSugarClient db)
    {
        _nodeRep = nodeRep;
        _metaRep = metaRep;
        _db = db;
    }

    /// <summary>
    /// 获取完整天赋树数据
    /// </summary>
    [DisplayName("获取天赋树")]
    [HttpGet("getTalentTree")]
    public async Task<TalentTreeOutput> GetTalentTreeAsync([FromQuery] MetaIdInput input)
    {
        var meta = await _metaRep.GetByIdAsync(input.MetaId);
        if (meta == null)
            throw Oops.Oh("Meta档案不存在");

        var nodes = await _nodeRep.AsQueryable()
            .Where(n => n.MetaId == input.MetaId)
            .OrderBy(n => n.RouteName)
            .OrderBy(n => n.Position)
            .ToListAsync();

        if (nodes.Count == 0)
        {
            // 自动初始化
            await InitializeTalentTreeInternalAsync(input.MetaId);
            nodes = await _nodeRep.AsQueryable()
                .Where(n => n.MetaId == input.MetaId)
                .OrderBy(n => n.RouteName)
                .OrderBy(n => n.Position)
                .ToListAsync();
        }

        var output = new TalentTreeOutput
        {
            AvailablePoints = meta.TalentPoints,
            Nodes = nodes.Select(n => new TalentNodeOutput
            {
                NodePath = n.NodePath,
                NodeName = n.NodeName,
                NodeEffect = n.NodeEffect ?? "",
                RouteName = n.RouteName,
                Position = n.Position,
                IsUnlocked = n.IsUnlocked,
                IsBridge = n.IsBridge,
                CanUnlock = CanUnlockNode(n, nodes, meta.TalentPoints)
            }).ToList()
        };

        return output;
    }

    /// <summary>
    /// 解锁指定节点
    /// </summary>
    [DisplayName("解锁天赋节点")]
    [HttpPost("unlockNode")]
    public async Task<TalentNodeOutput> UnlockNodeAsync([FromBody] UnlockNodeInput input)
    {
        var meta = await _metaRep.GetByIdAsync(input.MetaId);
        if (meta == null)
            throw Oops.Oh("Meta档案不存在");

        if (meta.TalentPoints < 1)
            throw Oops.Oh("天赋点不足");

        var node = await _nodeRep.GetFirstAsync(n => n.MetaId == input.MetaId && n.NodePath == input.NodePath);
        if (node == null)
            throw Oops.Oh("天赋节点不存在");

        if (node.IsUnlocked)
            throw Oops.Oh("该节点已解锁");

        // 获取所有节点用于验证前置条件
        var allNodes = await _nodeRep.AsQueryable()
            .Where(n => n.MetaId == input.MetaId)
            .ToListAsync();

        if (!CanUnlockNode(node, allNodes, meta.TalentPoints))
            throw Oops.Oh("未满足解锁条件(前置节点未解锁或桥接条件不满足)");

        // 解锁节点
        node.IsUnlocked = true;
        await _nodeRep.AsUpdateable(node)
            .UpdateColumns(n => new { n.IsUnlocked })
            .ExecuteCommandAsync();

        // 扣除天赋点
        meta.TalentPoints--;
        await _metaRep.AsUpdateable(meta)
            .UpdateColumns(m => new { m.TalentPoints })
            .ExecuteCommandAsync();

        return new TalentNodeOutput
        {
            NodePath = node.NodePath,
            NodeName = node.NodeName,
            NodeEffect = node.NodeEffect ?? "",
            RouteName = node.RouteName,
            Position = node.Position,
            IsUnlocked = true,
            IsBridge = node.IsBridge,
            CanUnlock = false
        };
    }

    /// <summary>
    /// 获取当前可解锁的节点列表
    /// </summary>
    [DisplayName("获取可解锁节点")]
    [HttpGet("getAvailableNodes")]
    public async Task<List<TalentNodeOutput>> GetAvailableNodesAsync([FromQuery] MetaIdInput input)
    {
        var meta = await _metaRep.GetByIdAsync(input.MetaId);
        if (meta == null)
            throw Oops.Oh("Meta档案不存在");

        var nodes = await _nodeRep.AsQueryable()
            .Where(n => n.MetaId == input.MetaId)
            .ToListAsync();

        return nodes
            .Where(n => !n.IsUnlocked && CanUnlockNode(n, nodes, meta.TalentPoints))
            .Select(n => new TalentNodeOutput
            {
                NodePath = n.NodePath,
                NodeName = n.NodeName,
                NodeEffect = n.NodeEffect ?? "",
                RouteName = n.RouteName,
                Position = n.Position,
                IsUnlocked = false,
                IsBridge = n.IsBridge,
                CanUnlock = true
            }).ToList();
    }

    /// <summary>
    /// 初始化完整天赋树
    /// </summary>
    [DisplayName("初始化天赋树")]
    [HttpPost("initializeTalentTree")]
    public async Task InitializeTalentTreeApiAsync([FromBody] MetaIdInput input)
        => await InitializeTalentTreeInternalAsync(input.MetaId);

    /// <summary>
    /// 初始化天赋树（内部调用）
    /// </summary>
    internal async Task InitializeTalentTreeInternalAsync(long metaId)
    {
        // 检查是否已初始化
        var existCount = await _nodeRep.AsQueryable()
            .Where(n => n.MetaId == metaId)
            .CountAsync();

        if (existCount > 0)
            return;

        var nodes = GetPredefinedNodes(metaId);
        await _db.Insertable(nodes).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取节点效果描述
    /// </summary>
    [DisplayName("获取节点效果")]
    [HttpGet("getNodeEffect")]
    public Task<string> GetNodeEffectAsync([FromQuery] NodePathInput input)
    {
        var effect = NodeEffects.TryGetValue(input.NodePath, out var desc) ? desc : "未知效果";
        return Task.FromResult(effect);
    }

    #region 内部方法

    /// <summary>
    /// 判断节点是否可解锁
    /// </summary>
    private static bool CanUnlockNode(GameTalentNode node, List<GameTalentNode> allNodes, int talentPoints)
    {
        if (node.IsUnlocked) return false;
        if (talentPoints < 1) return false;

        if (node.IsBridge)
        {
            // 桥接节点: 两侧路线各有至少3个节点解锁
            var bridgeRoutes = GetBridgeRoutes(node.NodePath);
            if (bridgeRoutes == null) return false;

            foreach (var route in bridgeRoutes)
            {
                var unlockedCount = allNodes.Count(n =>
                    n.RouteName == route && !n.IsBridge && n.IsUnlocked);
                if (unlockedCount < 3)
                    return false;
            }
            return true;
        }
        else
        {
            // 普通节点: 前置节点(同路线position-1)已解锁
            if (node.Position == 1)
                return true; // 第一个节点无前置

            var prevNode = allNodes.FirstOrDefault(n =>
                n.RouteName == node.RouteName &&
                !n.IsBridge &&
                n.Position == node.Position - 1);

            return prevNode != null && prevNode.IsUnlocked;
        }
    }

    /// <summary>
    /// 获取桥接节点连接的两条路线
    /// </summary>
    private static string[]? GetBridgeRoutes(string nodePath)
    {
        return nodePath switch
        {
            "bridge_combat_stealth" => new[] { "combat", "stealth" },
            "bridge_combat_social" => new[] { "combat", "social" },
            "bridge_stealth_social" => new[] { "stealth", "social" },
            "bridge_combat_survival" => new[] { "combat", "survival" },
            _ => null
        };
    }

    /// <summary>
    /// 预定义天赋树节点
    /// </summary>
    private static List<GameTalentNode> GetPredefinedNodes(long metaId)
    {
        var nodes = new List<GameTalentNode>();

        // combat路线
        var combatNames = new[] { "近战精通", "重击", "格挡", "连击", "武器专精", "战术眼", "铁壁", "狂暴", "处刑人", "战神" };
        for (int i = 0; i < combatNames.Length; i++)
        {
            nodes.Add(new GameTalentNode
            {
                MetaId = metaId,
                NodePath = $"combat_{i + 1}",
                NodeName = combatNames[i],
                NodeEffect = NodeEffects.GetValueOrDefault($"combat_{i + 1}", ""),
                IsUnlocked = false,
                IsBridge = false,
                RouteName = "combat",
                Position = i + 1
            });
        }

        // stealth路线
        var stealthNames = new[] { "轻步", "暗影", "解锁", "陷阱感知", "暗杀", "逃脱", "伪装", "幻影", "影舞", "无影" };
        for (int i = 0; i < stealthNames.Length; i++)
        {
            nodes.Add(new GameTalentNode
            {
                MetaId = metaId,
                NodePath = $"stealth_{i + 1}",
                NodeName = stealthNames[i],
                NodeEffect = NodeEffects.GetValueOrDefault($"stealth_{i + 1}", ""),
                IsUnlocked = false,
                IsBridge = false,
                RouteName = "stealth",
                Position = i + 1
            });
        }

        // social路线
        var socialNames = new[] { "洞察", "话术", "威压", "魅惑", "谈判", "煽动", "间谍", "操控", "领袖", "传奇" };
        for (int i = 0; i < socialNames.Length; i++)
        {
            nodes.Add(new GameTalentNode
            {
                MetaId = metaId,
                NodePath = $"social_{i + 1}",
                NodeName = socialNames[i],
                NodeEffect = NodeEffects.GetValueOrDefault($"social_{i + 1}", ""),
                IsUnlocked = false,
                IsBridge = false,
                RouteName = "social",
                Position = i + 1
            });
        }

        // survival路线
        var survivalNames = new[] { "急救", "觅食", "方向感", "天气预知", "追踪", "炼药", "坚韧", "适应", "不屈", "永生" };
        for (int i = 0; i < survivalNames.Length; i++)
        {
            nodes.Add(new GameTalentNode
            {
                MetaId = metaId,
                NodePath = $"survival_{i + 1}",
                NodeName = survivalNames[i],
                NodeEffect = NodeEffects.GetValueOrDefault($"survival_{i + 1}", ""),
                IsUnlocked = false,
                IsBridge = false,
                RouteName = "survival",
                Position = i + 1
            });
        }

        // 桥接节点
        nodes.Add(new GameTalentNode
        {
            MetaId = metaId,
            NodePath = "bridge_combat_stealth",
            NodeName = "暗杀战技",
            NodeEffect = "近战攻击对未察觉目标造成额外伤害,隐匿判定+2",
            IsUnlocked = false,
            IsBridge = true,
            RouteName = "bridge",
            Position = 0
        });

        nodes.Add(new GameTalentNode
        {
            MetaId = metaId,
            NodePath = "bridge_combat_social",
            NodeName = "审讯术",
            NodeEffect = "威吓判定+3,可在战斗中使用威压迫使敌人投降",
            IsUnlocked = false,
            IsBridge = true,
            RouteName = "bridge",
            Position = 0
        });

        nodes.Add(new GameTalentNode
        {
            MetaId = metaId,
            NodePath = "bridge_stealth_social",
            NodeName = "情报网",
            NodeEffect = "每次进入新区域自动获取一条情报,社交探听DC-3",
            IsUnlocked = false,
            IsBridge = true,
            RouteName = "bridge",
            Position = 0
        });

        nodes.Add(new GameTalentNode
        {
            MetaId = metaId,
            NodePath = "bridge_combat_survival",
            NodeName = "野战医官",
            NodeEffect = "战斗中可使用急救恢复HP,急救判定+3",
            IsUnlocked = false,
            IsBridge = true,
            RouteName = "bridge",
            Position = 0
        });

        return nodes;
    }

    /// <summary>
    /// 节点效果定义
    /// </summary>
    private static readonly Dictionary<string, string> NodeEffects = new()
    {
        // combat
        ["combat_1"] = "近战武器攻击判定+1",
        ["combat_2"] = "近战伤害+2,可触发重击效果",
        ["combat_3"] = "受到近战攻击时防御判定+2",
        ["combat_4"] = "单回合可进行两次攻击判定",
        ["combat_5"] = "选定一种武器类型,使用时判定+2",
        ["combat_6"] = "战斗开始时获知敌方弱点",
        ["combat_7"] = "HP低于30%时防御判定+4",
        ["combat_8"] = "HP低于50%时攻击判定+3",
        ["combat_9"] = "对濒死目标伤害翻倍",
        ["combat_10"] = "所有战斗判定+2,免疫恐惧效果",
        // stealth
        ["stealth_1"] = "移动时隐匿判定+1",
        ["stealth_2"] = "在阴暗环境隐匿判定额外+2",
        ["stealth_3"] = "开锁和解除装置判定+2",
        ["stealth_4"] = "自动感知5米内陷阱,调查判定+1",
        ["stealth_5"] = "对未察觉目标攻击判定+3",
        ["stealth_6"] = "脱离战斗时敏捷豁免+3",
        ["stealth_7"] = "变装和伪装判定+3",
        ["stealth_8"] = "可创造幻象分身干扰敌人",
        ["stealth_9"] = "隐匿状态移动速度不减",
        ["stealth_10"] = "完美隐匿,非魔法手段无法侦测",
        // social
        ["social_1"] = "洞悉判定+1,可感知谎言",
        ["social_2"] = "说服和欺瞒判定+1",
        ["social_3"] = "威吓判定+2,可影响同等级NPC",
        ["social_4"] = "魅力相关判定+2",
        ["social_5"] = "商业谈判时价格优惠20%",
        ["social_6"] = "可煽动NPC群体情绪",
        ["social_7"] = "可获取NPC隐藏信息",
        ["social_8"] = "可改变NPC的行动计划",
        ["social_9"] = "可号召NPC协助战斗",
        ["social_10"] = "所有社交判定+3,NPC初始态度+1",
        // survival
        ["survival_1"] = "急救判定+2,稳定濒死角色DC-3",
        ["survival_2"] = "野外休息恢复额外HP",
        ["survival_3"] = "导航判定+2,不会迷路",
        ["survival_4"] = "可预知下一时段天气变化",
        ["survival_5"] = "追踪判定+3,可追踪隐匿目标",
        ["survival_6"] = "可制作治疗药剂和毒药",
        ["survival_7"] = "体质豁免+2,抵抗疾病和毒素",
        ["survival_8"] = "环境伤害减半",
        ["survival_9"] = "濒死时自动稳定,死亡豁免+3",
        ["survival_10"] = "HP降至0时保留1HP(每副本1次)"
    };

    #endregion
}

/// <summary>
/// 解锁天赋节点输入
/// </summary>
public class UnlockNodeInput
{
    /// <summary>Meta档案ID</summary>
    public long MetaId { get; set; }
    /// <summary>节点路径</summary>
    public string NodePath { get; set; } = "";
}

/// <summary>
/// 节点路径输入
/// </summary>
public class NodePathInput
{
    /// <summary>节点路径</summary>
    public string NodePath { get; set; } = "";
}
