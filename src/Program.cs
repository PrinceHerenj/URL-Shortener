using Microsoft.EntityFrameworkCore;
using SmartUrlShortener.Data;
using SmartUrlShortener.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=urlshortener.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapPost("/shorten", async (ShortenRequest request, AppDbContext db, HttpContext httpContext) =>
{
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
        uri.Scheme is not ("http" or "https"))
    {
        return Results.BadRequest(new { error = "Please provide a valid absolute URL." });
    }

    string shortCode = Guid.NewGuid().ToString("N")[..6];

    var mapping = new UrlMapping
    {
        OriginalUrl = request.Url,
        ShortCode = shortCode,
        CreatedAt = DateTime.UtcNow
    };

    db.UrlMappings.Add(mapping);
    await db.SaveChangesAsync();

    var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/r/{shortCode}";

    return Results.Created($"/r/{shortCode}", new
    {
        shortCode = mapping.ShortCode,
        shorturl = shortUrl,
        originalUrl = mapping.OriginalUrl
    });
});

app.MapGet("/r/{code}", async (string code, AppDbContext db) =>
{
    var mapping = await db.UrlMappings.FirstOrDefaultAsync(m => m.ShortCode == code);

    if (mapping is null)
    {
        return Results.NotFound(new { error = "Short URL not found" });
    }

    mapping.ClickCount++;
    await db.SaveChangesAsync();

    return Results.Redirect(mapping.OriginalUrl, permanent: false);
});

app.MapGet("/analytics/{code}", async (string code, AppDbContext db) =>
{
    var mapping = await db.UrlMappings.FirstOrDefaultAsync(m => m.ShortCode == code);

    if (mapping is null)
    {
        return Results.NotFound(new { error = "Short URL not found." });
    }

    return Results.Ok(new
    {
        shortCode = mapping.ShortCode,
        originalUrl = mapping.OriginalUrl,
        totalClicks = mapping.ClickCount,
        createdAt = mapping.CreatedAt
    });
});

app.Run();

public partial class Program { }
