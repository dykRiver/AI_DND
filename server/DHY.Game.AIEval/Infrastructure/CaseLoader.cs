namespace DHY.Game.AIEval.Infrastructure;

/// <summary>
/// 评测案例加载器：Cases/{suite}/*.json，每个文件为同类型用例数组
/// </summary>
public static class CaseLoader
{
    /// <summary>案例字段采用 snake_case（与 AI 输出协议风格一致）</summary>
    public static readonly JsonSerializerSettings CaseJsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    /// <summary>
    /// 加载指定评测集的全部用例（按文件名排序保证顺序稳定）
    /// </summary>
    public static List<T> Load<T>(string suiteName)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Cases", suiteName);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"案例目录不存在: {dir}");

        var results = new List<T>();
        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
        {
            var json = File.ReadAllText(file);
            try
            {
                var cases = JsonConvert.DeserializeObject<List<T>>(json, CaseJsonSettings);
                if (cases == null || cases.Count == 0)
                    throw new InvalidDataException("用例数组为空");
                results.AddRange(cases);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"案例文件解析失败 {Path.GetFileName(file)}: {ex.Message}", ex);
            }
        }
        return results;
    }
}
