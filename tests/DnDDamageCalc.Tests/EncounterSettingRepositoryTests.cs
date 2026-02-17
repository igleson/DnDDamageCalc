using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Models;
using Microsoft.Data.Sqlite;

namespace DnDDamageCalc.Tests;

[Collection("Database")]
public class EncounterSettingRepositoryTests : IDisposable
{
    private const string TestUserId = "test-user-id";
    private readonly string _dbPath;
    private readonly SqliteEncounterSettingRepository _repository;

    public EncounterSettingRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dnd_encounter_test_{Guid.NewGuid()}.db");
        Database.Configure($"Data Source={_dbPath}");
        Database.Initialize();
        _repository = new SqliteEncounterSettingRepository();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { }
        try { File.Delete(_dbPath + "-wal"); } catch { }
        try { File.Delete(_dbPath + "-shm"); } catch { }
    }

    [Fact]
    public async Task SaveAndGetById_RoundTripsCombats()
    {
        var id = await _repository.SaveAsync(new EncounterSetting
        {
            Name = "Adventuring Day",
            Combats =
            [
                new CombatDefinition { Rounds = 3, ShortRestAfter = false },
                new CombatDefinition { Rounds = 2, ShortRestAfter = true }
            ]
        }, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");

        Assert.NotNull(loaded);
        Assert.Equal("Adventuring Day", loaded.Name);
        Assert.Equal(2, loaded.Combats.Count);
        Assert.Equal(3, loaded.Combats[0].Rounds);
        Assert.True(loaded.Combats[1].ShortRestAfter);
    }

    [Fact]
    public async Task ListAll_OnlyReturnsOwnSettings()
    {
        await _repository.SaveAsync(new EncounterSetting
        {
            Name = "Mine",
            Combats = [new CombatDefinition { Rounds = 1 }]
        }, TestUserId, "fake-token");
        await _repository.SaveAsync(new EncounterSetting
        {
            Name = "Other",
            Combats = [new CombatDefinition { Rounds = 1 }]
        }, "other-user-id", "fake-token");

        var mine = await _repository.ListAllAsync(TestUserId, "fake-token");

        Assert.Single(mine);
        Assert.Equal("Mine", mine[0].Name);
    }
}
