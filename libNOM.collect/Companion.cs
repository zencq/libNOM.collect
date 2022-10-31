using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class CompanionCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".pet", ".cmp" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Goatfungus, FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public CompanionCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Companion(json, index);
        _collection[result.Tag] = result;
        return true;
    }

    public override CollectionItem? GetOrAdd(JObject json, int index)
    {
        var key = GetTag(json, index);
        if (_collection.ContainsKey(key))
        {
            var collect = _collection[key];
            collect.Link(json, index);
            return collect;
        }

        var result = new Companion(json, index);
        _collection.TryAdd(key, result);
        return result;
    }

    #endregion

    #region Getter

    protected override string GetTag(JObject json, int index)
    {
        // Prepare.
        var useMapping = json.UseMapping();

        // Create Dictionary.
        var data = new Dictionary<string, JToken?>
        {
            { "Pet", json.SelectToken(useMapping ? $"PlayerStateData.Pets[{index}]" : $"6f=.Mcl[{index}]") },
            { "AccessoryCustomisation", json.SelectToken(useMapping ? $"PlayerStateData.PetAccessoryCustomisation[{index}]" : $"6f=.j30[{index}]") },
        };

        // Create tag.
        return Companion.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessGoatfungus(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Companion
        {
            Data = new()
            {
                { "Pet", jObject },
                { "AccessoryCustomisation", null },
            },
        };
    }

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Companion
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Pet", jObject.SelectToken("Companion") },
                { "AccessoryCustomisation", jObject.SelectToken("Accessories") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Companion
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Pet", jObject.SelectToken("Data.Pet") },
                { "AccessoryCustomisation", jObject.SelectToken("Data.AccessoryCustomisation") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Companion supports up to 6 previews/thumbnails.
/// </summary>
public class Companion : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.Pets[{_index}]";

            return $"6f=.Mcl[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Pet"]?.GetValue<string>("fH8", "CustomName");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Id
        builder.Append(data["Pet"]!.GetValue<string>("XID", "CreatureID"));
        // Descriptors
        builder.Append(data["Pet"]!.GetValue<JArray>("osl", "Descriptors")!.ConcatOrderedValues());
        // Seed
        builder.Append(data["Pet"]!.GetValue<string>("WTp[1]", "CreatureSeed[1]"));
        builder.Append(data["Pet"]!.GetValue<string>("1p=[1]", "CreatureSecondarySeed[1]"));
        builder.Append(data["Pet"]!.GetValue<string>("m9o[1]", "SpeciesSeed[1]"));
        builder.Append(data["Pet"]!.GetValue<string>("JrL[1]", "GenusSeed[1]"));
        // Traits
        builder.Append(string.Concat(data["Pet"]!.GetValue<JArray>("JAy", "Traits")!.ConcatSignedValues()));

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        if (_useMapping)
        {
            Data = new()
            {
                { "Pet", json.SelectDeepClonedToken(JsonPath) },
                { "AccessoryCustomisation", json.SelectDeepClonedToken($"PlayerStateData.PetAccessoryCustomisation[{_index}]") },
            };
        }
        else
        {
            Data = new()
            {
                { "Pet", json.SelectDeepClonedToken(JsonPath) },
                { "AccessoryCustomisation", json.SelectDeepClonedToken($"6f=.j30[{_index}]") },
            };
        }
    }

    #endregion

    // //

    #region Constructor

    public Companion() : base() { }

    public Companion(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!CompanionCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportGoatfungus()
    {
        // Prepare
        Deobfuscate();
        var result = Data["Pet"];

        // Return
        return result.Serialize().GetBytes();
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var jsonAddress = Data["Pet"]!.SelectToken("5L6")!;
        var universeAddress = jsonAddress.Type == JTokenType.String ? jsonAddress.Value<string>()! : jsonAddress.Value<long>().ToString("X");
        var result = new Dictionary<string, object?>
        {
            { "Companion",  Data["Pet"] },
            { "Accessories", Data["AccessoryCustomisation"] },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { "GalacticAddress", universeAddress },
            { "Galaxy", universeAddress.GetGalaxy() },
            { "GlyphsString", universeAddress.GetGlyphsString() },
            { "Thumbnail", Preview },
        };

        // Return
        return result.Serialize().GetBytes();
    }

    #endregion

    #region Import

    public override void Import(JObject json, int index)
    {
        _useMapping = json.UseMapping();

        // Adapt mapping in data to match with the JSON object.
        AdaptMapping(Data, _useMapping);

        if (Data.TryGetValue("Pet", out var pet) && pet is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["Pets"]![index] = pet;
            }
            else
            {

                json["6f="]!["Mcl"]![index] = pet;
            }
        }
        if (Data.TryGetValue("AccessoryCustomisation", out var accessoryCustomisation) && accessoryCustomisation is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["PetAccessoryCustomisation"]![index] = accessoryCustomisation;
            }
            else
            {
                json["6f="]!["j30"]![index] = accessoryCustomisation;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!CompanionCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        if (format == FormatEnum.Goatfungus)
            return ".pet";

        return ".cmp";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Id
            // Descriptors
            // Seed
            // Traits
            var creatureId = Data["Pet"]!.GetValue<string>("4In", "CreatureID");
            var descriptors = string.Join("_", Data["Pet"]!.GetValue<JArray>("osl", "Descriptors")!.Select(d => d.Value<string>()));
            var seed = Data["Pet"]!.GetValue<string>("WTp[1]", "CreatureSeed[1]");

            return $"{creatureId}_{descriptors}-{seed}".Remove("^");
        }
        return Name;
    }
}
