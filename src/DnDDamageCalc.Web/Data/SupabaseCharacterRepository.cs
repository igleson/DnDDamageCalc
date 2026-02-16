using DnDDamageCalc.Web.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace DnDDamageCalc.Web.Data;

public class SupabaseCharacterRepository : ICharacterRepository
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _anonKey;

    public SupabaseCharacterRepository(HttpClient http, string url, string anonKey)
    {
        _http = http;
        _url = url.TrimEnd('/');
        _anonKey = anonKey;
    }

    public async Task<int> SaveAsync(Character character, string userId, string accessToken)
    {
        var jsonData = JsonSerializer.Serialize(character.Levels, CharacterJsonContext.Default.ListCharacterLevel);
        var dataElement = JsonDocument.Parse(jsonData).RootElement;

        if (character.Id > 0)
        {
            var update = new UpdatePayload
            {
                Name = character.Name,
                Data = dataElement
            };
            var updateJson = JsonSerializer.Serialize(update, CharacterJsonContext.Default.UpdatePayload);

            var request = new HttpRequestMessage(HttpMethod.Patch, $"{_url}/rest/v1/characters?id=eq.{character.Id}")
            {
                Content = new StringContent(updateJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("apikey", _anonKey);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Supabase API error: {response.StatusCode}. URL: {request.RequestUri}. Response: {errorBody}");
            }
            
            return character.Id;
        }
        else
        {
            var insert = new InsertPayload
            {
                UserId = userId,
                Name = character.Name,
                Data = dataElement
            };
            var insertJson = JsonSerializer.Serialize(insert, CharacterJsonContext.Default.InsertPayload);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_url}/rest/v1/characters")
            {
                Content = new StringContent(insertJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("apikey", _anonKey);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("Prefer", "return=representation");

            var response = await _http.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Supabase API error: {response.StatusCode}. URL: {request.RequestUri}. Response: {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var inserted = JsonSerializer.Deserialize(responseJson, CharacterJsonContext.Default.ListDbCharacter);
            return inserted?[0].Id ?? 0;
        }
    }

    public async Task<Character?> GetByIdAsync(int id, string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/characters?id=eq.{id}&select=id,name,data");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize(json, CharacterJsonContext.Default.ListDbCharacter);
        if (results == null || results.Count == 0) return null;

        var dbChar = results[0];
        return new Character
        {
            Id = dbChar.Id,
            Name = dbChar.Name,
            Levels = dbChar.Data ?? []
        };
    }

    public async Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/characters?select=id,name&order=name.asc");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize(json, CharacterJsonContext.Default.ListCharacterListItem);
        return results?.Select(r => (r.Id, r.Name)).ToList() ?? [];
    }

    public async Task DeleteAsync(int id, string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_url}/rest/v1/characters?id=eq.{id}");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

internal class DbCharacter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public List<CharacterLevel>? Data { get; set; }
}

internal class CharacterListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

internal class InsertPayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

internal class UpdatePayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

[JsonSerializable(typeof(List<CharacterLevel>))]
[JsonSerializable(typeof(List<DbCharacter>))]
[JsonSerializable(typeof(List<CharacterListItem>))]
[JsonSerializable(typeof(InsertPayload))]
[JsonSerializable(typeof(UpdatePayload))]
internal partial class CharacterJsonContext : JsonSerializerContext;
