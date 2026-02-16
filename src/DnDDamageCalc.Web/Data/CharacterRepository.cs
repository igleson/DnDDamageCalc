using DnDDamageCalc.Web.Models;
using ProtoBuf;

namespace DnDDamageCalc.Web.Data;

public class SqliteCharacterRepository : ICharacterRepository
{
    public async Task<int> SaveAsync(Character character, string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();

            byte[] blob;
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, character.Levels);
            blob = ms.ToArray();
        }

        if (character.Id > 0)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Characters SET Name = @name, Data = @data WHERE Id = @id AND SupabaseUserId = @userId";
            cmd.Parameters.AddWithValue("@id", character.Id);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@name", character.Name);
            cmd.Parameters.AddWithValue("@data", blob);
            cmd.ExecuteNonQuery();
            return character.Id;
        }
        else
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Characters (SupabaseUserId, Name, Data) VALUES (@userId, @name, @data); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@name", character.Name);
            cmd.Parameters.AddWithValue("@data", blob);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        });
    }

    public async Task<Character?> GetByIdAsync(int id, string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Data FROM Characters WHERE Id = @id AND SupabaseUserId = @userId";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@userId", userId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var character = new Character
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1)
        };

        var blob = (byte[])reader["Data"];
        using var ms = new MemoryStream(blob);
        character.Levels = Serializer.Deserialize<List<CharacterLevel>>(ms);

        return character;
        });
    }

    public async Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Characters WHERE SupabaseUserId = @userId ORDER BY Name";
        cmd.Parameters.AddWithValue("@userId", userId);

        var list = new List<(int, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return list;
        });
    }

    public async Task DeleteAsync(int id, string userId, string accessToken)
    {
        await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Characters WHERE Id = @id AND SupabaseUserId = @userId";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        });
    }
}
