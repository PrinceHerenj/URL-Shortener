using System;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartUrlShortener;
using SmartUrlShortener.Data;
using SmartUrlShortener.Models;
using Xunit;

namespace SmartUrlShortener.Tests;

public class UrlShortenerApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();

    public UrlShortenerApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove production SQLite DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Register isolated in-memory DB for tests (stable name per test instance)
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        });

        // Disable auto-redirect to assert 302 Found status
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task PostShorten_WithValidPayload_ReturnsSuccessStatusCode()
    {
        var request = new ShortenRequest("https://github.com");

        var response = await _client.PostAsJsonAsync("/shorten", request);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetRedirect_WithNonExistentCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/r/nonexistent123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRedirect_WithValidCode_Returns302RedirectToTarget()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.UrlMappings.Add(new UrlMapping
            {
                ShortCode = "git123",
                OriginalUrl = "https://github.com/repo",
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/r/git123");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://github.com/repo", response.Headers.Location?.ToString());
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://invalid-scheme.com")]
    [InlineData("javascript:alert(1)")]
    public async Task PostShorten_WithInvalidUrlFormat_ReturnsBadRequestOrUnprocessable(string invalidUrl)
    {
        var request = new ShortenRequest(invalidUrl);

        var response = await _client.PostAsJsonAsync("/shorten", request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422 for input '{invalidUrl}', but received {response.StatusCode}");
    }

    [Fact]
    public async Task PostShorten_WithMalformedJson_ReturnsBadRequest()
    {
        var content = new StringContent("{ malformed_json: ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/shorten", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostShorten_WithEmptyBody_ReturnsBadRequest()
    {
        var content = new StringContent("", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/shorten", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("../secret")]
    [InlineData("slug with spaces")]
    [InlineData("???")]
    public async Task GetRedirect_WithMalformedSlugCharacters_ReturnsNotFound(string invalidSlug)
    {
        var response = await _client.GetAsync($"/{invalidSlug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
