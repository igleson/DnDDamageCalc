using Scriban;

namespace DnDDamageCalc.Web.Services;

/// <summary>
/// Scriban-based template service for rendering HTML fragments.
/// Templates are loaded from wwwroot/templates/ and cached after first use.
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<string, Template> _templateCache;
    private readonly Dictionary<string, DateTime> _templateLastModified;
    private readonly object _cacheLock = new();

    public TemplateService(IWebHostEnvironment env)
    {
        _env = env;
        _templateCache = new Dictionary<string, Template>();
        _templateLastModified = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Renders a template by name with the given model.
    /// </summary>
    /// <param name="templateName">Name of the template file (without .scriban extension)</param>
    /// <param name="model">Model object or anonymous object with properties for the template</param>
    /// <returns>Rendered HTML string</returns>
    public string Render(string templateName, object? model = null)
    {
        var template = GetOrLoadTemplate(templateName);
        return template.Render(model ?? new { });
    }

    /// <summary>
    /// Clears the template cache. Used for hot reloading in development.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _templateCache.Clear();
            _templateLastModified.Clear();
        }
    }

    private Template GetOrLoadTemplate(string templateName)
    {
        var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateName}.scriban");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templatePath}");
        }

        var fileInfo = new FileInfo(templatePath);
        var lastModified = fileInfo.LastWriteTimeUtc;

        // Check cache and file modification time
        lock (_cacheLock)
        {
            if (_templateCache.TryGetValue(templateName, out var cachedTemplate) &&
                _templateLastModified.TryGetValue(templateName, out var cachedTime) &&
                cachedTime >= lastModified)
            {
                return cachedTemplate;
            }
        }

        // Load from file
        var content = File.ReadAllText(templatePath);
        var template = Template.Parse(content, templatePath);

        // Cache it
        lock (_cacheLock)
        {
            _templateCache[templateName] = template;
            _templateLastModified[templateName] = lastModified;
        }

        return template;
    }
}

