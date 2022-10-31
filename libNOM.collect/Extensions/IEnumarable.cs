using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect.Extensions;


internal static class IEnumarableExtensions
{
    internal static string ConcatBaseStatValues(this JArray self)
    {
        return string.Concat(self.Select(i => i.GetValue<double>(">MX", "Value")));
    }

    internal static string ConcatColours(this JArray self)
    {
        var builder = new StringBuilder();

        foreach (var colour in self)
        {
            var colourAlt = colour.GetValue<string>("RVl.Ty=", "Palette.ColourAlt")!;
            if (colourAlt == "Primary")
            {
                builder.Append(colour.GetValue<string>("RVl.RVl", "Palette.Palette")!.Split('_').Last());
            }
            else
            {
                builder.Append($"{colourAlt.First()}{colourAlt.Last()}");
            }
            var r = (int)(255 * colour.GetValue<double>("xEg[0]", "Colour[0]"));
            var g = (int)(255 * colour.GetValue<double>("xEg[1]", "Colour[1]"));
            var b = (int)(255 * colour.GetValue<double>("xEg[2]", "Colour[2]"));

            builder.Append($"{r:X2}{g:X2}{b:X2}");
        }

        return builder.ToString();
    }

    internal static string ConcatOrderedValues(this JArray self)
    {
        return string.Concat(self.OrderBy(i => i));
    }

    internal static string ConcatSignedValues(this JArray self)
    {
        return string.Concat(self.Select(i => i.Value<double>().ToString("-0")));
    }

    internal static string GetCount(this JArray self)
    {
        return self.Count.ToString("D3");
    }

    internal static string ToBase64String(this byte[] self)
    {
        return Convert.ToBase64String(self);
    }
}
