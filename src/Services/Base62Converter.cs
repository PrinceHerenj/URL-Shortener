using System;
using System.Text;

namespace SmartUrlShortener.Services;

public static class Base62Converter
{
    private const string CharacterSet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string Encode(long number)
    {
        if (number < 0)
            throw new ArgumentOutOfRangeException(nameof(number), "Input must be non-negative.");

        if (number == 0)
            return "0";

        var result = new StringBuilder();
        while (number > 0)
        {
            result.Insert(0, CharacterSet[(int)(number % 62)]);
            number /= 62;
        }
        return result.ToString();
    }

    public static long Decode(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            throw new ArgumentException("Encoded string cannot be null or empty.", nameof(encoded));

        long result = 0;
        foreach (char c in encoded)
        {
            int index = CharacterSet.IndexOf(c);
            if (index == -1)
                throw new ArgumentException($"Invalid character '{c}' in Base62 string.", nameof(encoded));

            result = checked(result * 62 + index);
        }
        return result;
    }
}
