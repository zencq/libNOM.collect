using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;

namespace libNOM.collect;


public abstract class Collection : IEnumerable<CollectionItem>
{
    #region Constant

    protected abstract string[] COLLECTION_EXTENSIONS { get; }
    protected static readonly string[] GOATFUNGUS_EXTENSIONS = new[] { ".fb3", ".pb3", ".pet", ".sh0", ".wp0" }; // Freighter Base, Planetary Base, Pet, Ship, Weapon
    public static readonly FormatEnum[] SUPPORTED_FORMATS = Array.Empty<FormatEnum>();

    #endregion

    #region Field

    protected readonly ConcurrentDictionary<string, CollectionItem> _collection = new();
    protected string _path = null!;

    #endregion

    // //

    #region Constructor

    public Collection(string path)
    {
        Reinitialize(path);
    }

    #endregion

    // //

    #region Getter

    protected abstract string GetTag(JObject json, int index);

    #endregion

    #region Collection

    public virtual bool AddOrUpdate(string path, out CollectionItem? result)
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

    public abstract bool AddOrUpdate(JObject json, int index, out CollectionItem? result);

    public bool AddOrUpdate(string json, FormatEnum format, out CollectionItem? result)
    {
        var item = result = null;

        item = format switch
        {
            FormatEnum.Goatfungus => ProcessGoatfungus(json),
            FormatEnum.Kaii => ProcessKaii(json),
            FormatEnum.Standard => ProcessStandard(json),
            _ => null,
        };
        if (item is null)
            return false;

        item.Format = format;

        // Update collection and result before returning it.
        result = _collection.AddOrUpdate(item.Tag, item, (k, v) => item);
        return true;
    }

    public void Remove(CollectionItem outfit)
    {
        if (!outfit.IsLinked)
        {
            _collection.TryRemove(outfit.Tag, out _);
        }
        if (outfit.IsCollected)
        {
            File.Delete(outfit.Location!.FullName);
        }
    }

    public abstract CollectionItem? GetOrAdd(JObject json, int index);

    #endregion

    #region IEnumerable

    public IEnumerator<CollectionItem> GetEnumerator()
    {
        foreach (var pair in _collection)
        {
            yield return pair.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Initialize

    public void Reinitialize()
    {
        // Clear current collection.
        _collection.Clear();

        var tasks = new List<Task>();

        // Read all files in path and add them to the collection.
        foreach (var extension in COLLECTION_EXTENSIONS)
        {
            foreach (var file in Directory.GetFiles(_path, $"*{extension}"))
            {
                tasks.Add(Task.Run(() => AddOrUpdate(file, out _)));
            }
        }

        Task.WaitAll(tasks.ToArray());
    }

    public void Reinitialize(string path)
    {
        // Set new path.
        _path = path;

        // Call actual method that uses _path.
        Directory.CreateDirectory(_path);
        Reinitialize();
    }

    #endregion

    #region Process

    protected virtual CollectionItem? ProcessGoatfungus(FileInfo file)
    {
        var json = File.ReadAllText(file.FullName);
        return ProcessGoatfungus(json);
    }

    protected virtual CollectionItem? ProcessGoatfungus(string json)
    {
        return null;
    }

    protected virtual CollectionItem? ProcessKaii(string json)
    {
        return null;
    }

    protected virtual CollectionItem? ProcessStandard(string json)
    {
        return null;
    }

    #endregion
}
