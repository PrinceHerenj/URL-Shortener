using System;
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
}
