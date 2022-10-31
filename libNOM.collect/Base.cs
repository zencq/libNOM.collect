using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace libNOM.collect;


public class BaseCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".fb3", ".pb3", ".bse" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Goatfungus, FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public BaseCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(string path, out CollectionItem? result)
    {
        var item = result = null;

        var file = new FileInfo(path);
        if (!file.Exists)
            return false;

        if (GOATFUNGUS_EXTENSIONS.Contains(file.Extension))
        {
            item = ProcessGoatfungus(file);
            if (item is not null)
            {
                item.Format = FormatEnum.Goatfungus;
                item.Name = Path.GetFileNameWithoutExtension(file.Name); // this was added
            }
        }
        else
        {
            var json = File.ReadAllText(file.FullName);
            if (json.Contains("\"FileVersion\":1"))
            {
                item = ProcessKaii(json);
                if (item is not null)
                {
                    item.Format = FormatEnum.Kaii;
                }
            }
            else if (json.Contains("\"FileVersion\":2"))
            {
                item = ProcessStandard(json);
                if (item is not null)
                {
                    item.Format = FormatEnum.Standard;
                }
            }
            else
            {
                // nothing yet...
            }
        }
        if (item is null)
            return false;

        // Add additional data not available in all reading methods.
        item.DateCreated = file.CreationTime;
        item.Location = file;

        // Update collection and result before returning it.
        result = _collection.AddOrUpdate(item.Tag, item, (k, v) => item);
        return true;
    }

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Base(json, index);
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

        var result = new Base(json, index);
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
            { "PersistentBase", json.SelectToken(useMapping ? $"PlayerStateData.PersistentPlayerBases[{index}]" : $"6f=.F?0[{index}]") },
        };

        // Create tag.
        return Base.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessGoatfungus(FileInfo file)
    {
        var json = string.Empty;

        using (var binaryReader = new BinaryReader(File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            var header = binaryReader.ReadBytes(8);
            if (header.Length != 8 || !header.Take(4).SequenceEqual(Base.GoatfungusFileHeader))
                return null;

            var baseVersion = header[4] << 8 | header[5];
            switch (baseVersion)
            {
                case 3:
                case 4:
                    break;
                default:
                    return null;
            }

            // Needed to decrypt the file.
            var initializationVector = binaryReader.ReadBytes(16);
            if (initializationVector.Length != 16)
                return null;

#pragma warning disable SYSLIB0022 // Type or member is obsolete // Alternative for .NET 6 is System.Security.Cryptography.AesCng but only available on Windows.
            using var rijndael = new RijndaelManaged();
            rijndael.IV = initializationVector;
            rijndael.Key = Base.GoatfungusSecret;
#pragma warning restore SYSLIB0022

            using var cryptoStream = new CryptoStream(binaryReader.BaseStream, rijndael.CreateDecryptor(rijndael.Key, rijndael.IV), CryptoStreamMode.Read);

            if (cryptoStream.Read(header, 0, 4) != 4 || !header.Take(4).SequenceEqual(Base.GoatfungusCryptoHeader))
                return null;

            if (cryptoStream.ReadByte() < 0)
                return null;

            if (cryptoStream.ReadByte() < 0)
                return null;

            if (cryptoStream.ReadByte() < 0)
                return null;

            if (cryptoStream.ReadByte() < 0)
                return null;

            var bytes = new List<byte>();
            var read = 0;
            while ((read = cryptoStream.ReadByte()) >= 0)
            {
                bytes.Add((byte)(read));
            }

            json = Encoding.UTF8.GetString(bytes.ToArray());
        }

        var item = ProcessGoatfungus(json) as Base;
        item!.Type = file.Extension == COLLECTION_EXTENSIONS[0] ? PersistentBaseTypesEnum.FreighterBase : PersistentBaseTypesEnum.HomePlanetBase;
        return item;
    }

    protected override CollectionItem? ProcessGoatfungus(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Base
        {
            Data = new()
            {
                { "Objects", jObject },
            },
        };
    }

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        var baseType = jObject.SelectToken("Data.peI.DPp")!.Value<string>();
        return new Base
        {
#if NETSTANDARD2_0_OR_GREATER
            Type = (PersistentBaseTypesEnum)(Enum.GetValues(typeof(PersistentBaseTypesEnum)).Cast<Enum>().First(t => t.ToString() == baseType)),
#else
            Type = Enum.GetValues<PersistentBaseTypesEnum>().First(t => t.ToString() == baseType),
#endif
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Thumbnail")?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "PersistentBase", jObject.SelectToken("Data") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        var baseType = jObject.SelectToken("Data.PersistentBase.peI.DPp")!.Value<string>();
        return new Base
        {
#if NETSTANDARD2_0_OR_GREATER
            Type = (PersistentBaseTypesEnum)(Enum.GetValues(typeof(PersistentBaseTypesEnum)).Cast<Enum>().First(t => t.ToString() == baseType)),
#else
            Type = Enum.GetValues<PersistentBaseTypesEnum>().First(t => t.ToString() == baseType),
#endif
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "PersistentBase", jObject.SelectToken("Data.PersistentBase") },
            },
        };
    }

    #endregion
}

