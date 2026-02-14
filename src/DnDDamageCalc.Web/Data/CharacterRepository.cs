using DnDDamageCalc.Web.Models;
using ProtoBuf;

namespace DnDDamageCalc.Web.Data;

public static class CharacterRepository
{
    public static int Save(Character character)
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
            cmd.CommandText = "UPDATE Characters SET Name = @name, Data = @data WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", character.Id);
            cmd.Parameters.AddWithValue("@name", character.Name);
            cmd.Parameters.AddWithValue("@data", blob);
            cmd.ExecuteNonQuery();
            return character.Id;
        }
        else
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Characters (Name, Data) VALUES (@name, @data); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", character.Name);
            cmd.Parameters.AddWithValue("@data", blob);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    public static Character? GetById(int id)
    {
        using var connection = Database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Data FROM Characters WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

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
    }

    public static List<(int Id, string Name)> ListAll()
    {
        using var connection = Database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM Characters ORDER BY Name";

        var list = new List<(int, string)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return list;
    }

    public static void Delete(int id)
    {
        using var connection = Database.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Characters WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
