namespace DHY.Core;

/// <summary>
/// 副本模板种子数据（含世界难度修正值映射）
/// 难度等级 → 修正值: E=-3, D=-2, C=0, B=+2, A=+3
/// </summary>
public class GameDungeonTemplateSeedData : ISqlSugarEntitySeedData<GameDungeonTemplate>
{
    public IEnumerable<GameDungeonTemplate> HasData()
    {
        return new[]
        {
            new GameDungeonTemplate
            {
                Id = 1800000000001,
                Name = "暗影庄园的秘密",
                WorldTheme = "哥特悬疑",
                Difficulty = "E",
                DifficultyModifier = -3,
                TimeLimitDays = 3,
                Tags = new List<string> { "新手", "悬疑", "探索" },
                Description = "一座被遗忘的庄园中，隐藏着家族诅咒的真相。适合初出茅庐的冒险者。",
                MaxLevel = 0
            },
            new GameDungeonTemplate
            {
                Id = 1800000000002,
                Name = "消失的商队",
                WorldTheme = "中世纪奇幻",
                Difficulty = "D",
                DifficultyModifier = -2,
                TimeLimitDays = 4,
                Tags = new List<string> { "调查", "荒野", "商队" },
                Description = "一支商队在穿越迷雾森林后离奇消失，你需要找到他们的下落。",
                MaxLevel = 0
            },
            new GameDungeonTemplate
            {
                Id = 1800000000003,
                Name = "宫廷暗流",
                WorldTheme = "宫廷政治",
                Difficulty = "C",
                DifficultyModifier = 0,
                TimeLimitDays = 5,
                Tags = new List<string> { "政治", "社交", "阴谋" },
                Description = "王宫内部暗流涌动，各方势力角逐权力，你必须在夹缝中生存。",
                MaxLevel = 0
            },
            new GameDungeonTemplate
            {
                Id = 1800000000004,
                Name = "龙脊要塞",
                WorldTheme = "高魔奇幻",
                Difficulty = "B",
                DifficultyModifier = 2,
                TimeLimitDays = 5,
                Tags = new List<string> { "战斗", "要塞", "龙族" },
                Description = "古老的龙脊要塞中封印着远古之力，强大的守卫不会轻易放你通行。",
                MaxLevel = 0
            },
            new GameDungeonTemplate
            {
                Id = 1800000000005,
                Name = "深渊裂隙",
                WorldTheme = "末日幻想",
                Difficulty = "A",
                DifficultyModifier = 3,
                TimeLimitDays = 7,
                Tags = new List<string> { "高难度", "深渊", "终极挑战" },
                Description = "世界边缘的裂隙通往深渊，只有最强的冒险者才能在其中存活并封印混沌之力。",
                MaxLevel = 0
            }
        };
    }
}
