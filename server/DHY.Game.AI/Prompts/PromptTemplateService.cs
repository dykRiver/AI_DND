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
