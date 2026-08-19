using System.ComponentModel.DataAnnotations;

namespace SmartUrlShortener.Models;

public class UrlMapping
{
    public int Id { get; set; }

    [Required]
    public string OriginalUrl { get; set; } = string.Empty;

    [Required]
    public string ShortCode { get; set; } = string.Empty;

    public int ClickCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}