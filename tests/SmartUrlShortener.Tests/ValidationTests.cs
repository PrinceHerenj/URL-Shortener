using Xunit;

namespace SmartUrlShortener.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("https://www.google.com", true)]
    [InlineData("http://github.com", true)]
    [InlineData("ftp://invalid-scheme.com", false)]
    [InlineData("not-a-valid-url", false)]
    [InlineData("", false)]
    public void ValidateUrl_CorrectlyValidatesUrlFormats(string url, bool expectedValidity)
    {
        bool isValid = Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                       && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        Assert.Equal(expectedValidity, isValid);
    }
}
