using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class WeaponCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".wp0", ".mlt" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Goatfungus, FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public WeaponCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Weapon(json, index);
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

        var result = new Weapon(json, index);
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
            { "Multitool", json.SelectToken(useMapping ? $"PlayerStateData.Multitools[{index}]" : $"6f=.SuJ[{index}]") },
            { "Type", null },
        };

        // Create tag.
        return Weapon.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessGoatfungus(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Weapon
        {
            Data = new()
            {
                { "Multitool", jObject },
                { "Type", null },
            },
        };
    }

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Weapon
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Multitool", jObject.SelectToken("MultiTool") },
                { "Type", null },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Weapon
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Multitool", jObject.SelectToken("Data.Multitool") },
                { "Type", jObject.SelectToken("Data.Type") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Companion supports up to 6 previews/thumbnails.
/// </summary>
public class Weapon : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.Multitools[{_index}]";

            return $"6f=.SuJ[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Multitool"]?.GetValue<string>("NKm", "Name");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    public WeaponTypeEnum? Type
    {
        get => Data["Type"] is null ? null : (WeaponTypeEnum)(Data["Type"]!.Value<int>());
        set => Data["Type"] = value is null ? null : (int)(value);
    }

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Resource
        builder.Append(data["Multitool"]!.GetValue<string>("NTx.93M", "Resource.Filename")?.GetResource() ?? "MULTITOOL"); // MODELS/COMMON/WEAPONS/MULTITOOL/MULTITOOL.SCENE.MBIN
        // Seed
        builder.Append(data["Multitool"]!.GetValue<string>("@EL[1]", "Seed[1]"));
        // Inventory Size
        builder.Append(data["Multitool"]!.GetValue<JArray>("OsQ.hl?", "Store.ValidSlotIndices")!.GetCount());
        // Stats
        builder.Append(data["Multitool"]!.GetValue<JArray>("OsQ.@bB", "Store.BaseStatValues")!.ConcatBaseStatValues());

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Multitool", json.SelectDeepClonedToken(JsonPath) },
            { "Type", Data.ContainsKey("Type") ? Data["Type"] : null },
        };
    }

    #endregion

    // //

    #region Constructor

    public Weapon() : base() { }

    public Weapon(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!WeaponCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportGoatfungus()
    {
        // Prepare
        Deobfuscate();
        var result = Data["Multitool"];

        // Return
        return result.Serialize().GetBytes();
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "MultiTool", Data["Multitool"] },
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

        if (Data.TryGetValue("Multitool", out var multitool) && multitool is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["Multitools"]![index] = multitool;
            }
            else
            {

                json["6f="]!["SuJ"]![index] = multitool;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!WeaponCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        if (format == FormatEnum.Goatfungus)
            return ".wp0";

        return ".mlt";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Resource
            // Seed
            // Inventory Size
            // Stats
            var weaponClass = Type?.ToString() ?? GetWeaponClassEnumFromResource(Data["Multitool"]!.GetValue<string>("NTx.93M", "Resource.Filename"))?.ToString() ?? nameof(Weapon);
            var seed = Data["Multitool"]!.GetValue<string>("@EL[1]", "Seed[1]");
            var validSlots = Data["Multitool"]!.GetValue<JArray>("OsQ.hl?", "Store.ValidSlotIndices")!.GetCount();
            var inventoryClass = Data["Multitool"]?.GetValue<string>("OsQ.B@N.1o6", "Store.Class.InventoryClass");

            return $"{weaponClass}-{inventoryClass}-{seed}-{validSlots}";
        }
        return Name;
    }
}
