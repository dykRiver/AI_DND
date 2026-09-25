using System.Text.RegularExpressions;

namespace DHY.Game.AI.Prompts;

/// <summary>
/// Prompt模板管理服务
/// </summary>
public class PromptTemplateService : ITransient
{
    private static readonly string _templateBasePath;
    private static readonly Dictionary<string, string> _templateCache = new();
    private static readonly object _cacheLock = new();

    static PromptTemplateService()
    {
        _templateBasePath = Path.Combine(AppContext.BaseDirectory, "Prompts", "Templates");
    }

    /// <summary>
    /// 加载模板
    /// </summary>
    /// <param name="templateName">模板名称（不含扩展名）</param>
    /// <returns>模板内容</returns>
    public string LoadTemplate(string templateName)
    {
        lock (_cacheLock)
        {
            if (_templateCache.TryGetValue(templateName, out var cached))
                return cached;
        }

        var filePath = Path.Combine(_templateBasePath, $"{templateName}.txt");
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Prompt模板不存在: {templateName}", filePath);

        var content = File.ReadAllText(filePath);

        lock (_cacheLock)
        {
            _templateCache[templateName] = content;
        }

        return content;
    }

    /// <summary>
    /// 按选项节奏档裁剪模板：仅保留适用档位（keepFine=true 保留细粒度档，false 保留粗粒度档）
    /// 的规则块，删除另一档块并清除档位标记，从而减少无关指令与Token。
    /// 标记格式：&lt;!--TIER:FINE--&gt;...&lt;!--/TIER:FINE--&gt; 与 &lt;!--TIER:COARSE--&gt;...&lt;!--/TIER:COARSE--&gt;。
    /// 无标记的模板原样返回（向后兼容）。
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="keepFine">true 保留细粒度档，false 保留粗粒度档</param>
    /// <returns>裁剪后的模板内容</returns>
    public string SelectPacingTier(string template, bool keepFine)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var dropTier = keepFine ? "COARSE" : "FINE";
        // 1. 删除不适用档位整块（含首尾标记与其后换行）
        var result = Regex.Replace(template,
            $@"<!--TIER:{dropTier}-->[\s\S]*?<!--/TIER:{dropTier}-->\r?\n?", string.Empty);
        // 2. 清除保留档位残留的标记行
        result = Regex.Replace(result, @"[ \t]*<!--/?TIER:(?:FINE|COARSE)-->[ \t]*\r?\n?", string.Empty);
        return result;
    }

    /// <summary>
    /// 按是否存在玩家目标裁剪模板：hasGoal=true 保留 &lt;!--GOAL:ON--&gt; 块、删除 &lt;!--GOAL:OFF--&gt; 块；
    /// hasGoal=false 反之（保留 OFF 块、删除 ON 块）。清除保留分支残留标记，减少无关指令与Token。
    /// 标记格式：&lt;!--GOAL:ON--&gt;...&lt;!--/GOAL:ON--&gt; 与 &lt;!--GOAL:OFF--&gt;...&lt;!--/GOAL:OFF--&gt;；
    /// 允许只存在其中一个分支（如 SuggestionsOnly 模板仅需 ON 块），缺失分支为无操作。
    /// 无标记的模板原样返回（向后兼容）。
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="hasGoal">true 保留 GOAL:ON 分支，false 保留 GOAL:OFF 分支</param>
    /// <returns>裁剪后的模板内容</returns>
    public string SelectGoalMode(string template, bool hasGoal)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var dropMode = hasGoal ? "OFF" : "ON";
        // 1. 删除不适用分支整块（含首尾标记与其后换行）
        var result = Regex.Replace(template,
            $@"<!--GOAL:{dropMode}-->[\s\S]*?<!--/GOAL:{dropMode}-->\r?\n?", string.Empty);
        // 2. 清除保留分支残留的标记行
        result = Regex.Replace(result, @"[ \t]*<!--/?GOAL:(?:ON|OFF)-->[ \t]*\r?\n?", string.Empty);
        return result;
    }

    /// <summary>
    /// 条件块裁剪：按 include 保留或删除模板中以 &lt;!--IF:name--&gt;...&lt;!--/IF:name--&gt; 标记的可选块。
    /// include=true 时保留块内容并清除标记；include=false 时删除整块（含首尾标记与其后换行）。
    /// 无匹配标记的模板原样返回（向后兼容）。
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="name">条件块名称（与标记中的 name 一致）</param>
    /// <param name="include">true 保留该块，false 删除该块</param>
    /// <returns>裁剪后的模板内容</returns>
    public string ResolveConditionalBlock(string template, string name, bool include)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var esc = Regex.Escape(name);
        return include
            // 保留：仅清除标记行，块内容原样保留
            ? Regex.Replace(template, $@"[ \t]*<!--/?IF:{esc}-->[ \t]*\r?\n?", string.Empty)
            // 删除：连同首尾标记与内容整块移除
            : Regex.Replace(template, $@"[ \t]*<!--IF:{esc}-->[\s\S]*?<!--/IF:{esc}-->[ \t]*\r?\n?", string.Empty);
    }

    /// <summary>
    /// 渲染模板（变量替换）
    /// 模板中用 variable_name 标记占位符
    /// </summary>
    /// <param name="template">模板内容</param>
    /// <param name="variables">变量字典</param>
    /// <returns>渲染后的文本</returns>
    public string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        if (variables == null || variables.Count == 0)
            return template;

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace(key, value ?? "");
        }

        return result;
    }

    /// <summary>
    /// 加载并渲染模板
    /// </summary>
    public string LoadAndRender(string templateName, Dictionary<string, string> variables)
    {
        var template = LoadTemplate(templateName);
        return RenderTemplate(template, variables);
    }

    /// <summary>
    /// 清除模板缓存
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _templateCache.Clear();
        }
    }
}
