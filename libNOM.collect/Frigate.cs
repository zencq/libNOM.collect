using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class FrigateCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".flt" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public FrigateCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Frigate(json, index);
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

        var result = new Frigate(json, index);
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
            { "Frigate", json.SelectToken(useMapping ? $"PlayerStateData.FleetFrigates[{index}]" : $"6f=.;Du[{index}]") },
        };

        // Create tag.
        return Frigate.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Frigate
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Frigate", jObject.SelectToken("Frigate") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Frigate
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Frigate", jObject.SelectToken("Data.Frigate") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// </summary>
public class Frigate : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.FleetFrigates[{_index}]";

            return $"6f=.;Du[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Frigate"]?.GetValue<string>("fH8", "CustomName");
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

        // FrigateClass
        builder.Append(data["Frigate"]!.GetValue<string>("uw7.uw7", "FrigateClass.FrigateClass"));
        // Seed
        builder.Append(data["Frigate"]!.GetValue<string>("SLc[1]", "ResourceSeed[1]"));
        builder.Append(data["Frigate"]!.GetValue<string>("@ui[1]", "HomeSystemSeed[1]"));
        // AlienRace
        builder.Append(data["Frigate"]!.GetValue<string>("SS2.0Hi", "Race.AlienRace"));
        // TraitIDs
        builder.Append(data["Frigate"]!.GetValue<JArray>("Mjm", "TraitIDs")!.ConcatOrderedValues());

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Frigate", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public Frigate() : base() { }

    public Frigate(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!FrigateCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "Frigate", Data["Frigate"] },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { "Thumbnail", Preview?.ToBase64String() },
            { "Thumbnail2", Preview2?.ToBase64String() },
            { "Thumbnail3", Preview3?.ToBase64String() },
            { "Thumbnail4", Preview4?.ToBase64String() },
            { "Thumbnail5", Preview5?.ToBase64String() },
            { "Thumbnail6", Preview6?.ToBase64String() },
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

        if (Data.TryGetValue("Frigate", out var frigate) && frigate is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["FleetFrigates"]![index] = frigate;
            }
            else
            {

                json["6f="]![";Du"]![index] = frigate;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!FrigateCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".flt";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // FrigateClass
            // Seed
            // AlienRace
            // TraitIDs
            var frigateClass = Data["Frigate"]!.GetValue<string>("uw7.uw7", "FrigateClass.FrigateClass");
            var seed = Data["Frigate"]!.GetValue<string>("SLc[1]", "ResourceSeed[1]");
            var homeSystem = Data["Frigate"]!.GetValue<string>("@ui[1]", "HomeSystemSeed[1]");

            return $"{frigateClass}-{seed}-{homeSystem}";
        }
        return Name;
    }
}
