using Newtonsoft.Json.Linq;

namespace libNOM.collect.Extensions;


internal static class NewtonsoftExtensions
{
    internal static string GetUniverseAddress(this JObject self)
    {
        if (self.TryGetValue("Iis", out var jsonRealityIndex) && self.TryGetValue("oZw", out var jsonGalacticAddress))
        {
            var realityIndex = jsonRealityIndex.Value<int>();
            var voxelX = ConvertVoxelForAddress(jsonGalacticAddress["dZj"]!.Value<int>(), 3);
            var voxelY = ConvertVoxelForAddress(jsonGalacticAddress["IyE"]!.Value<int>(), 2);
            var voxelZ = ConvertVoxelForAddress(jsonGalacticAddress["uXE"]!.Value<int>(), 3);
            var solarSystemIndex = jsonGalacticAddress["vby"]!.Value<int>();
            var planetIndex = jsonGalacticAddress["jsv"]!.Value<int>();

            return $"0x{planetIndex:X1}{solarSystemIndex:X3}{realityIndex:X2}{voxelY:X2}{voxelZ:X3}{voxelX:X3}";
        }
        return string.Empty;
    }

    private static int ConvertVoxelForAddress(int value, int byteLength)
    {
        var signValue = (int)(Math.Pow(16, byteLength));

        var result = value % signValue;
        return result < 0 ? result + signValue : result;
    }

    internal static IEnumerable<JToken> GetTokens(this JToken self, string pathObfuscated, string pathDeobfuscated)
    {
        // Obfuscated is more likely and therefore tried first.
        var tokens = self.SelectTokens(pathObfuscated);
        if (tokens.Any())
            return tokens;

        return self.SelectTokens(pathDeobfuscated);
    }

    internal static T? GetValue<T>(this JToken self, string pathObfuscated, string pathDeobfuscated)
    {
        // Obfuscated is more likely and therefore tried first.
        var token = self.SelectToken(pathObfuscated) ?? self.SelectToken(pathDeobfuscated);
        if (token is null)
            return default;

        return token.Value<T>();
    }

    internal static JToken? SelectDeepClonedToken(this JToken self, string path)
    {
        return self.SelectToken(path)?.DeepClone();
    }

    /// <summary>
    /// Returns whether the specified object is deobfuscated. Needs to be the root object.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    internal static bool UseMapping(this JObject self)
    {
        return self.ContainsKey("Version");
    }
}
