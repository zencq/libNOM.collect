using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class VehicleCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".exo" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public VehicleCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Vehicle(json, index);
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

        var result = new Vehicle(json, index);
        _collection.TryAdd(key, result);
        return result;
    }

    #endregion

    #region Getter

    protected override string GetTag(JObject json, int index)
    {
        // Prepare.
        var customisationIndex = Vehicle.GetCustomisationIndex(index);
        var useMapping = json.UseMapping();

        // Create Dictionary.
        var data = new Dictionary<string, JToken?>
        {
            { "Vehicle", json.SelectToken(useMapping ? $"PlayerStateData.VehicleOwnership[{index}]" : $"6f=.P;m[{index}]") },
            { "CustomisationData", json.SelectToken(useMapping ? $"PlayerStateData.CharacterCustomisationData[{customisationIndex}]" : $"6f=.l:j[{customisationIndex}]") },
            { "Type", index },
        };

        // Create tag.
        return Vehicle.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Vehicle
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Vehicle", jObject.SelectToken("Data") },
                { "CustomisationData", null },
                { "Type", jObject.SelectToken("Type")!.Value<long>() },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Vehicle
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Vehicle", jObject.SelectToken("Data.Vehicle") },
                { "CustomisationData", jObject.SelectToken("Data.CustomisationData") },
                { "Type", jObject.SelectToken("Data.Type")!.Value<long>() },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Companion supports up to 6 previews/thumbnails.
/// </summary>
public class Vehicle : CollectionItem
{
    #region Field

    protected int _customisationIndex;

    #endregion

    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.VehicleOwnership[{_index}]";

            return $"6f=.P;m[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Vehicle"]?.GetValue<string>("NKm", "Name");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? ((VehicleTypeEnum)(_index!.Value)).ToString()) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    internal VehicleTypeEnum Type => (VehicleTypeEnum)(_index ?? 0);

    #endregion

    #region Getter

    internal static int GetCustomisationIndex(int index)
    {
        return index == 0 ? 1 : index + 8;
    }

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Type
        builder.Append(data["Type"]!.Value<long>());
        // CustomisationData
        if (data.TryGetValue("CustomisationData", out var customisationData) && customisationData is not null)
        {
            var preset = data["CustomisationData"]!.GetValue<string>("VFd", "SelectedPreset");
            if (preset == "^")
            {
                // DescriptorGroups
                builder.Append(data["CustomisationData"]!.GetValue<JArray>("wnR.SMP", "CustomData.DescriptorGroups")!.ConcatOrderedValues());
                // Colours
                builder.Append(data["CustomisationData"]!.GetValue<JArray>("wnR.Aak", "CustomData.Colours")!.ConcatColours());
            }
            else
            {
                builder.Append(preset); // usually default preset
            }
        }
        else
        {
            builder.Append("^DEFAULT_VEHICLE"); // default preset
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
                { "Vehicle", json.SelectDeepClonedToken(JsonPath) },
                { "CustomisationData", json.SelectDeepClonedToken($"PlayerStateData.CharacterCustomisationData[{_customisationIndex}]") },
                { "Type", _index },
            };
        }
        else
        {
            Data = new()
            {
                { "Vehicle", json.SelectDeepClonedToken(JsonPath) },
                { "CustomisationData", json.SelectDeepClonedToken($"6f=.l:j[{_customisationIndex}]") },
                { "Type", _index },
            };
        }
    }

    #endregion

    // //

    #region Constructor

    public Vehicle() : base() { }

    public Vehicle(JObject json, int index) : base(json, index)
    {
        // Field
        _customisationIndex = GetCustomisationIndex(index);

        // Initialize again to make sure CustomisationData is properly set.
        SetData(json);
    }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!VehicleCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { nameof(Data), Data["Vehicle"] },
            { nameof(DateCreated), DateCreated },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { nameof(Starred), Starred },
            { "Thumbnail", Preview },
            { "Type", _index },
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

        if (Data.TryGetValue("Vehicle", out var vehicle) && vehicle is not null)
        {
            // Store previous values to keep position and owner.
            var location = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.VehicleOwnership[{index}].Location" : $"6f=.P;m[{index}].YTa");
            var position = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.VehicleOwnership[{index}].Position" : $"6f=.P;m[{index}].wMC");
            var direction = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.VehicleOwnership[{index}].Direction" : $"6f=.P;m[{index}].l?l");

            if (_useMapping)
            {
                json["PlayerStateData"]!["VehicleOwnership"]![index] = vehicle;

                json["PlayerStateData"]!["VehicleOwnership"]![index]!["Location"] = location;
                json["PlayerStateData"]!["VehicleOwnership"]![index]!["Position"] = position;
                json["PlayerStateData"]!["VehicleOwnership"]![index]!["Direction"] = direction;
            }
            else
            {
                json["6f="]!["P;m"]![index] = vehicle;

                json["6f="]!["P;m"]![index]!["YTa"] = location;
                json["6f="]!["P;m"]![index]!["wMC"] = position;
                json["6f="]!["P;m"]![index]!["l?l"] = direction;
            }
        }
        if (Data.TryGetValue("CustomisationData", out var customisationData) && customisationData is not null)
        {
            var customisationIndex = GetCustomisationIndex(index);
            if (_useMapping)
            {
                json["PlayerStateData"]!["CharacterCustomisationData"]![customisationIndex] = customisationData;
            }
            else
            {
                json["6f="]!["l:j"]![customisationIndex] = customisationData;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!VehicleCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".exo";
    }
}
