using System.ComponentModel;

namespace libNOM.collect.Enums;


/// <summary>
/// 
/// </summary>
/// <seealso cref="libMBIN\Source\NMS\GameComponents\GcSpaceshipClasses.cs"/>
internal enum ShipTypeEnum
{
    Freighter,
    Dropship,
    Fighter,
    FighterClassicGold,
    FighterSpecialSwitch,
    FighterVrSpeeder,
    [Obsolete("Use RoyalStarbornRunner instead.")]
    FighterStarbornRunner,
    [Obsolete("Use ScientificBoundaryHerald instead.")]
    FighterBoundaryHerald,
    Scientific,
    ScientificBoundaryHerald,
    Shuttle,
    Royal,
    RoyalStarbornRunner,
    RoyalStarbornPhoenix,
    Alien,
    AlienWraith,
    Sail,
    Robot,

    Unknown,
}
