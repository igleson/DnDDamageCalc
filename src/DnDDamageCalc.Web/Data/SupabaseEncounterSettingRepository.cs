using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Data;

public class SupabaseEncounterSettingRepository : IEncounterSettingRepository
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _anonKey;

    public SupabaseEncounterSettingRepository(HttpClient http, string url, string anonKey)
    {
        _http = http;
        _url = url.TrimEnd('/');
        _anonKey = anonKey;
    }

    public async Task<int> SaveAsync(EncounterSetting setting, string userId, string accessToken)
    {
        var jsonData = JsonSerializer.Serialize(setting.Combats, EncounterSettingJsonContext.Default.ListCombatDefinition);
        var dataElement = JsonDocument.Parse(jsonData).RootElement;

        if (setting.Id > 0)
        {
            var update = new EncounterUpdatePayload
            {
                Name = setting.Name,
                Data = dataElement
            };
            var updateJson = JsonSerializer.Serialize(update, EncounterSettingJsonContext.Default.EncounterUpdatePayload);

            var request = new HttpRequestMessage(HttpMethod.Patch, $"{_url}/rest/v1/encounter_settings?id=eq.{setting.Id}")
            {
                Content = new StringContent(updateJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("apikey", _anonKey);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return setting.Id;
        }

        var insert = new EncounterInsertPayload
        {
            UserId = userId,
            Name = setting.Name,
            Data = dataElement
        };
        var insertJson = JsonSerializer.Serialize(insert, EncounterSettingJsonContext.Default.EncounterInsertPayload);

        var insertRequest = new HttpRequestMessage(HttpMethod.Post, $"{_url}/rest/v1/encounter_settings")
        {
            Content = new StringContent(insertJson, Encoding.UTF8, "application/json")
        };
        insertRequest.Headers.Add("apikey", _anonKey);
        insertRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
        insertRequest.Headers.Add("Prefer", "return=representation");

        var insertResponse = await _http.SendAsync(insertRequest);
        insertResponse.EnsureSuccessStatusCode();

        var responseJson = await insertResponse.Content.ReadAsStringAsync();
        var inserted = JsonSerializer.Deserialize(responseJson, EncounterSettingJsonContext.Default.ListDbEncounterSetting);
        return inserted?[0].Id ?? 0;
    }

    public async Task<EncounterSetting?> GetByIdAsync(int id, string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/encounter_settings?id=eq.{id}&select=id,name,data");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize(json, EncounterSettingJsonContext.Default.ListDbEncounterSetting);
        if (results is null || results.Count == 0) return null;

        var row = results[0];
        return new EncounterSetting
        {
            Id = row.Id,
            Name = row.Name,
            Combats = row.Data ?? []
        };
    }

    public async Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_url}/rest/v1/encounter_settings?select=id,name&order=name.asc");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize(json, EncounterSettingJsonContext.Default.ListEncounterSettingListItem);
        return results?.Select(r => (r.Id, r.Name)).ToList() ?? [];
    }

    public async Task DeleteAsync(int id, string userId, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{_url}/rest/v1/encounter_settings?id=eq.{id}");
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

internal class DbEncounterSetting
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public List<CombatDefinition>? Data { get; set; }
}

internal class EncounterSettingListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

internal class EncounterInsertPayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

internal class EncounterUpdatePayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

[JsonSerializable(typeof(List<CombatDefinition>))]
[JsonSerializable(typeof(List<DbEncounterSetting>))]
[JsonSerializable(typeof(List<EncounterSettingListItem>))]
[JsonSerializable(typeof(EncounterInsertPayload))]
[JsonSerializable(typeof(EncounterUpdatePayload))]
internal partial class EncounterSettingJsonContext : JsonSerializerContext;
