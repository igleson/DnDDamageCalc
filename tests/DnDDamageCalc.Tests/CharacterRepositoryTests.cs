using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Models;
using Microsoft.Data.Sqlite;

namespace DnDDamageCalc.Tests;

[Collection("Database")]
public class CharacterRepositoryTests : IDisposable
{
    private readonly string _dbPath;

    public CharacterRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"dnd_test_{Guid.NewGuid()}.db");
        Database.Configure($"Data Source={_dbPath}");
        Database.Initialize();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { }
        try { File.Delete(_dbPath + "-wal"); } catch { }
        try { File.Delete(_dbPath + "-shm"); } catch { }
    }

    [Fact]
    public void Save_NewCharacter_ReturnsPositiveId()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);
        Assert.True(id > 0);
    }

    [Fact]
    public void GetById_AfterSave_ReturnsCharacter()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);

        var loaded = CharacterRepository.GetById(id);

        Assert.NotNull(loaded);
        Assert.Equal("TestChar", loaded.Name);
        Assert.Equal(id, loaded.Id);
    }

    [Fact]
    public void GetById_PreservesLevels()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);

        var loaded = CharacterRepository.GetById(id)!;

        Assert.Single(loaded.Levels);
        Assert.Equal(1, loaded.Levels[0].LevelNumber);
    }

    [Fact]
    public void GetById_PreservesAttacks()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);

        var loaded = CharacterRepository.GetById(id)!;
        var attack = loaded.Levels[0].Attacks[0];

        Assert.Equal("Longsword", attack.Name);
        Assert.Equal(65, attack.HitPercent);
        Assert.Equal(5, attack.CritPercent);
        Assert.Equal(3, attack.FlatModifier);
        Assert.True(attack.MasteryVex);
        Assert.False(attack.MasteryTopple);
    }

    [Fact]
    public void GetById_PreservesDiceGroups()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);

        var loaded = CharacterRepository.GetById(id)!;
        var dice = loaded.Levels[0].Attacks[0].DiceGroups;

        Assert.Equal(2, dice.Count);
        Assert.Equal(2, dice[0].Quantity);
        Assert.Equal(6, dice[0].DieSize);
        Assert.Equal(1, dice[1].Quantity);
        Assert.Equal(4, dice[1].DieSize);
    }

    [Fact]
    public void Save_ExistingCharacter_Updates()
    {
        var character = CreateTestCharacter();
        var id = CharacterRepository.Save(character);

        character.Id = id;
        character.Name = "Updated";
        CharacterRepository.Save(character);

        var loaded = CharacterRepository.GetById(id)!;
        Assert.Equal("Updated", loaded.Name);
    }

    [Fact]
    public void ListAll_ReturnsAllCharacters()
    {
        CharacterRepository.Save(CreateTestCharacter("Alice"));
        CharacterRepository.Save(CreateTestCharacter("Bob"));

        var list = CharacterRepository.ListAll();

        Assert.True(list.Count >= 2);
        Assert.Contains(list, c => c.Name == "Alice");
        Assert.Contains(list, c => c.Name == "Bob");
    }

    [Fact]
    public void Delete_RemovesCharacter()
    {
        var id = CharacterRepository.Save(CreateTestCharacter());
        CharacterRepository.Delete(id);

        var loaded = CharacterRepository.GetById(id);
        Assert.Null(loaded);
    }

    [Fact]
    public void GetById_NonExistent_ReturnsNull()
    {
        var loaded = CharacterRepository.GetById(99999);
        Assert.Null(loaded);
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
