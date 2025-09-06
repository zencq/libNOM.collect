using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class StarshipCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".sh0", ".shp" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Goatfungus, FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public StarshipCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Starship(json, index);
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

        var result = new Starship(json, index);
        _collection.TryAdd(key, result);
        return result;
    }

    #endregion

    #region Getter

    protected override string GetTag(JObject json, int index)
    {
        // Prepare.
        var customisationIndex = Starship.GetCustomisationIndex(index);
        var useMapping = json.UseMapping();

        // Create Dictionary.
        var data = new Dictionary<string, JToken?>
        {
            { "Ship", json.SelectToken(useMapping ? $"BaseContext.PlayerStateData.ShipOwnership[{index}]" : $"vLc.6f=.@Cs[{index}]") },
            { "UseLegacyColours", json.SelectToken(useMapping ? $"BaseContext.PlayerStateData.ShipUsesLegacyColours[{index}]" : $"vLc.6f=.4hl[{index}]") },
            { "Colours", json.SelectDeepClonedToken(useMapping ? $"BaseContext.PlayerStateData.CharacterCustomisationData[{customisationIndex}].CustomData.Colours" : $"vLc.6f=.l:j[{customisationIndex}].wnR.Aak") },
        };

        // Create tag.
        return Starship.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessGoatfungus(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Starship
        {
            Data = new()
            {
                { "Ship", jObject },
                { "UseLegacyColours", false },
                { "Colours", null },
                { "PersistentPlayerBases", null }, // TODO: added?
            },
        };
    }

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Starship
        {
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),

            Data = new()
            {
                { "Ship", jObject.SelectToken("Ship.@Cs") },
                { "UseLegacyColours", jObject.SelectToken("Ship.4hl") },
                { "Colours", jObject.SelectToken("Colours") ?? new JArray() }, // custom addition by Mjstral (MetaIdea)
                { "PersistentPlayerBases", null }, // TODO: added?
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Starship
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Ship", jObject.SelectToken("Data.Ship") },
                { "UseLegacyColours", jObject.SelectToken("Data.UseLegacyColours") },
                { "Colours", jObject.SelectToken("Data.Colours") },
                { "PersistentPlayerBases", jObject.SelectToken("Data.PersistentPlayerBases") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// </summary>
public class Starship : CollectionItem
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
                return $"BaseContext.PlayerStateData.ShipOwnership[{_index}]";

            return $"vLc.6f=.@Cs[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Ship"]?.GetValue<string>("NKm", "Name");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static int GetCustomisationIndex(int index)
    {
        return index < 6 ? index + 3 : index + 11; // three additinal ship were added with Outlaws 3.85
    }

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // Legacy Color
        builder.Append(Convert.ToInt32(data["UseLegacyColours"]!.Value<bool>()));
        // Resource
        builder.Append(data["Ship"]!.GetValue<string>("NTx.93M", "Resource.Filename")!.GetResource());
        // Seed
        builder.Append(data["Ship"]!.GetValue<string>("NTx.@EL[1]", "Resource.Seed[1]"));
        // Inventory Size
        builder.Append(data["Ship"]!.GetValue<JArray>(";l5.hl?", "Inventory.ValidSlotIndices")!.GetCount());
        builder.Append(data["Ship"]!.GetValue<JArray>("PMT.hl?", "Inventory_TechOnly.ValidSlotIndices")!.GetCount());
        builder.Append(data["Ship"]!.GetValue<JArray>("gan.hl?", "Inventory_Cargo.ValidSlotIndices")?.GetCount() ?? "000");
        // Stats
        builder.Append(data["Ship"]!.GetValue<JArray>(";l5.@bB", "Inventory.BaseStatValues")!.ConcatBaseStatValues());
        // Colours
        builder.Append((data["Colours"] as JArray)!.ConcatColours());

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
                { "Ship", json.SelectDeepClonedToken(JsonPath) },
                { "UseLegacyColours", json.SelectDeepClonedToken($"BaseContext.PlayerStateData.ShipUsesLegacyColours[{_index}]") },
                { "Colours", json.SelectDeepClonedToken($"BaseContext.PlayerStateData.CharacterCustomisationData[{_customisationIndex}].CustomData.Colours") },
                { "PersistentPlayerBases", json.SelectDeepClonedToken($"BaseContext.PlayerStateData.PersistentPlayerBases[?(@.UserData == {_index} && @.BaseType.PersistentBaseTypes == 'PlayerShipBase')]") },
            };
        }
        else
        {
            Data = new()
            {
                { "Ship", json.SelectDeepClonedToken(JsonPath) },
                { "UseLegacyColours", json.SelectDeepClonedToken($"vLc.6f=.4hl[{_index}]") },
                { "Colours", json.SelectDeepClonedToken($"vLc.6f=.l:j[{_customisationIndex}].wnR.Aak") },
                { "PersistentPlayerBases", json.SelectDeepClonedToken($"vLc.6f=.F?0[?(@.CVX == {_index} && @.peI.DPp == 'PlayerShipBase')]") },
            };
        }
    }

    #endregion

    // //

    #region Constructor

    public Starship() : base() { }

    public Starship(JObject json, int index) : base(json, index)
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
        if (!StarshipCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportGoatfungus()
    {
        // Prepare
        Deobfuscate();
        var result = Data["Ship"];

        // Return
        return result.Serialize().GetBytes();
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { "Ship", new Dictionary<string, JToken?>
                {
                    // Keys are not shortened but otherwise the same as in the Data dictionary but obfuscated.
                    { "@Cs", Data["Ship"] },
                    { "4hl", Data["UseLegacyColours"] },
                }
            },
            { "Colours", Data["Colours"] }, // custom addition by Mjstral (MetaIdea)
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

        if (Data.TryGetValue("Ship", out var ship) && ship is not null)
        {
            if (_useMapping)
            {
                json["BaseContext"]!["PlayerStateData"]!["ShipOwnership"]![index] = ship;
            }
            else
            {

                json["vLc"]!["6f="]!["@Cs"]![index] = ship;
            }
        }
        if (Data.TryGetValue("UseLegacyColours", out var useLegacyColours) && useLegacyColours is not null)
        {
            if (_useMapping)
            {
                json["BaseContext"]!["PlayerStateData"]!["ShipUsesLegacyColours"]![index] = useLegacyColours;
            }
            else
            {
                json["vLc"]!["6f="]!["4hl"]![index] = useLegacyColours;
            }
        }
        if (Data.TryGetValue("Colours", out var colours) && colours is not null)
        {
            var customisationIndex = GetCustomisationIndex(index);
            if (_useMapping)
            {
                json["BaseContext"]!["PlayerStateData"]!["CharacterCustomisationData"]![customisationIndex]!["CustomData"]!["Colours"] = colours;
            }
            else
            {
                json["vLc"]!["6f="]!["l:j"]![customisationIndex]!["wnR"]!["Aak"] = colours;
            }
        }
        if (Data.TryGetValue("PersistentPlayerBases", out var persistentPlayerBases) && persistentPlayerBases is not null)
        {
            if (_useMapping)
            {
                // set index to the one we import to
                persistentPlayerBases["UserData"] = index;

                var replacedCorvette = json.SelectToken($"BaseContext.PlayerStateData.PersistentPlayerBases[?(@.UserData == {index} && @.BaseType.PersistentBaseTypes == 'PlayerShipBase')]");                
                if (replacedCorvette != null)
                {
                    var i = int.Parse(replacedCorvette.Path.Split('[').Last().TrimEnd(']'));
                    json["BaseContext"]!["PlayerStateData"]!["PersistentPlayerBases"]![i] = persistentPlayerBases;
                }
                else
                {
                    // add base part to the save
                    var playerBases = (JArray)(json["BaseContext"]!["PlayerStateData"]!["PersistentPlayerBases"]!);
                    playerBases.Add(persistentPlayerBases);
                }
            }
            else
            {
                // set index to the one we import to
                persistentPlayerBases["4U6"] = index;

                var replacedCorvette = json.SelectToken($"vLc.6f=.F?0[?(@.4U6 == {index} && @.peI.DPp == 'PlayerShipBase')]");
                if (replacedCorvette != null)
                {
                    var i = int.Parse(replacedCorvette.Path.Split('[').Last().TrimEnd(']'));
                    json["vLc"]!["6f="]!["F?0"]![i] = persistentPlayerBases;
                }
                else
                {
                    // add base part to the save
                    var playerBases = (JArray)(json["vLc"]!["6f="]!["F?0"]!);
                    playerBases.Add(persistentPlayerBases);
                }
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!StarshipCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        if (format == FormatEnum.Goatfungus)
            return ".sh0";

        return ".shp";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Legacy Color
            // Resource
            // Seed
            // Inventory Size
            // Stats
            // Colours
            var shipClass = GetShipClassEnumFromResource(Data["Ship"]!.GetValue<string>("NTx.93M", "Resource.Filename"))?.ToString() ?? nameof(Starship);
            var seed = Data["Ship"]!.GetValue<string>("NTx.@EL[1]", "Resource.Seed[1]");
            var validSlotsGeneral = Data["Ship"]!.GetValue<JArray>(";l5.hl?", "Inventory.ValidSlotIndices")!.GetCount();
            var validSlotsTech = Data["Ship"]!.GetValue<JArray>("PMT.hl?", "Inventory_TechOnly.ValidSlotIndices")!.GetCount();
            var validSlotsCargo = Data["Ship"]!.GetValue<JArray>("gan.hl?", "Inventory_Cargo.ValidSlotIndices")?.GetCount() ?? "000";
            var inventoryClass = Data["Ship"]?.GetValue<string>(";l5.B@N.1o6", "Inventory.Class.InventoryClass");

            return $"{shipClass}-{inventoryClass}-{seed}-{validSlotsGeneral}-{validSlotsTech}-{validSlotsCargo}";
        }
        return Name;
    }
}
