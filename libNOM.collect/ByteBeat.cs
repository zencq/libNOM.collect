using Newtonsoft.Json.Linq;

namespace libNOM.collect;


public class ByteBeatCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".bbt" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Kaii, FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public ByteBeatCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new ByteBeat(json, index);
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

        var result = new ByteBeat(json, index);
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
            { "Song", json.SelectToken(useMapping ? $"CommonStateData.ByteBeatLibrary.MySongs[{index}]" : $"<h0.8iI.ON4[{index}]") },
        };

        // Create tag.
        return ByteBeat.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessKaii(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new ByteBeat
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken("Track")?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Song", jObject.SelectToken("Data") },
            },
        };
    }

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new ByteBeat
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Song", jObject.SelectToken("Data.Song") },
            },
        };
    }

    #endregion
}

public class ByteBeat : CollectionItem
{
    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return $"CommonStateData.ByteBeatLibrary.MySongs[{_index}]";

            return $"<h0.8iI.ON4[{_index}]";
        }
    }

    public override string Name // { get; set; }
    {
        get
        {
            var fromJson = Data["Song"]?.GetValue<string>("NKm", "Name");
            return string.IsNullOrWhiteSpace(fromJson) ? (_name ?? string.Empty) : fromJson!;
        }
        set => _name = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        // Data
        return string.Concat(data["Song"]!.GetValue<JArray>("8?J", "Data") ?? new JArray());
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Song", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public ByteBeat() : base() { }

    public ByteBeat(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!ByteBeatCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    protected override byte[] ExportKaii()
    {
        // Prepare
        Obfuscate();
        var result = new Dictionary<string, object?>
        {
            { nameof(Data), Data["Song"] },
            { nameof(DateCreated), DateCreated },
            { nameof(Description), Description },
            { "FileVersion", 1 },
            { "OriginalAuthor", Data["Song"]!.SelectToken("4ha")!.Value<string>() },
            { "OriginalAuthorID", Data["Song"]!.SelectToken("m7b")!.Value<string>() },
            { "OriginalAuthorPlatform", Data["Song"]!.SelectToken("d2f")!.Value<string>() },
            { "OriginalName", Data["Song"]!.SelectToken("NKm")!.Value<string>() },
            { nameof(Starred), Starred },
            { "Track", Preview?.ToBase64String() },
        };

        // Return
        return result.Serialize().GetBytes();
    }

    protected override byte[] ExportStandard()
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
            { nameof(Starred), Starred },
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

        if (Data.TryGetValue("Song", out var song) && song is not null)
        {
            if (_useMapping)
            {
                json["CommonStateData"]!["ByteBeatLibrary"]!["MySongs"]![index] = song;
            }
            else
            {

                json["<h0"]!["8iI"]!["ON4"]![index] = song;
            }
        }
    }

    #endregion

    protected override string? GetExtension(FormatEnum format)
    {
        if (!ByteBeatCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".bbt";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // Data
            return $"{nameof(ByteBeat)}-{_index}";
        }
        return Name;
    }
}
