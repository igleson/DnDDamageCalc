using System.Net;
using DnDDamageCalc.Web.Data;
using DnDDamageCalc.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DnDDamageCalc.Tests;

[Collection("Database")]
public class CharacterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CharacterEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Root_RedirectsToIndexHtml()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/index.html", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetForm_ReturnsHtml()
    {
        var response = await _client.GetAsync("/character/form");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("character-form", html);
        Assert.Contains("characterName", html);
    }

    [Fact]
    public async Task GetList_ReturnsHtml()
    {
        var response = await _client.GetAsync("/character/list");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ValidatePercentages_Valid_ReturnsEmpty()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "60"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5")
        ]);
        var response = await _client.PostAsync("/character/validate-percentages", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal("", html);
    }

    [Fact]
    public async Task ValidatePercentages_Exceeds100_ReturnsError()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "90"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "20")
        ]);
        var response = await _client.PostAsync("/character/validate-percentages", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 100", html);
    }

    [Fact]
    public async Task SaveCharacter_ValidData_ReturnsConfirmation()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("characterId", "0"),
            new KeyValuePair<string, string>("characterName", "Gandalf"),
            new KeyValuePair<string, string>("level[0].number", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].name", "Staff Strike"),
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "65"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5"),
            new KeyValuePair<string, string>("level[0].attacks[0].flatModifier", "3"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].quantity", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].dieSize", "8")
        ]);
        var response = await _client.PostAsync("/character/save", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("saved successfully", html);
        Assert.Contains("Gandalf", html);
    }

    [Fact]
    public async Task SaveCharacter_MissingName_ReturnsError()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("characterId", "0"),
            new KeyValuePair<string, string>("characterName", "")
        ]);
        var response = await _client.PostAsync("/character/save", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Character name is required", html);
    }

    [Fact]
    public async Task GetCharacter_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/character/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IndexHtml_ContainsFormContainer()
    {
        var response = await _client.GetAsync("/index.html");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("form-container", html);
        Assert.Contains("htmx.org", html);
        Assert.Contains("picocss/pico", html);
    }

    [Fact]
    public async Task Calculate_ValidData_ReturnsResultsGraph()
    {
        var encounterSettingId = await CreateEncounterSettingAsync();
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("encounterSettingId", encounterSettingId.ToString()),
            new KeyValuePair<string, string>("characterName", "Test"),
            new KeyValuePair<string, string>("level[0].number", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].name", "Longsword"),
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "65"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5"),
            new KeyValuePair<string, string>("level[0].attacks[0].flatModifier", "3"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].quantity", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].dieSize", "8")
        ]);
        var response = await _client.PostAsync("/character/calculate", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Damage Statistics", html);
        Assert.Contains("id=\"damage-results-graph\"", html);
        Assert.Contains("data-series-toggle=\"average\"", html);
        Assert.Contains("data-damage-tooltip", html);
        Assert.Contains("onmouseenter=\"showDamageTooltip(event)\"", html);
        Assert.Contains("data-stat-label=\"Average\"", html);
        Assert.DoesNotContain("<table>", html);
    }

    [Fact]
    public async Task Calculate_NoLevels_ReturnsError()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("characterName", "Empty")
        ]);
        var response = await _client.PostAsync("/character/calculate", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("at least one level", html);
    }

    [Fact]
    public async Task Calculate_WithTopple_ReturnsResults()
    {
        var encounterSettingId = await CreateEncounterSettingAsync();
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("encounterSettingId", encounterSettingId.ToString()),
            new KeyValuePair<string, string>("characterName", "Topple Fighter"),
            new KeyValuePair<string, string>("level[0].number", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].name", "Warhammer"),
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "65"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5"),
            new KeyValuePair<string, string>("level[0].attacks[0].flatModifier", "4"),
            new KeyValuePair<string, string>("level[0].attacks[0].masteryTopple", "on"),
            new KeyValuePair<string, string>("level[0].attacks[0].topplePercent", "40"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].quantity", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].dieSize", "8")
        ]);
        var response = await _client.PostAsync("/character/calculate", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Damage Statistics", html);
    }

    [Fact]
    public async Task Calculate_WithoutEncounterSetting_ReturnsError()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("characterName", "No Encounter"),
            new KeyValuePair<string, string>("level[0].number", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].name", "Longsword"),
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "65"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5")
        ]);
        var response = await _client.PostAsync("/character/calculate", content);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Select an encounter setting", html);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip()
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("characterId", "0"),
            new KeyValuePair<string, string>("characterName", "Frodo"),
            new KeyValuePair<string, string>("level[0].number", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].name", "Sting"),
            new KeyValuePair<string, string>("level[0].attacks[0].hitPercent", "70"),
            new KeyValuePair<string, string>("level[0].attacks[0].critPercent", "5"),
            new KeyValuePair<string, string>("level[0].attacks[0].flatModifier", "2"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].quantity", "1"),
            new KeyValuePair<string, string>("level[0].attacks[0].dice[0].dieSize", "6")
        ]);
        var saveResponse = await _client.PostAsync("/character/save", content);
        saveResponse.EnsureSuccessStatusCode();

        var listResponse = await _client.GetAsync("/character/list");
        var listHtml = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("Frodo", listHtml);
    }

    private async Task<int> CreateEncounterSettingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEncounterSettingRepository>();
        return await repo.SaveAsync(new EncounterSetting
        {
            Name = "Test Encounter",
            Combats = [new CombatDefinition { Rounds = 3, ShortRestAfter = false }]
        }, "test-user-id", "fake-token");
    }
}
