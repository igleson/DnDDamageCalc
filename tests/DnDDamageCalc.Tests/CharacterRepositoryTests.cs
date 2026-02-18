using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Models;
using Microsoft.Data.Sqlite;

namespace DnDDamageCalc.Tests;

[Collection("Database")]
public class CharacterRepositoryTests : IDisposable
{
    private const string TestUserId = "test-user-id";
    private readonly string _dbPath;
    private readonly SqliteCharacterRepository _repository;

    public CharacterRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dnd_test_{Guid.NewGuid()}.db");
        Database.Configure($"Data Source={_dbPath}");
        Database.Initialize();
        _repository = new SqliteCharacterRepository();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { }
        try { File.Delete(_dbPath + "-wal"); } catch { }
        try { File.Delete(_dbPath + "-shm"); } catch { }
    }

    [Fact]
    public async Task Save_NewCharacter_ReturnsPositiveId()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetById_AfterSave_ReturnsCharacter()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");

        Assert.NotNull(loaded);
        Assert.Equal("TestChar", loaded.Name);
        Assert.Equal(id, loaded.Id);
    }

    [Fact]
    public async Task GetById_PreservesLevels()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");

        Assert.NotNull(loaded);
        Assert.Single(loaded.Levels);
        Assert.Equal(1, loaded.Levels[0].LevelNumber);
        Assert.True(loaded.Levels[0].Resources.HasActionSurge);
        Assert.True(loaded.Levels[0].Resources.HasShieldMaster);
        Assert.Equal(45, loaded.Levels[0].Resources.ShieldMasterTopplePercent);
        Assert.True(loaded.Levels[0].Resources.HasHeroicInspiration);
        Assert.True(loaded.Levels[0].Resources.HasStudiedAttacks);
        Assert.True(loaded.Levels[0].Resources.HasExtraActionSurge);
        Assert.True(loaded.Levels[0].Resources.HasBoonOfCombatProwess);
        Assert.True(loaded.Levels[0].Resources.HasPureAdvantage);
        Assert.Equal(35, loaded.Levels[0].Resources.PureAdvantagePercent);
    }

    [Fact]
    public async Task GetById_PreservesAttacks()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");
        Assert.NotNull(loaded);
        var attack = loaded.Levels[0].Attacks[0];

        Assert.Equal("Longsword", attack.Name);
        Assert.Equal(65, attack.HitPercent);
        Assert.Equal(5, attack.CritPercent);
        Assert.Equal(3, attack.FlatModifier);
        Assert.True(attack.MasteryVex);
        Assert.False(attack.MasteryTopple);
        Assert.True(attack.RequiresSetup);
    }

    [Fact]
    public async Task GetById_PreservesDiceGroups()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");
        Assert.NotNull(loaded);
        var dice = loaded.Levels[0].Attacks[0].DiceGroups;

        Assert.Equal(2, dice.Count);
        Assert.Equal(2, dice[0].Quantity);
        Assert.Equal(6, dice[0].DieSize);
        Assert.Equal(1, dice[1].Quantity);
        Assert.Equal(4, dice[1].DieSize);
    }

    [Fact]
    public async Task Save_ExistingCharacter_Updates()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        character.Id = id;
        character.Name = "Updated";
        await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");
        Assert.NotNull(loaded);
        Assert.Equal("Updated", loaded.Name);
    }

    [Fact]
    public async Task ListAll_ReturnsAllCharacters()
    {
        await _repository.SaveAsync(CreateTestCharacter("Alice"), TestUserId, "fake-token");
        await _repository.SaveAsync(CreateTestCharacter("Bob"), TestUserId, "fake-token");

        var list = await _repository.ListAllAsync(TestUserId, "fake-token");

        Assert.True(list.Count >= 2);
        Assert.Contains(list, c => c.Name == "Alice");
        Assert.Contains(list, c => c.Name == "Bob");
    }

    [Fact]
    public async Task Delete_RemovesCharacter()
    {
        var id = await _repository.SaveAsync(CreateTestCharacter(), TestUserId, "fake-token");
        await _repository.DeleteAsync(id, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var loaded = await _repository.GetByIdAsync(99999, TestUserId, "fake-token");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetById_WrongUser_ReturnsNull()
    {
        var character = CreateTestCharacter();
        var id = await _repository.SaveAsync(character, TestUserId, "fake-token");

        var loaded = await _repository.GetByIdAsync(id, "other-user-id", "fake-token");
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ListAll_OnlyReturnsOwnCharacters()
    {
        await _repository.SaveAsync(CreateTestCharacter("MyChar"), TestUserId, "fake-token");
        await _repository.SaveAsync(CreateTestCharacter("OtherChar"), "other-user-id", "fake-token");

        var myList = await _repository.ListAllAsync(TestUserId, "fake-token");
        var otherList = await _repository.ListAllAsync("other-user-id", "fake-token");

        Assert.Single(myList);
        Assert.Equal("MyChar", myList[0].Name);
        Assert.Single(otherList);
        Assert.Equal("OtherChar", otherList[0].Name);
    }

    [Fact]
    public async Task Delete_WrongUser_DoesNotDelete()
    {
        var id = await _repository.SaveAsync(CreateTestCharacter(), TestUserId, "fake-token");
        await _repository.DeleteAsync(id, "other-user-id", "fake-token");

        var loaded = await _repository.GetByIdAsync(id, TestUserId, "fake-token");
        Assert.NotNull(loaded);
    }

    private static Character CreateTestCharacter(string name = "TestChar")
    {
        return new Character
        {
            Name = name,
            Levels =
            [
                new CharacterLevel
                {
                    LevelNumber = 1,
                    Resources = new LevelResources { HasActionSurge = true, HasShieldMaster = true, ShieldMasterTopplePercent = 45, HasHeroicInspiration = true, HasStudiedAttacks = true, HasExtraActionSurge = true, HasBoonOfCombatProwess = true, HasPureAdvantage = true, PureAdvantagePercent = 35 },
                    Attacks =
                    [
                        new Attack
                        {
                            Name = "Longsword",
                            HitPercent = 65,
                            CritPercent = 5,
                            FlatModifier = 3,
                            MasteryVex = true,
                            MasteryTopple = false,
                            RequiresSetup = true,
                            DiceGroups =
                            [
                                new DiceGroup { Quantity = 2, DieSize = 6 },
                                new DiceGroup { Quantity = 1, DieSize = 4 }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
