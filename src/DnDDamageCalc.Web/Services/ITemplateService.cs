namespace DnDDamageCalc.Web.Services;

/// <summary>
/// Service for rendering Scriban templates.
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// Renders a template with the given model.
    /// </summary>
    /// <param name="templateName">Name of the template file (without .scriban extension)</param>
    /// <param name="model">Model object to pass to the template</param>
    /// <returns>Rendered HTML string</returns>
    string Render(string templateName, object? model = null);
}
