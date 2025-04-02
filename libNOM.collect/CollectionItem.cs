using libNOM.map;

using Newtonsoft.Json.Linq;

namespace libNOM.collect;


public abstract class CollectionItem
{
    #region Field

    protected int? _index;
    protected string? _name;
    protected bool _useMapping = true;

    #endregion

    #region Property

    internal Dictionary<string, JToken?> Data { get; set; } = [];

    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;

    public string Description { get; set; } = string.Empty;

    internal FormatEnum? Format { get; set; }

    public bool IsCollected => Location is not null; // { get; }

    internal bool IsLinked => _index is not null; // { get; }

    public FileInfo? Location { get; set; }

    public virtual string Name
    {
        get => _name ?? Description;
        set => _name = value;
    }

    public byte[]? Preview { get; set; }

    public byte[]? Preview2 { get; set; }

    public byte[]? Preview3 { get; set; }

    public byte[]? Preview4 { get; set; }

    public byte[]? Preview5 { get; set; }

    public byte[]? Preview6 { get; set; }

    public bool Starred { get; set; } // = false;

    // //

    public abstract string JsonPath { get; }

    public virtual string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static string GetTag(Dictionary<string, JToken?> data)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Setter

    protected abstract void SetData(JObject data);

    #endregion

    // //

    #region Constructor

    public CollectionItem() { }

    public CollectionItem(JObject json, int index)
    {
        // Field
        _index = index;
        _useMapping = json.UseMapping();

        // Initialize
        SetData(json);
    }

    #endregion

    // //

    #region Export

    /// <summary>
    /// Re-export this.
    /// </summary>
    public void Export()
    {
        if (!IsCollected)
            return;

        // Create
        var content = Format switch
        {
            FormatEnum.Goatfungus => ExportGoatfungus(),
            FormatEnum.Kaii => ExportKaii(),
            FormatEnum.Standard => ExportStandard(),
            _ => null,
        };
        if (content is null)
            return;

        // Write
        Directory.CreateDirectory(Location!.Directory!.FullName);

#if NETSTANDARD2_0
        File.WriteAllBytes(Location!.FullName, content);
#else
        // File does not matter until next startup and therefore no need to wait.
        _ = File.WriteAllBytesAsync(Location!.FullName, content);
#endif
    }

    public virtual void Export(JObject json, FormatEnum format, string path)
    {
        // Update
        DateCreated = DateTime.Now;
        Format = format;
        SetData(json);

        // Create
        var content = format switch
        {
            FormatEnum.Goatfungus => ExportGoatfungus(),
            FormatEnum.Kaii => ExportKaii(),
            FormatEnum.Standard => ExportStandard(),
            _ => null,
        };
        if (content is null)
            return;

        // Write
        Directory.CreateDirectory(path);
        path = Path.Combine(path, $"{GetFilename().CoerceValidFileName()}{GetExtension(format)}"); // use specific format to be able to automatically detect in ctor

#if NETSTANDARD2_0
        File.WriteAllBytes(path, content);
#else
        // File does not matter until next startup and therefore no need to wait.
        _ = File.WriteAllBytesAsync(path, content);
#endif
    }

    protected virtual byte[] ExportGoatfungus()
    {
        throw new NotImplementedException();
    }

    protected virtual byte[] ExportKaii()
    {
        throw new NotImplementedException();
    }

