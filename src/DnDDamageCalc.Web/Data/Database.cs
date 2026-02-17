using Microsoft.Data.Sqlite;

namespace DnDDamageCalc.Web.Data;

public static class Database
{
    private static string _connectionString = "Data Source=characters.db";

    public static void Configure(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        return connection;
    }

    public static void Initialize()
    {
        using var connection = CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Characters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SupabaseUserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Data BLOB NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_characters_user ON Characters(SupabaseUserId);
            CREATE TABLE IF NOT EXISTS EncounterSettings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SupabaseUserId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Data TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_encounter_settings_user ON EncounterSettings(SupabaseUserId);
            """;
        cmd.ExecuteNonQuery();
    }
}
