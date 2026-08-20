using System;
using SmartUrlShortener.Services;
using Xunit;

namespace SmartUrlShortener.Tests;

public class Base62Tests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(61, "Z")]
    [InlineData(62, "10")]
    [InlineData(125, "21")]
    public void Encode_KnownValues_ReturnsExpectedString(long id, string expected)
    {
        string actual = Base62Converter.Encode(id);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(1000)]
    [InlineData(987654321)]
    [InlineData(1000000000000L)]
    public void EncodeDecode_RoundTrip_ReturnsOriginalValue(long originalValue)
    {
        string encoded = Base62Converter.Encode(originalValue);
        long decoded = Base62Converter.Decode(encoded);

        Assert.Equal(originalValue, decoded);
    }

    [Theory]
    [InlineData("abc@123")]
    [InlineData("slug-with-hyphen")]
    [InlineData("hello!")]
    public void Decode_InvalidCharacters_ThrowsArgumentException(string invalidSlug)
    {
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode(invalidSlug));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Decode_NullOrEmptyString_ThrowsArgumentException(string? invalidSlug)
    {
        Assert.Throws<ArgumentException>(() => Base62Converter.Decode(invalidSlug!));
    }

    [Fact]
    public void Encode_NegativeNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62Converter.Encode(-1));
    }
}