/// <summary>
/// ...
/// Note: NMS Base supports up to 6 previews/thumbnails.
/// </summary>
public class Base : CollectionItem
{
    #region Constant

    internal static byte[] GoatfungusCryptoHeader = new byte[] { 84, 82, 85, 69 }; // 54 52 55 45 (TRUE)
    internal static byte[] GoatfungusFileHeader = new byte[] { 78, 77, 83, 66 }; // 4E 4D 53 42 (NMSB)
    internal static byte[] GoatfungusFileVersion = new byte[] { 0, 4, 0, 0 };

    // Original by goatfungus as signed byte and converted to unsigend to be useable here.
    // sbyte[] { 50, -99, -78, -55, 92, 88, -34, 74, -57, 17, 57, -108, -94, sbyte.MaxValue, 97, -79 }; // 32 9D B2 C9 5C 58 DE 4A C7 11 39 94 A2 7F 61 B1
    internal static byte[] GoatfungusSecret = new byte[] { 50, 157, 178, 201, 92, 88, 222, 74, 199, 17, 57, 148, 162, 127, 97, 177 };

    #endregion

    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"PlayerStateData.PersistentPlayerBases[{_index}]";

            return $"6f=.F?0[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["PersistentBase"]?.GetValue<string>("NKm", "Name");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag
    {
        get
        {
            var tag = GetTag(Data);

            if (string.IsNullOrEmpty(tag))
                return Name.ToAlphaNumericString();

            return tag;
        }
    }

    internal PersistentBaseTypesEnum Type { get; set; }

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // GalacticAddress
        builder.Append(data["PersistentBase"]?.GetValue<string>("r:j", "GalacticAddress") ?? string.Empty);
        // Position
        builder.Append(data["PersistentBase"]?.GetValue<JArray>("wMC", "Position")?.ConcatSignedValues() ?? string.Empty);

        return builder.ToAlphaNumericString();
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "PersistentBase", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public Base() : base() { }