    protected virtual byte[] ExportStandard()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { nameof(Data), Data },
            { nameof(DateCreated), DateCreated.ToUniversalTime() },
            { nameof(Description), Description },
            { "FileVersion", 2 },
            { nameof(Preview), Preview?.ToBase64String() },
            { nameof(Preview2), Preview2?.ToBase64String() },
            { nameof(Preview3), Preview3?.ToBase64String() },
            { nameof(Preview4), Preview4?.ToBase64String() },
            { nameof(Preview5), Preview5?.ToBase64String() },
            { nameof(Preview6), Preview6?.ToBase64String() },
            { nameof(Starred), Starred },
        };

        // Return
        return result.Serialize().GetBytes();
    }

    #endregion

    #region Import

    public abstract void Import(JObject json, int index);

    #endregion

    #region Mapping

    protected static void AdaptMapping(Dictionary<string, JToken?> data, bool useMapping)
    {
        if (useMapping)
        {
            Deobfuscate(data);
        }
        else
        {
            Obfuscate(data);
        }
    }

    protected void Deobfuscate()
    {
        Deobfuscate(Data);
    }

    protected static void Deobfuscate(Dictionary<string, JToken?> data)
    {
        foreach (var value in data.Values.Where(d => d is not null))
        {
            Mapping.Deobfuscate(value!);
        }
    }

    protected void Obfuscate()
    {
        Obfuscate(Data);
    }

    protected static void Obfuscate(Dictionary<string, JToken?> data)
    {
        foreach (var value in data.Values)
        {
            Mapping.Obfuscate(value!);
        }
    }

    #endregion

    protected abstract string? GetExtension(FormatEnum format);

    protected virtual string GetFilename()
    {
        return string.IsNullOrEmpty(Name) ? Tag : Name;
    }

    internal static AlienRaceEnum? GetAlienRaceEnumFromResource(string? resource)
    {
        return resource switch
        {
            "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN" => AlienRaceEnum.Traders,
            "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN" => AlienRaceEnum.Warriors,
            "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN" => AlienRaceEnum.Explorers,
            "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCFOURTH.SCENE.MBIN" => AlienRaceEnum.Diplomats,
            "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCFIFTH.SCENE.MBIN" => AlienRaceEnum.Exotics,
            _ => null,
        };
    }

    internal static string? GetFreighterFromResource(string? resource)
    {
        return resource switch
        {
            "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/FREIGHTER_PROC.SCENE.MBIN" => "Normal",
            "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/CAPITALFREIGHTER_PROC.SCENE.MBIN" => "Capital",
            "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/PIRATEFREIGHTER.SCENE.MBIN" => "Dreadnought",
            _ => null,
        };
    }

    internal static ShipTypeEnum? GetShipClassEnumFromResource(string? resource)
    {
        return resource switch
        {
            "MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN" => ShipTypeEnum.Dropship,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN" => ShipTypeEnum.Fighter,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERCLASSICGOLD.SCENE.MBIN" => ShipTypeEnum.FighterClassicGold,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERSPECIALSWITCH.SCENE.MBIN" => ShipTypeEnum.FighterSpecialSwitch,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/VRSPEEDER.SCENE.MBIN" => ShipTypeEnum.FighterVrSpeeder,
            "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN" => ShipTypeEnum.Scientific,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/SPOOKSHIP.SCENE.MBIN" => ShipTypeEnum.ScientificBoundaryHerald,
            "MODELS/COMMON/SPACECRAFT/SHUTTLE/SHUTTLE_PROC.SCENE.MBIN" => ShipTypeEnum.Shuttle,
            "MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN" => ShipTypeEnum.Royal,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACER.SCENE.MBIN" => ShipTypeEnum.RoyalStarbornRunner,
            "MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACERSE.SCENE.MBIN" => ShipTypeEnum.RoyalStarbornPhoenix,
            "MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOSHIP_PROC.SCENE.MBIN" => ShipTypeEnum.Alien,
            "MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOFIGHTER.SCENE.MBIN" => ShipTypeEnum.AlienWraith,
            "MODELS/COMMON/SPACECRAFT/SAILSHIP/SAILSHIP_PROC.SCENE.MBIN" => ShipTypeEnum.Sail,
            "MODELS/COMMON/SPACECRAFT/SENTINELSHIP/SENTINELSHIP_PROC.SCENE.MBIN" => ShipTypeEnum.Robot,
            _ => null,
        };
    }

    internal static WeaponTypeEnum? GetWeaponClassEnumFromResource(string? resource)
    {
        return resource switch
        {
            "MODELS/COMMON/WEAPONS/MULTITOOL/ATLASMULTITOOL.SCENE.MBIN" => WeaponTypeEnum.Atlas,
            "MODELS/COMMON/WEAPONS/MULTITOOL/SWITCHMULTITOOL.SCENE.MBIN" => WeaponTypeEnum.RifleSwitch,
            "MODELS/COMMON/WEAPONS/MULTITOOL/ROYALMULTITOOL.SCENE.MBIN" => WeaponTypeEnum.Royal,
            "MODELS/COMMON/WEAPONS/MULTITOOL/SENTINELMULTITOOL.SCENE.MBIN" => WeaponTypeEnum.Robot,
            "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOL.SCENE.MBIN" => WeaponTypeEnum.Staff,
            "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLATLAS.SCENE.MBIN" => WeaponTypeEnum.StaffAtlas,
            "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLRUIN.SCENE.MBIN" => WeaponTypeEnum.StaffRuin,
            "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLBONE.SCENE.MBIN" => WeaponTypeEnum.StaffBone,
            _ => null,
        };
    }

    // Overridable to add additional/missing data.
    internal virtual void Link(JObject json, int index)
    {
        _index = index;
    }
}
