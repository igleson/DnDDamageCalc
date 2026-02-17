using Microsoft.AspNetCore.Mvc.Testing;

namespace DnDDamageCalc.Tests;

[Collection("Database")]
public class EncounterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EncounterEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task SaveEncounter_ValidData_ReturnsFormAndConfirmation()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("encounterId", "0"),
            new KeyValuePair<string, string>("encounterName", "Boss Fight"),
            new KeyValuePair<string, string>("combat[0].rounds", "3"),
            new KeyValuePair<string, string>("combat[1].rounds", "2"),
            new KeyValuePair<string, string>("combat[1].shortRestAfter", "on")
        ]);
        var response = await _client.PostAsync("/encounter/save", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Boss Fight", html);
        Assert.Contains("Encounter", html);
        Assert.Contains("saved successfully", html);
    }

    [Fact]
    public async Task SaveEncounter_MissingName_ReturnsError()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("encounterId", "0"),
            new KeyValuePair<string, string>("encounterName", ""),
            new KeyValuePair<string, string>("combat[0].rounds", "2")
        ]);
        var response = await _client.PostAsync("/encounter/save", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Encounter setting name is required", html);
    }

    [Fact]
    public async Task EncounterForm_ReturnsHtml()
    {
        var response = await _client.GetAsync("/encounter/form");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("encounter-form", html);
        Assert.Contains("encounterName", html);
    }
}
