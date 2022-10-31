using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace libNOM.collect.Extensions;


internal static class StringExtensions
{
    private static readonly char[] ADDITIONAL_CHAR = new[] { '-', '+' };

    internal static JToken? Deserialize(this string self)
    {
        return JsonConvert.DeserializeObject(self) as JToken;
    }

    /// <summary>
    /// Encodes all the characters in the specified string into a sequence of bytes in UTF-8 format.
    /// </summary>
    /// <param name="self"></param>
    /// <returns>A byte array containing the results of encoding the set of characters.</returns>
    internal static byte[] GetBytes(this string self)
    {
        return Encoding.UTF8.GetBytes(self);
    }

    internal static byte[]? GetBytesFromBase64String(this string? self)
    {
        if (self is null)
            return null;

        return Convert.FromBase64String(self);
    }

    internal static long GetGalaxy(this string self)
    {
        return Convert.ToInt64(self.Substring(6, 2), 16);
    }

    internal static string GetGlyphsString(this string self)
    {
        return self.Remove(6, 2).Substring(2);
    }

    internal static string GetResource(this string self)
    {
        return self.Split('/').Last().Split('.').First();
    }

    internal static string CoerceValidFileName(this string self)
    {
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format("[{0}]+", invalidChars);

        return Regex.Replace(self, invalidRegStr, "_");
    }

    internal static string Remove(this string self, params string[] values)
    {
        foreach (var value in values)
        {
            self = self.Replace(value, string.Empty);
        }
        return self;
    }

    internal static string ToAlphaNumericString(this string self)
    {
        return string.Concat(self.Where(i => char.IsLetterOrDigit(i) || ADDITIONAL_CHAR.Contains(i))).ToUpperInvariant();
    }
}
