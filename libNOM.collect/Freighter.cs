using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class FreighterCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".frt" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public FreighterCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Freighter(json, index);
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

        var result = new Freighter(json, index);
        _collection.TryAdd(key, result);
        return result;
    }

    #endregion

    #region Getter

    protected override string GetTag(JObject json, int index)
    {
        // Prepare.
        var customisationIndex = Freighter.GetCustomisationIndex();
        var useMapping = json.UseMapping();

        // Create Dictionary.
        var data = new Dictionary<string, JToken?>
        {
            { "HomeSystem", json.SelectToken(useMapping ? $"PlayerStateData.CurrentFreighterHomeSystemSeed" : $"6f=.kYq") },
            { "Freighter", json.SelectToken(useMapping ? $"PlayerStateData.CurrentFreighter" : $"6f=.bIR") },
            { "Inventory", json.SelectToken(useMapping ? $"PlayerStateData.FreighterInventory" : $"6f=.8ZP") },
            { "Inventory_TechOnly", json.SelectToken(useMapping ? $"PlayerStateData.FreighterInventory_TechOnly" : $"6f=.0wS") },
            { "Inventory_Cargo", json.SelectToken(useMapping ? $"PlayerStateData.FreighterInventory_Cargo" : $"6f=.FdP") },
            { "Name", json.SelectToken(useMapping ? $"PlayerStateData.PlayerFreighterName" : $"6f=.vxi") },
            { "Colours", json.SelectToken(useMapping ? $"PlayerStateData.CharacterCustomisationData[{customisationIndex}].CustomData.Colours" : $"6f=.l:j[{customisationIndex}].wnR.Aak") },
        };

        // Create tag.
        return Freighter.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Freighter
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "HomeSystem", jObject.SelectToken("Freighter.kYq") },
                { "Freighter", jObject.SelectToken("Freighter.bIR") },
                { "Inventory", jObject.SelectToken("Freighter.8ZP") },
                { "Inventory_TechOnly", jObject.SelectToken("Freighter.0wS") },
                { "Inventory_Cargo", null },
                { "Name", jObject.SelectToken("Freighter.vxi") },
                { "Colours", null },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Freighter
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "HomeSystem", jObject.SelectToken("Data.HomeSystem") },
                { "Freighter", jObject.SelectToken("Data.Freighter") },
                { "Inventory", jObject.SelectToken("Data.Inventory") },
                { "Inventory_TechOnly", jObject.SelectToken("Data.Inventory_TechOnly") },
                { "Inventory_Cargo", jObject.SelectToken("Data.Inventory_Cargo") },
                { "Name", jObject.SelectToken("Data.Name") },
                { "Colours", jObject.SelectToken("Data.Colours") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Companion supports up to 6 previews/thumbnails.
/// </summary>
public class Freighter : CollectionItem
{
    #region Field

    protected int _customisationIndex;

    #endregion

    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            // Spread over multiple entries and not under a separate object and therefore the first in that area.
            if (_useMapping)
                return $"PlayerStateData.CurrentFreighter";

            return $"6f=.bIR";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Name"]?.Value<string>();
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static int GetCustomisationIndex()
    {
        return 15;
    }

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Resource
        builder.Append(data["Freighter"]!.GetValue<string>("93M", "Filename")!.GetResource());
        // Seed
        builder.Append(data["Freighter"]!.GetValue<string>("@EL[1]", "Seed[1]"));
        builder.Append(data["HomeSystem"]!.GetValue<string>("[1]", "[1]"));
        // Inventory Size
        builder.Append(data["Inventory"]!.GetValue<JArray>("hl?", "ValidSlotIndices")!.GetCount());
        builder.Append(data["Inventory_TechOnly"]!.GetValue<JArray>("hl?", "ValidSlotIndices")!.GetCount());
        builder.Append(data["Inventory_Cargo"]?.GetValue<JArray>("hl?", "ValidSlotIndices")?.GetCount() ?? "000");
        // Stats
        builder.Append(data["Inventory"]!.GetValue<JArray>("@bB", "BaseStatValues")!.ConcatBaseStatValues());
        // Colours
        if (data["Colours"] is JArray colours)
        {
            builder.Append(colours.ConcatColours());
        }

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
                { "HomeSystem", json.SelectDeepClonedToken(JsonPath) },
                { "Freighter", json.SelectDeepClonedToken($"PlayerStateData.CurrentFreighter") },
                { "Inventory", json.SelectDeepClonedToken($"PlayerStateData.FreighterInventory") },
                { "Inventory_TechOnly", json.SelectDeepClonedToken($"PlayerStateData.FreighterInventory_TechOnly") },
                { "Inventory_Cargo", json.SelectDeepClonedToken($"PlayerStateData.FreighterInventory_Cargo") },
                { "Name", json.SelectDeepClonedToken($"PlayerStateData.PlayerFreighterName") },
                { "Colours", json.SelectDeepClonedToken($"PlayerStateData.CharacterCustomisationData[{_customisationIndex}].CustomData.Colours") },
            };
        }
        else
        {
            Data = new()
            {
                { "HomeSystem", json.SelectDeepClonedToken(JsonPath) },
                { "Freighter", json.SelectDeepClonedToken($"6f=.bIR") },
                { "Inventory", json.SelectDeepClonedToken($"6f=.8ZP") },
                { "Inventory_TechOnly", json.SelectDeepClonedToken($"6f=.0wS") },
                { "Inventory_Cargo", json.SelectDeepClonedToken($"6f=.FdP") },
                { "Name", json.SelectDeepClonedToken($"6f=.vxi") },
                { "Colours", json.SelectDeepClonedToken($"6f=.l:j[{_customisationIndex}].wnR.Aak") },
            };
        }
    }

    #endregion

    // //

    #region Constructor

    public Freighter() : base() { }

    public Freighter(JObject json, int index) : base(json, index)
    {
        // Field
        _customisationIndex = GetCustomisationIndex();

        // Initialize again to make sure CustomisationData is properly set.
        SetData(json);
    }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!FreighterCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "Freighter", new Dictionary<string, JToken?>
                {
                    // Keys are not shortened but otherwise the same as in the Data dictionary but obfuscated.
                    { "kYq", Data["HomeSystem"] },
                    { "bIR", Data["Freighter"] },
                    { "8ZP", Data["Inventory"] },
                    { "0wS", Data["Inventory_TechOnly"] },
                    { "vxi", Data["Name"] },
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

        if (Data.TryGetValue("HomeSystem", out var homeSystem) && homeSystem is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["CurrentFreighterHomeSystemSeed"] = homeSystem;
            }
            else
            {

                json["6f="]!["kYq"] = homeSystem;
            }
        }
        if (Data.TryGetValue("Freighter", out var freighter) && freighter is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["CurrentFreighter"] = freighter;
            }
            else
            {
                json["6f="]!["bIR"] = freighter;
            }
        }
        if (Data.TryGetValue("Inventory", out var inventory) && inventory is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["FreighterInventory"] = inventory;
            }
            else
            {
                json["6f="]!["8ZP"] = inventory;
            }
        }
        if (Data.TryGetValue("Inventory_TechOnly", out var inventoryTechOnly) && inventoryTechOnly is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["FreighterInventory_TechOnly"] = inventoryTechOnly;
            }
            else
            {
                json["6f="]!["0wS"] = inventoryTechOnly;
            }
        }
        if (Data.TryGetValue("Inventory_Cargo", out var inventoryCargo) && inventoryCargo is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["FreighterInventory_Cargo"] = inventoryCargo;
            }
            else
            {
                json["6f="]!["FdP"] = inventoryCargo;
            }
        }
        if (Data.TryGetValue("Name", out var name) && name is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["PlayerFreighterName"] = name;
            }
            else
            {
                json["6f="]!["vxi"] = name;
            }
        }
        if (Data.TryGetValue("Colours", out var colours) && colours is not null)
        {
            var customisationIndex = GetCustomisationIndex();
            if (_useMapping)
            {
                json["PlayerStateData"]!["CharacterCustomisationData"]![customisationIndex]!["CustomData"]!["Colours"] = colours;
            }
            else
            {
                json["6f="]!["l:j"]![customisationIndex]!["wnR"]!["Aak"] = colours;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!FreighterCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".frt";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Resource
            // Seed
            // Inventory Size
            // Stats
            // Colours
            var freighterType = GetFreighterFromResource(Data["Freighter"]!.GetValue<string>("93M", "Filename"));
            var seed = Data["Freighter"]!.GetValue<string>("@EL[1]", "Seed[1]");
            var homeSystem = Data["HomeSystem"]!.GetValue<string>("[1]", "[1]");
            var validSlotsGeneral = Data["Inventory"]!.GetValue<JArray>("hl?", "ValidSlotIndices")!.GetCount();
            var validSlotsTech = Data["Inventory_TechOnly"]!.GetValue<JArray>("hl?", "ValidSlotIndices")!.GetCount();
            var validSlotsCargo = Data["Inventory_Cargo"]?.GetValue<JArray>("hl?", "ValidSlotIndices")?.GetCount() ?? "000";

            return $"{freighterType}-{seed}-{homeSystem}-{validSlotsGeneral}-{validSlotsTech}-{validSlotsCargo}";
        }
        return Name;
    }
}
