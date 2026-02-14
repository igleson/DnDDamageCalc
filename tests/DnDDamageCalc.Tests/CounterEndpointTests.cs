using Microsoft.AspNetCore.Mvc.Testing;

namespace DnDDamageCalc.Tests;

public class CounterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CounterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Root_RedirectsToIndexHtml()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/index.html", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Increment_FromZero_ReturnsOne()
    {
        var response = await _client.PostAsync("/counter/increment?count=0", null);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Count: 1", html);
    }

    [Fact]
    public async Task Increment_FromFive_ReturnsSix()
    {
        var response = await _client.PostAsync("/counter/increment?count=5", null);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Count: 6", html);
    }

    [Fact]
    public async Task Increment_DefaultCount_ReturnsOne()
    {
        var response = await _client.PostAsync("/counter/increment", null);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Count: 1", html);
    }

    [Fact]
    public async Task Increment_ReturnsHtmlContentType()
    {
        var response = await _client.PostAsync("/counter/increment?count=0", null);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task IndexHtml_ContainsHtmxScript()
    {
        var client = _client;
        var response = await client.GetAsync("/index.html");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("htmx.org", html);
    }

    [Fact]
    public async Task IndexHtml_ContainsPicoCss()
    {
        var response = await _client.GetAsync("/index.html");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("picocss/pico", html);
    }
}
