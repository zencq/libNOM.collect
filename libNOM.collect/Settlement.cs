using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class SettlementCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".stl" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Goatfungus, FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public SettlementCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Settlement(json, index);
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

        var result = new Settlement(json, index);
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
            { "Settlement", json.SelectToken(useMapping ? $"PlayerStateData.SettlementStatesV2[{index}]" : $"6f=.GQA[{index}]") },
        };

        // Create tag.
        return Settlement.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Settlement
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Settlement", jObject.SelectToken("Settlement.Settlement") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Settlement
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Settlement", jObject.SelectToken("Data.Settlement") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Companion supports up to 6 previews/thumbnails.
/// </summary>
public class Settlement : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.SettlementStatesV2[{_index}]";

            return $"6f=.GQA[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Settlement"]?.GetValue<string>("NKm", "Name");
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

        // UniverseAddress
        builder.Append(data["Settlement"]!.GetValue<string>("yhJ", "UniverseAddress"));
        // Seed
        builder.Append(data["Settlement"]!.GetValue<string>("qK9", "SeedValue"));

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Settlement", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public Settlement() : base() { }

    public Settlement(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!SettlementCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "Settlement", new Dictionary<string, JToken?>
                {
                    { "Settlement", Data["Settlement"] },
                    { "Index", _index },
                }
            },
            { nameof(Description), Description },
            { "FileVersion", 1 },
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

        if (Data.TryGetValue("Settlement", out var settlement) && settlement is not null)
        {
            // Store previous values to keep position and owner.
            var universeAddress = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.SettlementStatesV2[{index}].UniverseAddress" : $"6f=.GQA[{index}].yhJ");
            var position = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.SettlementStatesV2[{index}].Position" : $"6f=.GQA[{index}].wMC");
            var owner = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.SettlementStatesV2[{index}].Owner" : $"6f=.GQA[{index}].3?K");

            if (_useMapping)
            {
                json["PlayerStateData"]!["SettlementStatesV2"]![index] = settlement;

                json["PlayerStateData"]!["SettlementStatesV2"]![index]!["UniverseAddress"] = universeAddress;
                json["PlayerStateData"]!["SettlementStatesV2"]![index]!["Position"] = position;
                json["PlayerStateData"]!["SettlementStatesV2"]![index]!["Owner"] = owner;
            }
            else
            {
                json["6f="]!["GQA"]![index] = settlement;

                json["6f="]!["GQA"]![index]!["yhJ"] = universeAddress;
                json["6f="]!["GQA"]![index]!["wMC"] = position;
                json["6f="]!["GQA"]![index]!["3?K"] = owner;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!SettlementCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".stl";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // UniverseAddress
            // Seed
            var universeAddress = Data["Settlement"]!.GetValue<string>("yhJ", "UniverseAddress");
            var seed = Data["Settlement"]!.GetValue<string>("qK9", "SeedValue");

            return $"{universeAddress}-{seed}";
        }
        return Name;
    }
}
