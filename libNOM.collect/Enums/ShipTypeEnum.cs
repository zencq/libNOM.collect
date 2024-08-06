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
    [Description("Golden Vector")]
    FighterClassicGold,
    [Description("Horizon Vector NX")]
    FighterSpecialSwitch,
    FighterVrSpeeder,
    FighterStarbornRunner,
    Scientific,
    Shuttle,
    Royal,
    Alien,
    Sail,
    Robot,
}
