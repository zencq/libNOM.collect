using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class SquadronPilotCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".sqd" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public SquadronPilotCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new SquadronPilot(json, index);
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

        var result = new SquadronPilot(json, index);
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
            { "Pilot", json.SelectToken(useMapping ? $"BaseContext.PlayerStateData.SquadronPilots[{index}]" : $"vLc.6f=.S5O[{index}]") },
        };

        // Create tag.
        return SquadronPilot.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new SquadronPilot
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Pilot", jObject.SelectToken("Squadron") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new SquadronPilot
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Pilot", jObject.SelectToken("Data.Pilot") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// </summary>
public class SquadronPilot : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"BaseContext.PlayerStateData.SquadronPilots[{_index}]";

            return $"vLc.6f=.S5O[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get => Description;
        set => Description = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Resource (NPC)
        builder.Append(data["Pilot"]!.GetValue<string>(">r:.93M", "NPCResource.Filename")!.GetResource());
        // Seed (NPC)
        builder.Append(data["Pilot"]!.GetValue<string>(">r:.@EL[1]", "NPCResource.Seed[1]"));
        // Resource (Ship)
        builder.Append(data["Pilot"]!.GetValue<string>(":dY.93M", "ShipResource.Filename")!.GetResource());
        // Seed (Ship)
        builder.Append(data["Pilot"]!.GetValue<string>(":dY.@EL[1]", "ShipResource.Seed[1]"));

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Pilot", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public SquadronPilot() : base() { }

    public SquadronPilot(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!SquadronPilotCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "Squadron", Data["Pilot"] },
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

        if (Data.TryGetValue("Pilot", out var pilot) && pilot is not null)
        {
            if (_useMapping)
            {
                json["BaseContext"]!["PlayerStateData"]!["SquadronPilots"]![index] = pilot;
            }
            else
            {

                json["vLc"]!["6f="]!["S5O"]![index] = pilot;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!SquadronPilotCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".sqd";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Resource (NPC)
            // Seed (NPC)
            // Resource (Ship)
            // Seed (Ship)
            var alienRace = GetAlienRaceEnumFromResource(Data["Pilot"]!.GetValue<string>(">r:.93M", "NPCResource.Filename"))?.ToString() ?? "AlienRace";
            var AlienSeed = Data["Pilot"]!.GetValue<string>(">r:.@EL[1]", "NPCResource.Seed[1]");
            var shipClass = GetShipClassEnumFromResource(Data["Pilot"]!.GetValue<string>(":dY.93M", "ShipResource.Filename"))?.ToString() ?? nameof(Starship);
            var shipSeed = Data["Pilot"]!.GetValue<string>(":dY.@EL[1]", "ShipResource.Seed[1]");

            return $"{alienRace}-{AlienSeed}-{shipClass}-{shipSeed}";
        }
        return Name;
    }
}
