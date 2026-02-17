using System.Text.Json;
using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Data;

public class SqliteEncounterSettingRepository : IEncounterSettingRepository
{
    public async Task<int> SaveAsync(EncounterSetting setting, string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
            var combatsJson = JsonSerializer.Serialize(setting.Combats, EncounterSettingJsonContext.Default.ListCombatDefinition);

            if (setting.Id > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE EncounterSettings SET Name = @name, Data = @data WHERE Id = @id AND SupabaseUserId = @userId";
                cmd.Parameters.AddWithValue("@id", setting.Id);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@name", setting.Name);
                cmd.Parameters.AddWithValue("@data", combatsJson);
                cmd.ExecuteNonQuery();
                return setting.Id;
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO EncounterSettings (SupabaseUserId, Name, Data) VALUES (@userId, @name, @data); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@name", setting.Name);
                cmd.Parameters.AddWithValue("@data", combatsJson);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        });
    }

    public async Task<EncounterSetting?> GetByIdAsync(int id, string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Data FROM EncounterSettings WHERE Id = @id AND SupabaseUserId = @userId";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var data = reader.GetString(2);
            return new EncounterSetting
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Combats = JsonSerializer.Deserialize(data, EncounterSettingJsonContext.Default.ListCombatDefinition) ?? []
            };
        });
    }

    public async Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken)
    {
        return await Task.Run(() =>
        {
            using var connection = Database.CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM EncounterSettings WHERE SupabaseUserId = @userId ORDER BY Name";
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
            cmd.CommandText = "DELETE FROM EncounterSettings WHERE Id = @id AND SupabaseUserId = @userId";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.ExecuteNonQuery();
        });
    }
}
