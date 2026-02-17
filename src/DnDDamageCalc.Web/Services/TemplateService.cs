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
    private readonly object _cacheLock = new();

    public TemplateService(IWebHostEnvironment env)
    {
        _env = env;
        _templateCache = new Dictionary<string, Template>();
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

    private Template GetOrLoadTemplate(string templateName)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
            {
                return cachedTemplate;
            }
        }

        // Load from file
        var templatePath = Path.Combine(_env.WebRootPath, "templates", $"{templateName}.scriban");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templatePath}");
        }

        var content = File.ReadAllText(templatePath);
        var template = Template.Parse(content, templatePath);

        // Cache it
        lock (_cacheLock)
        {
            _templateCache[templateName] = template;
        }

        return template;
    }
}

