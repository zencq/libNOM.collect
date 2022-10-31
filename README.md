# libNOM.collect

![Maintained](https://img.shields.io/maintenance/yes/2022)
[![.NET Standard 2.0 - 2.1 | 6.0](https://img.shields.io/badge/.NET-Standard%202.0%20--%202.1%20%7C%206.0-lightgrey)](https://dotnet.microsoft.com/en-us/)
[![C# 10](https://img.shields.io/badge/C%23-10-lightgrey)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Release](https://img.shields.io/github/v/release/zencq/libNOM.collect?display_name=tag)](https://github.com/zencq/libNOM.collect/releases/latest)

[![libNOM.collect](https://github.com/zencq/libNOM.collect/actions/workflows/pipeline.yml/badge.svg)](https://github.com/zencq/libNOM.collect/actions/workflows/pipeline.yml)

## Introduction

The `libNOM` label is a collection of .NET class libraries originally developed
and used in [NomNom](https://github.com/zencq/NomNom), a savegame editor for [No Man's Sky](https://www.nomanssky.com/).

`libNOM.collect` can be used to backup and restore your collections.

## Getting Started

TODO

### Usage

TODO
```csharp
// Export
var format = FormatEnum.N3C2;
var json = container.GetJsonObject();
var path = "...";

new Outfit().Export(path, format, json); // export current
```

TODO
```csharp
// Import
var path "...";

var outfit = new Outfit().Import(path);
```

## License

This project is licensed under the GNU GPLv3 license - see the [LICENSE](LICENSE)
file for details.

## Authors

* **Christian Engelhardt** (zencq) - [GitHub](https://github.com/cengelha)

## Credits

Thanks to the following people for their help in one way or another.

* [Dr. Kaii](https://www.nexusmods.com/nomanssky/mods/1879) - Collaboration to create a common import/export format (N3C) as well as providing some code

## Dependencies

* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) - Handle JSON objects
