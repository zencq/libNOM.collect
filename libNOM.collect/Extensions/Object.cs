using Newtonsoft.Json;

namespace libNOM.collect.Extensions;


internal static class ObjectExtensions
{
    internal static string Serialize(this object? self)
    {
        return JsonConvert.SerializeObject(self);
    }
}
