using DnDDamageCalc.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DnDDamageCalc.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _dbPath;

    public CustomWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dnd_test_{Guid.NewGuid()}.db");
        
        Environment.SetEnvironmentVariable("SUPABASE_URL", "https://fake.supabase.co");
        Environment.SetEnvironmentVariable("SUPABASE_ANON_KEY", "fake-anon-key");
        Environment.SetEnvironmentVariable("SUPABASE_SERVICE_KEY", "fake-service-key");
        Environment.SetEnvironmentVariable("TestUserId", "test-user-id");
        
        Database.Configure($"Data Source={_dbPath}");
        Database.Initialize();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICharacterRepository>();
            services.AddSingleton<ICharacterRepository, SqliteCharacterRepository>();
        });

        builder.UseSetting("TestUserId", "test-user-id");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TestUserId"] = "test-user-id"
            });
        });
    }

    public new void Dispose()
    {
        base.Dispose();
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { }
        try { File.Delete(_dbPath + "-wal"); } catch { }
        try { File.Delete(_dbPath + "-shm"); } catch { }
        GC.SuppressFinalize(this);
    }
}
