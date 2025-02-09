# libNOM.collect

![Maintained](https://img.shields.io/maintenance/yes/2025)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/zencq/libNOM.collect/pipeline.yml?logo=github)](https://github.com/zencq/libNOM.collect/actions/workflows/pipeline.yml)
[![Maintainability](https://api.codeclimate.com/v1/badges/85ed3f9d8cd05cac804b/maintainability)](https://codeclimate.com/github/zencq/libNOM.collect/maintainability)

[![.NET | Standard 2.0 - 2.1 | 6 - 9](https://img.shields.io/badge/.NET-Standard%202.0%20--%202.1%20%7C%206%20--%209-lightgrey)](https://dotnet.microsoft.com/en-us/)
[![C# 13](https://img.shields.io/badge/C%23-13-lightgrey)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![GitHub Release](https://img.shields.io/github/v/release/zencq/libNOM.collect?logo=github)](https://github.com/zencq/libNOM.collect/releases/latest)
[![NuGet Version](https://img.shields.io/nuget/v/libNOM.collect?logo=nuget)](https://www.nuget.org/packages/libNOM.collect/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/libNOM.collect?logo=nuget)](https://www.nuget.org/packages/libNOM.collect/)

## Introduction

The `libNOM` label is a collection of .NET class libraries originally developed
and used in [NomNom](https://github.com/zencq/NomNom), the most complete savegame
editor for [No Man's Sky](https://www.nomanssky.com/).

`libNOM.collect` can be used to backup and restore all kinds of collections, like
*Starships* and *Companions*, to bypass the in-game limits.

## Getting Started

All commonly used formats are supported. This includes those used by [goatfungus](https://github.com/goatfungus/NMSSaveEditor),
[NMS Companion](https://www.nexusmods.com/nomanssky/mods/1879), and the [NMS Ship Editor/Colorizer/Customizer](https://www.patreon.com/posts/65130473).

There is also a new version of the NMS Companion format that unifies the
file content (including features such as marking as favorite) and adds missing entries
like the customization.

### Usage

Create a collection and add/remove items.
```csharp
var path = "..."; // where the collection is stored
var companionCollection = new CompanionCollection(path);

// Adding a new item to the collection can be done via a JSON string or reading from a file.
var jsonString = "..."; // JSON string in one of the supported formats (depends on collection type)
companionCollection.AddOrUpdate(jsonString, libNOM.collect.Enums.FormatEnum.Kaii, out var stringCompanion);

var pathToFile = "...";
companionCollection.AddOrUpdate(pathToFile, out var fileCompanion); // format will be automatically detected

// Remove
companionCollection.Remove(fileCompanion);
```

Backup an item.
```csharp
var json = container.GetJsonObject(); // JObject of the entire save
var format = libNOM.collect.Enums.FormatEnum.Kaii; // one of the supported formats (depends on collection type)
var path = "..."; // where the collection is stored

stringCompanion.Export(json, format, path);
```

## License

This project is licensed under the GNU GPLv3 license - see the [LICENSE](LICENSE)
file for details.

## Authors

* **Christian Engelhardt** (zencq) - [GitHub](https://github.com/zencq)

## Credits

Thanks to the following people for their help in one way or another.

* [Dr. Kaii](https://www.nexusmods.com/nomanssky/mods/1879) - Collaboration for the import/export format as well as providing some code

## Dependencies

* [libNOM.map](https://www.nuget.org/packages/libNOM.map) - Obfuscation and deobfuscation
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) - Handle JSON objects
