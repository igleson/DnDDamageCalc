namespace DnDDamageCalc.Web.Services;

/// <summary>
/// Service for tracking file changes for hot reload in development.
/// </summary>
public interface IFileWatcherService
{
    long GetLastModifiedTicks();
}

public class FileWatcherService : IFileWatcherService
{
    private readonly IWebHostEnvironment _env;
    private long _lastChecked = 0;

    public FileWatcherService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Returns the maximum last-modified time of all templates and CSS files.
    /// Used by client to detect when to refresh the page.
    /// </summary>
    public long GetLastModifiedTicks()
    {
        var templatesPath = Path.Combine(_env.WebRootPath, "templates");
        var cssPath = Path.Combine(_env.WebRootPath, "style.css");

        var maxTicks = 0L;

        // Check all template files
        if (Directory.Exists(templatesPath))
        {
            foreach (var file in Directory.GetFiles(templatesPath, "*.scriban"))
            {
                var info = new FileInfo(file);
                var ticks = info.LastWriteTimeUtc.Ticks;
                if (ticks > maxTicks)
                    maxTicks = ticks;
            }
        }

        // Check CSS file
        if (File.Exists(cssPath))
        {
            var info = new FileInfo(cssPath);
            var ticks = info.LastWriteTimeUtc.Ticks;
            if (ticks > maxTicks)
                maxTicks = ticks;
        }

        return maxTicks;
    }
}
