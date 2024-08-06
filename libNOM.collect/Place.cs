using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class PlaceCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".plc" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public PlaceCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Place(json, index);
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

        var result = new Place(json, index);
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
            { "UniverseAddress", json.SelectToken(useMapping ? "BaseContext.PlayerStateData.UniverseAddress" : "vLc.6f=.yhJ") },
            { "PlayerPosition", json.SelectToken(useMapping ? "SpawnStateData.PlayerPositionInSystem" : "rnc.mEH") },
            { "PlayerTransform", json.SelectToken(useMapping ? "SpawnStateData.PlayerTransformAt" : "rnc.l2U") },
            { "ShipPosition", json.SelectToken(useMapping ? "SpawnStateData.ShipPositionInSystem" : "rnc.tnP") },
            { "ShipTransform", json.SelectToken(useMapping ? "SpawnStateData.ShipTransformAt" : "rnc.l4H") },
            { "LastKnownPlayerState", json.SelectToken(useMapping ? "SpawnStateData.LastKnownPlayerState" : "rnc.jk4") },
            { "Type", 0 },
        };

        // Create tag.
        return Place.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Place
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "UniverseAddress", jObject.SelectToken("Data.yhJ") },
                { "PlayerPosition", jObject.SelectToken("Data.mEH") },
                { "PlayerTransform", jObject.SelectToken("Data.l2U") },
                { "ShipPosition", jObject.SelectToken("Data.tnP") },
                { "ShipTransform", jObject.SelectToken("Data.l4H") },
                { "LastKnownPlayerState", jObject.SelectToken("Data.jk4") },
                { "Type", jObject.SelectToken("Type") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Place
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "UniverseAddress", jObject.SelectToken("Data.UniverseAddress") },
                { "PlayerPosition", jObject.SelectToken("Data.PlayerPosition") },
                { "PlayerTransform", jObject.SelectToken("Data.PlayerTransform") },
                { "ShipPosition", jObject.SelectToken("Data.ShipPosition") },
                { "ShipTransform", jObject.SelectToken("Data.ShipTransform") },
                { "LastKnownPlayerState", jObject.SelectToken("Data.LastKnownPlayerState") },
                { "Type", jObject.SelectToken("Data.Type") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// </summary>
public class Place : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"BaseContext.PlayerStateData.UniverseAddress";

            return $"vLc.6f=.yhJ";
        }
    }

    public override string Name // { get; set; }
    {
        get => Description;
        set => Description = value;
    }

    public override string Tag => GetTag(Data);

    public PlaceTypeEnum Type
    {
        get => (PlaceTypeEnum)(Data["Type"]!.Value<int>()!);
        set => Data["Type"] = (int)(value);
    }

    public int RealityIndex => Data["UniverseAddress"]!.GetValue<int>("Iis", "RealityIndex");

    public int VoxelX => Data["UniverseAddress"]!.GetValue<int>("oZw.dZj", "GalacticAddress.VoxelX");

    public int VoxelY => Data["UniverseAddress"]!.GetValue<int>("oZw.IyE", "GalacticAddress.VoxelY");

    public int VoxelZ => Data["UniverseAddress"]!.GetValue<int>("oZw.uXE", "GalacticAddress.VoxelZ");

    public int SolarSystemIndex => Data["UniverseAddress"]!.GetValue<int>("oZw.vby", "GalacticAddress.SolarSystemIndex");

    public int PlanetIndex => Data["UniverseAddress"]!.GetValue<int>("oZw.jsv", "GalacticAddress.PlanetIndex");

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // UniverseAddress
        builder.Append("R");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("Iis", "RealityIndex"));
        builder.Append("X");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("oZw.dZj", "GalacticAddress.VoxelX"));
        builder.Append("Y");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("oZw.IyE", "GalacticAddress.VoxelY"));
        builder.Append("Z");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("oZw.uXE", "GalacticAddress.VoxelZ"));
        builder.Append("S");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("oZw.vby", "GalacticAddress.SolarSystemIndex"));
        builder.Append("P");
        builder.Append(data["UniverseAddress"]!.GetValue<long>("oZw.jsv", "GalacticAddress.PlanetIndex"));
        // PlayerPositionInSystem
        builder.Append((data["PlayerPosition"] as JArray)!.ConcatSignedValues());
        // PlayerTransformAt
        builder.Append((data["PlayerTransform"] as JArray)!.ConcatSignedValues());

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
                { "UniverseAddress", json.SelectDeepClonedToken(JsonPath) },
                { "PlayerPosition", json.SelectDeepClonedToken($"SpawnStateData.PlayerPositionInSystem") },
                { "PlayerTransform", json.SelectDeepClonedToken($"SpawnStateData.PlayerTransformAt") },
                { "ShipPosition", json.SelectDeepClonedToken($"SpawnStateData.ShipPositionInSystem") },
                { "ShipTransform", json.SelectDeepClonedToken($"SpawnStateData.ShipTransformAt") },
                { "LastKnownPlayerState", json.SelectDeepClonedToken($"SpawnStateData.LastKnownPlayerState") },
                { "Type", Data.ContainsKey("Type") ? Data["Type"] : null },
            };
        }
        else
        {
            Data = new()
            {
                { "UniverseAddress", json.SelectDeepClonedToken(JsonPath) },
                { "PlayerPosition", json.SelectDeepClonedToken($"rnc.mEH") },
                { "PlayerTransform", json.SelectDeepClonedToken($"rnc.l2U") },
                { "ShipPosition", json.SelectDeepClonedToken($"rnc.tnP") },
                { "ShipTransform", json.SelectDeepClonedToken($"rnc.l4H") },
                { "LastKnownPlayerState", json.SelectDeepClonedToken($"rnc.jk4") },
                { "Type", Data.ContainsKey("Type") ? Data["Type"] : null },
            };
        }
    }

    #endregion

    // //

    #region Constructor

    public Place() : base() { }

    public Place(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!PlaceCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var universeAddress = (Data["UniverseAddress"] as JObject)!.GetUniverseAddress();
        var result = new Dictionary<string, object?>
        {
            { nameof(Data), new Dictionary<string, JToken?>
                {
                    // Keys are not shortened but otherwise the same as in the Data dictionary but obfuscated.
                    { "yhJ", Data["UniverseAddress"] },
                    { "mEH", Data["PlayerPosition"] },
                    { "l2U", Data["PlayerTransform"] },
                    { "tnP", Data["ShipPosition"] },
                    { "l4H", Data["ShipTransform"] },
                    { "jk4", Data["LastKnownPlayerState"] },
                }
            },
            { nameof(DateCreated), DateCreated },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { "GalacticAddress", universeAddress },
            { "Galaxy", universeAddress.GetGalaxy() },
            { "GlyphsString", universeAddress.GetGlyphsString() },
            { nameof(Starred), Starred },
            { "Thumbnail", Preview?.ToBase64String() },
            { "Thumbnail2", Preview2?.ToBase64String() },
            { "Thumbnail3", Preview3?.ToBase64String() },
            { "Thumbnail4", Preview4?.ToBase64String() },
            { "Thumbnail5", Preview5?.ToBase64String() },
            { "Thumbnail6", Preview6?.ToBase64String() },
            { "Type", Data["Type"] },
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

        if (Data.TryGetValue("UniverseAddress", out var universeAddress) && universeAddress is not null)
        {
            if (_useMapping)
            {
                json["BaseContext"]!["PlayerStateData"]!["UniverseAddress"] = universeAddress;
            }
            else
            {

                json["vLc"]!["6f="]!["yhJ"] = universeAddress;
            }
        }
        if (Data.TryGetValue("PlayerPosition", out var playerPosition) && playerPosition is not null)
        {
            if (_useMapping)
            {
                json["SpawnStateData"]!["PlayerPositionInSystem"] = playerPosition;
            }
            else
            {

                json["rnc"]!["mEH"] = playerPosition;
            }
        }
        if (Data.TryGetValue("PlayerTransform", out var playerTransform) && playerTransform is not null)
        {
            if (_useMapping)
            {
                json["SpawnStateData"]!["PlayerTransformAt"] = playerTransform;
            }
            else
            {

                json["rnc"]!["l2U"] = playerTransform;
            }
        }
        if (Data.TryGetValue("ShipPosition", out var shipPosition) && shipPosition is not null)
        {
            if (_useMapping)
            {
                json["SpawnStateData"]!["ShipPositionInSystem"] = shipPosition;
            }
            else
            {

                json["rnc"]!["tnP"] = shipPosition;
            }
        }
        if (Data.TryGetValue("ShipTransform", out var shipTransform) && shipTransform is not null)
        {
            if (_useMapping)
            {
                json["SpawnStateData"]!["ShipTransformAt"] = shipTransform;
            }
            else
            {

                json["rnc"]!["l4H"] = shipTransform;
            }
        }
        if (Data.TryGetValue("LastKnownPlayerState", out var lastKnownPlayerState) && lastKnownPlayerState is not null)
        {
            if (_useMapping)
            {
                json["SpawnStateData"]!["LastKnownPlayerState"] = lastKnownPlayerState;
            }
            else
            {

                json["rnc"]!["jk4"] = lastKnownPlayerState;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!PlaceCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".plc";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // UniverseAddress
            // PlayerPositionInSystem
            // PlayerTransformAt
            var universeAddress = (Data["UniverseAddress"] as JObject)!.GetUniverseAddress();
            var position = (Data["PlayerPosition"] as JArray)!.ConcatSignedValues();

            return $"{universeAddress}{position}";
        }
        return Name;
    }
}