    public Base(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!BaseCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportGoatfungus()
    {
        // Prepare
        Deobfuscate();

        var objects = Data["Objects"]!.Serialize().GetBytes();
        var userData = Data["PersistentBase"]?.SelectToken("UserData")?.Value<int>() ?? 0;

#pragma warning disable SYSLIB0022 // Type or member is obsolete // Alternative for .NET 6 is System.Security.Cryptography.AesCng but only available on Windows.
        using var rijndael = new RijndaelManaged();
        rijndael.GenerateIV();
        rijndael.Key = GoatfungusSecret;
#pragma warning restore SYSLIB0022

        using var memoryStream = new MemoryStream();

        memoryStream.Write(GoatfungusFileHeader, 0, GoatfungusFileHeader.Length);
        memoryStream.Write(GoatfungusFileVersion, 0, GoatfungusFileVersion.Length);
        memoryStream.Write(rijndael.IV, 0, rijndael.IV.Length);

        using var cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(rijndael.Key, rijndael.IV), CryptoStreamMode.Write);

        cryptoStream.Write(GoatfungusCryptoHeader, 0, GoatfungusCryptoHeader.Length);
        cryptoStream.WriteByte((byte)(userData >> 24 & 0xFF));
        cryptoStream.WriteByte((byte)(userData >> 16 & 0xFF));
        cryptoStream.WriteByte((byte)(userData >> 8 & 0xFF));
        cryptoStream.WriteByte((byte)(userData & 0xFF));
        cryptoStream.Write(objects, 0, objects.Length);

        cryptoStream.Flush();
        cryptoStream.FlushFinalBlock();

        // Return
        return memoryStream.ToArray();
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var jsonAddress = Data["PersistentBase"]!.SelectToken("r:j")!;
        var galacticAddress = jsonAddress.Type == JTokenType.String ? jsonAddress.Value<string>()! : jsonAddress.Value<long>().ToString("X");
        var result = new Dictionary<string, object?>
        {
            { nameof(Data),  Data["PersistentBase"] },
            { nameof(DateCreated), DateCreated },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { "GalacticAddress", galacticAddress },
            { "Galaxy", galacticAddress.GetGalaxy() },
            { "GlyphsString", galacticAddress.GetGlyphsString() },
            { nameof(Starred), Starred },
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

        if (Data.TryGetValue("PersistentBase", out var persistentBase) && persistentBase is not null)
        {
            // Store previous values to keep position and owner.
            var galacticAddress = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.PersistentPlayerBases[{index}].GalacticAddress" : $"6f=.F?0[{index}].oZw");
            var position = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.PersistentPlayerBases[{index}].Position" : $"6f=.F?0[{index}].wMC");
            var forward = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.PersistentPlayerBases[{index}].Forward" : $"6f=.F?0[{index}].oHw");
            var owner = json.SelectDeepClonedToken(_useMapping ? $"PlayerStateData.PersistentPlayerBases[{index}].Owner" : $"6f=.F?0[{index}].3?K");

            if (_useMapping)
            {
                json["PlayerStateData"]!["PersistentPlayerBases"]![index] = persistentBase;

                json["PlayerStateData"]!["PersistentPlayerBases"]![index]!["GalacticAddress"] = galacticAddress;
                json["PlayerStateData"]!["PersistentPlayerBases"]![index]!["Position"] = position;
                json["PlayerStateData"]!["PersistentPlayerBases"]![index]!["Forward"] = forward;
                json["PlayerStateData"]!["PersistentPlayerBases"]![index]!["Owner"] = owner;
            }
            else
            {

                json["6f="]!["F?0"]![index] = persistentBase;

                json["6f="]!["F?0"]![index]!["oZw"] = galacticAddress;
                json["6f="]!["F?0"]![index]!["wMC"] = position;
                json["6f="]!["F?0"]![index]!["oHw"] = forward;
                json["6f="]!["F?0"]![index]!["3?K"] = owner;
            }
        }
        if (Data.TryGetValue("Objects", out var objects) && objects is not null)
        {
            if (_useMapping)
            {
                json["PlayerStateData"]!["PersistentPlayerBases"]![index]!["Objects"] = objects;
            }
            else
            {
                json["6f="]!["F?0"]![index]!["@ZJ"] = objects;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!BaseCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        if (format == FormatEnum.Goatfungus)
            return Type == PersistentBaseTypesEnum.FreighterBase ? ".fb3" : ".pb3";

        return ".bse";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // GalacticAddress
            // Position
            var galacticAddress = Data["PersistentBase"]?.GetValue<string>("r:j", "GalacticAddress");
            var position = Data["PersistentBase"]?.GetValue<JArray>("wMC", "Position")?.ConcatSignedValues();

            return $"{galacticAddress}{position}";
        }
        return Name;
    }
}
