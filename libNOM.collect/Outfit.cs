﻿using Newtonsoft.Json.Linq;
using System.Text;

namespace libNOM.collect;


public class OutfitCollection : Collection
{
    #region Constant

    protected override string[] COLLECTION_EXTENSIONS { get; } = new[] { ".ott" };
    public static new readonly FormatEnum[] SUPPORTED_FORMATS = new[] { FormatEnum.Standard };

    #endregion

    // //

    #region Constructor

    public OutfitCollection(string path) : base(path) { }

    #endregion

    // //

    #region Collection

    public override bool AddOrUpdate(JObject json, int index, out CollectionItem? result)
    {
        result = new Outfit(json, index);
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

        var result = new Outfit(json, index);
        _collection.TryAdd(key, result);
        return result;
    }

    #endregion

    #region Getter

    private string? GetPreset(JObject json, int index)
    {
        if (index < 0)
        {
            var preset = json.GetValue<string>("6f=.l:j[0].VFd", "PlayerStateData.CharacterCustomisationData[0].SelectedPreset");
            if (preset is not null && preset != "^")
                return preset.Replace("^", "");
        }

        return null;
    }

    protected override string GetTag(JObject json, int index)
    {
        // Use preset as tag.

        if (index < 0)
        {
            var preset = GetPreset(json, index);
            if (preset is not null)
            {
                if (!preset.StartsWith("OUTFIT"))
                    return preset;

                index = Convert.ToInt32(preset.Substring(preset.Length - 1, 1)) - 1;
            }
        }

        string? path;
        bool useMapping = json.UseMapping();

        // Prepare.
        if (useMapping)
        {
            path = index < 0 ? "PlayerStateData.CharacterCustomisationData[0].CustomData" : $"PlayerStateData.Outfits[{index}]";
        }
        else
        {
            path = index < 0 ? "6f=.l:j[0].wnR" : $"6f=.cf5[{index}]";
        }

        // Create Dictionary.
        var data = new Dictionary<string, JToken?>
        {
            { "Outfit", json.SelectToken(path) },
        };

        // Create tag.
        return Outfit.GetTag(data);
    }

    #endregion

    #region Process

    protected override CollectionItem? ProcessStandard(string json)
    {
        var jObject = json.Deserialize();
        if (jObject is null)
            return null;

        return new Outfit
        {
            DateCreated = jObject.SelectToken(nameof(CollectionItem.DateCreated))?.Value<DateTime>().ToLocalTime() ?? DateTimeOffset.Now,
            Description = jObject.SelectToken(nameof(CollectionItem.Description))?.Value<string>() ?? string.Empty,
            Preview = jObject.SelectToken(nameof(CollectionItem.Preview))?.Value<string>()?.GetBytesFromBase64String(),
            Starred = jObject.SelectToken(nameof(CollectionItem.Starred))?.Value<bool>() ?? false,

            Data = new()
            {
                { "Outfit", jObject.SelectToken("Data.Outfit") },
            },
        };
    }

    #endregion
}

public partial class Outfit : CollectionItem
{
    #region Constant

    internal const string TAG_START_01 = "ARMOURASTROASTRONAUTBASEBACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROHEADFF8400A1FFFFFFA2FFFFFFTORSOFF8400A13C90DDA23C90DDARMOUR3C90DDA1000000A2000000BACKPACKFF8400A1000000A2FFFFFFHANDSFF8400A1000000A2000000LEGSFF8400A1DD6767A2000000FEETFF8400A1000000A2000000ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK0BOOTSASTROBOOTS0GLOVESASTROGLOVES0HEADASTROHELMET0LEGSASTROLEGS0TORSOASTROTORSO000000010";
    internal const string TAG_START_02 = "ARMOURASTROASTRONAUTBASEASTROHEAD9BACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROHEAD3B4A66A1C09D70A2000000TORSO3B4A66A13C90DDA2C09D70ARMOURC09D70A1000000A2000000BACKPACK3B4A66A1C09D70A2C09D70HANDS3B4A66A1C09D70A2C09D70LEGS3B4A66A13B4A66A2C09D70FEET3B4A66A1C09D70A2C09D70ARMOURASTROCHESTARMOUR2BACKPACKBACKPACK1BOOTSASTROBOOTS0GLOVESASTROGLOVES3HEADASTROHELMET3LEGSASTROLEGS0TORSOASTROTORSO000000010";
    internal const string TAG_START_03 = "ARMOURASTROASTRONAUTBASEASTROHEAD9BACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROHEADAAAAAAA1895D47A2803939TORSOFFFFFFA1AAAAAAA23B4A66ARMOUR895D47A14B4B4BA2000000BACKPACKAAAAAAA1895D47A23C90DDHANDSFFFFFFA13B4A66A23B4A66LEGSFFFFFFA14B4B4BA2000000FEETFFFFFFA1895D47A2895D47ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK2BOOTSASTROBOOTS1GLOVESASTROGLOVES0HEADASTROHELMET0LEGSASTROLEGS1TORSOASTROTORSO300000010";
    internal const string TAG_START_04 = "ARMOURASTROASTRONAUTBASEASTROHEAD6BACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROHEADDD6767A1000000A2FFFFFFTORSODD6767A1000000A2000000ARMOUR000000A1000000A2000000BACKPACKDD6767A1000000A2C09D70HANDSDD6767A13D7A57A2000000LEGSDD6767A1C09D70A2000000FEETDD6767A1C09D70A2C09D70ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK0BOOTSASTROBOOTS0GLOVESASTROGLOVES0HEADASTROHELMET0LEGSASTROLEGS0TORSOASTROTORSO000000010";
    internal const string TAG_ASTRO_DEFAULT = "ARMOURASTROASTRONAUTBASEASTROHEAD0BOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROHEADFF8400A13C90DDA2FF8400TORSOFF8400A13C90DDA23C90DDARMOUR3C90DDA1000000A2000000BACKPACKFF8400A13C90DDA2FFFFFFHANDSFF8400A13C90DDA2000000LEGSFF8400A1895D47A2FF8400FEETFF8400A13C90DDA2000000ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK1BOOTSASTROBOOTS1GLOVESASTROGLOVES1HEADASTROHELMET1LEGSASTROLEGS0TORSOASTROTORSO100030010";
    internal const string TAG_ASTRO_0 = "ARMOURVANILLAASTRONAUTBASEASTROHEAD2BACKPACKVANILLBOOTSVANILLACAPENULLGLOVESVANILLALEGSVANILLATORSOVANILLAHEADDD6767A1DD6767A2DD6767TORSODD6767A1DD6767A2DD6767ARMOURDD6767A1000000A2DD6767BACKPACKDD6767A1000000A24B4B4BHANDSDD6767A1DD6767A2DD6767LEGS000000A1DD6767A2DD6767FEETDD6767A1000000A2DD6767ARMOURVANILLA1BACKPACKBACKPACK2BOOTSVANILLA1HEADASTROHELMET200030010";
    internal const string TAG_ASTRO_1 = "ARMOURFOURTHASTRONAUTBASEASTROHEAD3BACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESFOURTHLEGSFOURTHTORSOFOURTHHEAD3D7A57A151BB7EA2FFFFFFTORSO3D7A57A151BB7EA2895D47ARMOUR3D7A57A151BB7EA2FFFFFFBACKPACK3D7A57A151BB7EA2FFFFFFHANDS3D7A57A151BB7EA2000000LEGS3D7A57A151BB7EA23D7A57FEET3D7A57A151BB7EA251BB7EARMOURFOURTH1BACKPACKBACKPACK2BOOTSFOURTH1GLOVESFOURTH1TORSOFOURTH100030010";
    internal const string TAG_ASTRO_2 = "ARMOURGEKASTRONAUTBASEASTROHEAD7BACKPACKVANILLBOOTSGEKCAPENULLGLOVESGEKLEGSGEKTORSOGEKHEADFFFFFFA1895D47A2C09D70TORSO895D47A1895D47A2C09D70ARMOUR895D47A1895D47A2FFFFFFBACKPACK895D47A1895D47A2895D47HANDS895D47A1895D47A2895D47LEGS895D47A1C09D70A2000000FEET895D47A1895D47A2895D47ARMOURGEK1BACKPACKBACKPACK2BOOTSGEK1HEADASTROHELMET2LEGSGEK1TORSOGEK100030010";
    internal const string TAG_ASTRO_3 = "ARMOURVANILLAASTRONAUTBASEASTROHEAD4BACKPACKVANILLBOOTSVANILLACAPENULLGLOVESFOURTHLEGSVANILLATORSOFOURTHHEADFFFFFFA1AAAAAAA2AAAAAATORSOFFFFFFA1AAAAAAA2AAAAAAARMOURFFFFFFA1AAAAAAA2FFFFFFBACKPACKFFFFFFA1AAAAAAA2FFFFFFHANDSFFFFFFA1000000A2AAAAAALEGSFFFFFFA1FFFFFFA2FFFFFFFEETFFFFFFA1FFFFFFA2000000ARMOURVANILLA0BACKPACKBACKPACK3BOOTSVANILLA1GLOVESFOURTH1HEADASTROHELMET2TORSOFOURTH100030010";
    internal const string TAG_ASTRO_4 = "ARMOURGEKASTRONAUTBASEASTROHEAD10BACKPACKVANILLBOOTSVANILLACAPENULLGLOVESGEKLEGSVANILLATORSOFOURTHHEAD3B4A66A13B4A66A2000000TORSO3B4A66A13B4A66A2000000ARMOUR3B4A66A1000000A2000000BACKPACK3B4A66A13B4A66A23C90DDHANDS3B4A66A13B4A66A23B4A66LEGS3B4A66A13B4A66A23B4A66FEET3B4A66A13B4A66A23C90DDARMOURGEK1BACKPACKBACKPACK3BOOTSVANILLA1HEADASTROHELMET2LEGSVANILLA1TORSOFOURTH100030010";
    internal const string TAG_ASTRO_5 = "ARMOURVANILLAASTRONAUTBASEASTROHEAD5BACKPACKVANILLBOOTSGEKCAPENULLGLOVESVANILLALEGSGEKTORSOASTROHEADE484C1A1E484C1A23C90DDTORSOE484C1A18E546CA23C90DDARMOURE484C1A1000000A2000000BACKPACKFFFFFFA1E484C1A24B4B4BHANDSE484C1A1E484C1A2000000LEGSE484C1A1E484C1A2E484C1FEETE484C1A1E484C1A2000000ARMOURVANILLA1BACKPACKBACKPACK2BOOTSGEK1HEADASTROHELMET2LEGSGEK1TORSOASTROTORSO300030010";
    internal const string TAG_ASTRO_6 = "ARMOURFOURTHASTRONAUTBASEASTROHEAD9BACKPACKVANILLBOOTSVANILLACAPENULLGLOVESGEKLEGSASTROTORSOASTROHEADF2D064A14B4B4BA2F2D064TORSOF2D064A1000000A24B4B4BARMOURFFFFFFA1AAAAAAA2C09D70BACKPACK000000A1F2D064A2FFFFFFHANDS000000A1C09D70A2F2D064LEGSF2D064A14B4B4BA2F2D064FEET000000A14B4B4BA24B4B4BARMOURFOURTH1BACKPACKBACKPACK4BOOTSVANILLA1HEADASTROHELMET2LEGSASTROLEGS4TORSOASTROTORSO400030010";
    internal const string TAG_ASTRO_7 = "ARMOURVANILLAASTRONAUTBASEASTROHEAD8BACKPACKVANILLBOOTSVANILLACAPENULLGLOVESFOURTHLEGSVANILLATORSOGEKHEAD000000A15D4770A2000000TORSO000000A1895D47A251BB7EARMOUR5D4770A1000000A2000000BACKPACK000000A1000000A2FFFFFFHANDS000000A1000000A2000000LEGS000000A15D4770A25D4770FEET000000A15D4770A2000000ARMOURVANILLA1BACKPACKBACKPACK4BOOTSVANILLA1GLOVESFOURTH1HEADASTROHELMET2LEGSVANILLA1TORSOGEK100030010";
    internal const string TAG_ASTRO_8 = "ARMOURASTROASTRONAUTBASEASTROHEAD1BACKPACKVANILLBOOTSASTROCAPENULLGLOVESVANILLALEGSGEKTORSOVANILLAHEAD000000A1000000A24B4B4BTORSO000000A1000000A274C9BAARMOUR51BB7EA151BB7EA251BB7EBACKPACK000000A151BB7EA2000000HANDS000000A1000000A274C9BALEGS000000A1000000A2000000FEET000000A13D7A57A251BB7EARMOURASTROCHESTARMOUR1BACKPACKBACKPACK2BOOTSASTROBOOTS4GLOVESVANILLA1HEADASTROHELMET2LEGSGEK1TORSOVANILLA100030010";
    internal const string TAG_KORVAX_DEFAULT = "ARMOURASTROBOOTSVANILLACAPENULLGLOVESVANILLAKORVAXBASEKORVAXHEAD1LEGSVANILLATORSOVANILLAHEAD3C90DDA1AAAAAAA2AAAAAATORSO3C90DDA13B4A66A23B4A66ARMOUR3C90DDA13C90DDA23C90DDBACKPACK3B4A66A1000000A2FFFFFFHANDS3C90DDA13B4A66A23B4A66LEGS000000A13C90DDA23B4A66FEET3C90DDA13C90DDA2000000ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK2BOOTSVANILLA1GLOVESVANILLA1LEGSVANILLA100000010";
    internal const string TAG_KORVAX_0 = "ARMOURASTROBACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROKORVAXBASEKORVAXHEAD2LEGSASTROTORSOASTROHEAD3D6F70A1AAAAAAA2AAAAAATORSO3D6F70A1803939A23D6F70ARMOUR74C9BAA174C9BAA2000000BACKPACK74C9BAA1000000A23D6F70HANDS3D6F70A13D6F70A23D6F70LEGS3D6F70A13D6F70A2DD6767FEET3D6F70A13D6F70A23D6F70ARMOURASTROCHESTARMOUR1BACKPACKBACKPACK2BOOTSASTROBOOTS0GLOVESASTROGLOVES0LEGSASTROLEGS0TORSOASTROTORSO000030010";
    internal const string TAG_KORVAX_1 = "ARMOURFOURTHBACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESFOURTHKORVAXBASEKORVAXHEAD3LEGSFOURTHTORSOFOURTHHEAD895D47A1AAAAAAA2AAAAAATORSOAD85CFA1AD85CFA2895D47ARMOURAD85CFA1AD85CFA2F2D064BACKPACKAD85CFA1000000A2C09D70HANDSAAAAAAA15D4770A2F2D064LEGSAD85CFA1AD85CFA2AD85CFFEET895D47A1FFFFFFA25D4770ARMOURFOURTH1BACKPACKBACKPACK3BOOTSFOURTH0GLOVESFOURTH1TORSOFOURTH100030010";
    internal const string TAG_KORVAX_2 = "ARMOURGEKBACKPACKVANILLBOOTSGEKCAPENULLGLOVESGEKKORVAXBASEKORVAXHEAD4LEGSGEKTORSOGEKHEAD4B4B4BA1AAAAAAA2AAAAAATORSO4B4B4BA1000000A23C90DDARMOUR4B4B4BA1AAAAAAA23C90DDBACKPACK4B4B4BA13C90DDA24B4B4BHANDS4B4B4BA1000000A24B4B4BLEGS4B4B4BA1895D47A2000000FEET4B4B4BA1000000A23C90DDARMOURGEK1BACKPACKBACKPACK4BOOTSGEK1LEGSGEK1TORSOGEK300030010";
    internal const string TAG_KORVAX_3 = "ARMOURASTROBACKPACKVANILLBOOTSVANILLACAPENULLGLOVESASTROKORVAXBASEKORVAXHEAD8LEGSFOURTHTORSOFOURTHHEAD895D47A1AAAAAAA2AAAAAATORSOC09D70A1C09D70A2C09D70ARMOURC09D70A1C09D70A2C09D70BACKPACKF2D064A1C09D70A2C09D70HANDSC09D70A1C09D70A2C09D70LEGS000000A1C09D70A2C09D70FEETC09D70A1C09D70A2C09D70ARMOURASTROCHESTARMOUR1BACKPACKBACKPACK4BOOTSVANILLA1GLOVESASTROGLOVES0TORSOFOURTH100030010";
    internal const string TAG_KORVAX_4 = "ARMOURVANILLABACKPACKVANILLBOOTSVANILLACAPENULLGLOVESASTROKORVAXBASEKORVAXHEAD6LEGSVANILLATORSOVANILLAHEADDD6767A1AAAAAAA2AAAAAATORSO000000A1000000A2000000ARMOURDD6767A1DD6767A2DD6767BACKPACK000000A1DD6767A2DD6767HANDSDD6767A1803939A2DD6767LEGS000000A1000000A2000000FEETDD6767A1DD6767A2DD6767ARMOURVANILLA1BACKPACKBACKPACK2BOOTSVANILLA1GLOVESASTROGLOVES0LEGSVANILLA1TORSOVANILLA100030010";
    internal const string TAG_KORVAX_5 = "ARMOURGEKBACKPACKVANILLBOOTSVANILLACAPENULLGLOVESASTROKORVAXBASEKORVAXHEAD7LEGSVANILLATORSOFOURTHHEADFFFFFFA1AAAAAAA2AAAAAATORSO74C9BAA1000000A23B4A66ARMOURFFFFFFA1FFFFFFA2000000BACKPACKFFFFFFA1FFFFFFA2FFFFFFHANDSFFFFFFA1FFFFFFA2FFFFFFLEGS74C9BAA1000000A274C9BAFEETFFFFFFA151BB7EA251BB7EARMOURGEK1BACKPACKBACKPACK2BOOTSVANILLA1GLOVESASTROGLOVES0LEGSVANILLA1TORSOFOURTH100030010";
    internal const string TAG_KORVAX_6 = "ARMOURVANILLABACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESVANILLAKORVAXBASEKORVAXHEAD7LEGSGEKTORSOGEKHEAD895D47A1AAAAAAA2AAAAAATORSO895D47A1895D47A2895D47ARMOURFF8400A1FFFFFFA2895D47BACKPACKFF8400A1895D47A2C09D70HANDS3C90DDA1FFFFFFA23C90DDLEGS895D47A13C90DDA2895D47FEETFF8400A1FF8400A2FF8400ARMOURVANILLA1BACKPACKBACKPACK2BOOTSFOURTH0GLOVESVANILLA1LEGSGEK1TORSOGEK300030010";
    internal const string TAG_KORVAX_7 = "ARMOURVANILLABACKPACKVANILLBOOTSGEKCAPENULLGLOVESFOURTHKORVAXBASEKORVAXHEAD9LEGSVANILLATORSOASTROHEAD3C90DDA1AAAAAAA2AAAAAATORSO3C90DDA13C90DDA23C90DDARMOURF2D064A13C90DDA2000000BACKPACK3C90DDA1F2D064A2C09D70HANDSF2D064A1F2D064A2F2D064LEGSF2D064A1F2D064A2F2D064FEETF2D064A1F2D064A23C90DDARMOURVANILLA1BACKPACKBACKPACK2BOOTSGEK1GLOVESFOURTH1LEGSVANILLA1TORSOASTROTORSO000030010";
    internal const string TAG_KORVAX_8 = "ARMOURFOURTHBACKPACKVANILLBOOTSGEKCAPENULLGLOVESGEKKORVAXBASEKORVAXHEAD10LEGSASTROTORSOGEKHEAD803939A1AAAAAAA2AAAAAATORSODD6767A1803939A2FFFFFFARMOUR803939A1AAAAAAA2000000BACKPACKDD6767A1FFFFFFA2FFFFFFHANDS803939A1803939A2803939LEGS803939A1DD6767A2FFFFFFFEET803939A1803939A2803939ARMOURFOURTH1BACKPACKBACKPACK2BOOTSGEK1GLOVESGEK1LEGSASTROLEGS0TORSOGEK300030010";
    internal const string TAG_GEK_DEFAULT = "ARMOURGEKBOOTSGEKCAPENULLGEKBASEGEKHEAD1GLOVESGEKLEGSGEKTORSOGEKHEAD895D47A13C90DDA2C09D70TORSOFF8400A1FF8400A2FF8400ARMOURFF8400A1FF8400A2C09D70BACKPACKFF8400A1FF8400A2FF8400HANDSFF8400A13B4A66A2FF8400LEGS895D47A1FF8400A2FF8400FEET3B4A66A13B4A66A23B4A66ARMOURGEK1BACKPACKBACKPACK1BOOTSGEK0GLOVESGEK1HEADGBEAK1HEADGMARKSCALESHEADGTOPALT4LEGSGEK1PUPILSTXT3TORSOGEK110000010";
    internal const string TAG_GEK_0 = "ARMOURVANILLABACKPACKVANILLBOOTSVANILLACAPENULLGEKBASEGEKHEAD4GLOVESVANILLALEGSVANILLATORSOVANILLAHEADC09D70A1F2D064A25D4770TORSO3C90DDA13C90DDA23C90DDARMOUR3C90DDA13C90DDA23C90DDBACKPACK3C90DDA13C90DDA23C90DDHANDS3C90DDA13C90DDA23C90DDLEGS3C90DDA13C90DDA23C90DDFEET3C90DDA13C90DDA23C90DDARMOURVANILLA1BACKPACKBACKPACK1BOOTSVANILLA1GLOVESVANILLA1HEADGBEAK3HEADGMARKSCALESHEADGTOPALT5LEGSVANILLA1PUPILSTXT3TORSOVANILLA110020010";
    internal const string TAG_GEK_1 = "ARMOURASTROBACKPACKVANILLBOOTSASTROCAPENULLGEKBASEGEKHEAD1GLOVESASTROLEGSASTROTORSOFOURTHHEAD3D7A57A1FF8400A251BB7ETORSO51BB7EA151BB7EA2F2D064ARMOURFF8400A151BB7EA251BB7EBACKPACK51BB7EA1FF8400A251BB7EHANDS51BB7EA151BB7EA2FF8400LEGS51BB7EA151BB7EA251BB7EFEET51BB7EA1FF8400A2FF8400ARMOURASTROCHESTARMOUR2BACKPACKBACKPACK3BOOTSASTROBOOTS4GLOVESASTROGLOVES0HEADGBEAK3HEADGMARKSCALESHEADGTOPALT5LEGSASTROLEGS0PUPILSTXT5TORSOFOURTH010020010";
    internal const string TAG_GEK_2 = "ARMOURASTROBACKPACKVANILLBOOTSVANILLACAPENULLGEKBASEGEKHEAD2GLOVESFOURTHLEGSVANILLATORSOFOURTHHEAD5D4770A15D4770A25D4770TORSO000000A1FFFFFFA2FFFFFFARMOUR000000A1FFFFFFA2000000BACKPACKFFFFFFA1000000A2FFFFFFHANDSFFFFFFA1000000A2000000LEGSFFFFFFA1000000A2000000FEETFFFFFFA1000000A2000000ARMOURASTROCHESTARMOUR1BACKPACKBACKPACK3BOOTSVANILLA1GLOVESFOURTH1HEADGBEAK4HEADGMARKBONESHEADGTOPALT7LEGSVANILLA1PUPILSTXT1TORSOFOURTH110050010";
    internal const string TAG_GEK_3 = "ARMOURGEKBACKPACKVANILLBOOTSFOURTHCAPENULLGEKBASEGEKHEAD9GLOVESFOURTHLEGSFOURTHTORSOGEKHEADFFFFFFA13C90DDA2000000TORSO5D4770A15D4770A25D4770ARMOUR5D4770A15D4770A25D4770BACKPACK5D4770A15D4770A25D4770HANDS5D4770A15D4770A25D4770LEGS5D4770A15D4770A25D4770FEET5D4770A15D4770A25D4770ARMOURGEK1BACKPACKBACKPACK3BOOTSFOURTH1GLOVESFOURTH1HEADGBEAK5HEADGMARKSCALESHEADGTOPALT6LEGSFOURTH1PUPILSTXT5TORSOGEK110060010";
    internal const string TAG_GEK_4 = "ARMOURASTROBACKPACKVANILLBOOTSASTROCAPENULLGEKBASEGEKHEAD5GLOVESASTROLEGSASTROTORSOASTROHEADF2D064A151BB7EA24B4B4BTORSO803939A1F2D064A2803939ARMOUR803939A1F2D064A2803939BACKPACK803939A1F2D064A2803939HANDS803939A1F2D064A2803939LEGS803939A1F2D064A2803939FEET803939A1F2D064A2803939ARMOURASTROCHESTARMOUR2BACKPACKBACKPACK3BOOTSASTROBOOTS3GLOVESASTROGLOVES3HEADGBEAK1HEADGMARKBONESHEADGTOPALT5LEGSASTROLEGS4PUPILSTXT6TORSOASTROTORSO310040010";
    internal const string TAG_GEK_5 = "ARMOURGEKBACKPACKVANILLBOOTSVANILLACAPENULLGEKBASEGEKHEAD10GLOVESVANILLALEGSFOURTHTORSOVANILLAHEADE484C1A1FF8400A2E484C1TORSO3B4A66A13D7A57A23B4A66ARMOUR3B4A66A1FFFFFFA2FFFFFFBACKPACK3B4A66A1FFFFFFA2FFFFFFHANDS3B4A66A1FFFFFFA2FFFFFFLEGSFFFFFFA13B4A66A23B4A66FEET3B4A66A1FFFFFFA2FFFFFFARMOURGEK1BACKPACKBACKPACK1BOOTSVANILLA1GLOVESVANILLA0HEADGBEAK3HEADGMARKSCALESHEADGTOPALT6LEGSFOURTH1PUPILSTXT2TORSOVANILLA010040010";
    internal const string TAG_GEK_6 = "ARMOURFOURTHBACKPACKVANILLBOOTSFOURTHCAPENULLGEKBASEGEKHEAD7GLOVESFOURTHLEGSGEKTORSOVANILLAHEADFF8400A14B4B4BA2FF8400TORSOE484C1A1E484C1A2E484C1ARMOURE484C1A13D7A57A2E484C1BACKPACKE484C1A1E484C1A2E484C1HANDSE484C1A1E484C1A2E484C1LEGS8E546CA1E484C1A28E546CFEETE484C1A13D7A57A2E484C1ARMOURFOURTH0BACKPACKBACKPACK0BOOTSFOURTH0GLOVESFOURTH0HEADGBEAK1HEADGMARKBONESHEADGTOPALT1LEGSGEK0PUPILSTXT1TORSOVANILLA010090010";
    internal const string TAG_GEK_7 = "ARMOURVANILLABACKPACKVANILLBOOTSVANILLACAPENULLGEKBASEGEKHEAD3GLOVESASTROLEGSVANILLATORSOGEKHEAD3C90DDA1FF8400A251BB7ETORSO895D47A1F2D064A2895D47ARMOUR895D47A1F2D064A2895D47BACKPACK895D47A1F2D064A2895D47HANDS895D47A1F2D064A2895D47LEGS895D47A1F2D064A2895D47FEET895D47A1F2D064A2895D47ARMOURVANILLA1BACKPACKBACKPACK4BOOTSVANILLA0GLOVESASTROGLOVES2HEADGBEAK5HEADGMARKSCALESHEADGTOPALT4LEGSVANILLA1PUPILSTXT6TORSOGEK410040010";
    internal const string TAG_GEK_8 = "ARMOURGEKBACKPACKVANILLBOOTSGEKCAPENULLGEKBASEGEKHEAD6GLOVESGEKLEGSGEKTORSOGEKHEAD803939A1803939A2000000TORSO3D7A57A1C09D70A23D7A57ARMOUR3D7A57A1C09D70A23D7A57BACKPACK3D7A57A1C09D70A23D7A57HANDS3D7A57A1C09D70A23D7A57LEGS3D7A57A1C09D70A23D7A57FEET3D7A57A1C09D70A23D7A57ARMOURGEK1BACKPACKBACKPACK3BOOTSGEK1GLOVESGEK1HEADGBEAK3HEADGMARKSCALESHEADGTOPALT2LEGSGEK1PUPILSTXT2TORSOGEK410040010";
    internal const string TAG_VYKEEN_DEFAULT = "ARMOURVANILLABOOTSVANILLACAPENULLGLOVESVANILLALEGSVANILLATORSOVANILLAVYKEENBASEVYKEENHEAD1HEAD3C90DDA1803939A274C9BATORSO803939A1803939A2803939ARMOUR803939A1000000A2803939BACKPACK803939A1803939A2803939HANDS803939A1803939A2803939LEGS803939A1000000A2803939FEET803939A1803939A2803939ARMOURVANILLA1BACKPACKBACKPACK2HEADVTOP600090011";
    internal const string TAG_VYKEEN_0 = "ARMOURVANILLABACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESFOURTHLEGSFOURTHTORSOFOURTHVYKEENBASEVYKEENHEAD8HEADAAAAAAA13B4A66A23B4A66TORSO895D47A1895D47A2895D47ARMOUR895D47A1895D47A2895D47BACKPACK895D47A1895D47A2895D47HANDSAAAAAAA1895D47A2FFFFFFLEGS895D47A1895D47A2AAAAAAFEETFFFFFFA1895D47A2895D47ARMOURVANILLA1BACKPACKBACKPACK2HEADVTOP6TORSOFOURTH100090011";
    internal const string TAG_VYKEEN_1 = "ARMOURVANILLABACKPACKVANILLBOOTSASTROCAPENULLGLOVESASTROLEGSASTROTORSOASTROVYKEENBASEVYKEENHEAD7HEADFFFFFFA13B4A66A23B4A66TORSO3C90DDA1000000A23C90DDARMOUR3C90DDA13C90DDA23C90DDBACKPACK3C90DDA13C90DDA23C90DDHANDS3C90DDA13C90DDA23C90DDLEGS3C90DDA13B4A66A2FFFFFFFEET3C90DDA13C90DDA23C90DDARMOURVANILLA1BACKPACKBACKPACK2BOOTSASTROBOOTS0GLOVESASTROGLOVES0HEADVTOP6LEGSASTROLEGS0TORSOASTROTORSO200090011";
    internal const string TAG_VYKEEN_2 = "ARMOURVANILLABACKPACKVANILLBOOTSGEKCAPENULLGLOVESGEKLEGSGEKTORSOGEKVYKEENBASEVYKEENHEAD6HEADC09D70A13D7A57A2803939TORSO3D7A57A13D7A57A23D7A57ARMOUR3D7A57A1803939A23D7A57BACKPACK895D47A1895D47A2895D47HANDS803939A13D7A57A2803939LEGS3D7A57A1803939A23D7A57FEET000000A13B4A66A2803939ARMOURVANILLA1BACKPACKBACKPACK2BOOTSGEK1HEADVTOP6LEGSGEK100090011";
    internal const string TAG_VYKEEN_3 = "ARMOURASTROBACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESGEKLEGSVANILLATORSOFOURTHVYKEENBASEVYKEENHEAD9HEAD000000A1000000A2000000TORSO000000A1000000A2000000ARMOUR000000A1000000A2000000BACKPACK000000A1FFFFFFA2FFFFFFHANDS000000A1000000A2000000LEGS000000A1000000A2000000FEET000000A1000000A2000000ARMOURASTROCHESTARMOUR1BACKPACKBACKPACK1BOOTSFOURTH0HEADVTOP7LEGSVANILLA1TORSOFOURTH100000011";
    internal const string TAG_VYKEEN_4 = "ARMOURVANILLABACKPACKVANILLBOOTSVANILLACAPENULLGLOVESASTROLEGSVANILLATORSOASTROVYKEENBASEVYKEENHEAD3HEADF2D064A1F2D064A2DD6767TORSOF2D064A1F2D064A2DD6767ARMOURF2D064A1803939A2F2D064BACKPACKF2D064A1DD6767A2F2D064HANDSF2D064A1F2D064A2F2D064LEGSF2D064A1F2D064A2F2D064FEETF2D064A1F2D064A2803939ARMOURVANILLA1BACKPACKBACKPACK1GLOVESASTROGLOVES0HEADVTOP7TORSOASTROTORSO000000011";
    internal const string TAG_VYKEEN_5 = "ARMOURGEKBACKPACKVANILLBOOTSFOURTHCAPENULLGLOVESVANILLALEGSGEKTORSOGEKVYKEENBASEVYKEENHEAD2HEAD8E546CA15D4770A28E546CTORSO5D4770A15D4770A2803939ARMOUR000000A1000000A2000000BACKPACK000000A15D4770A2000000HANDS000000A15D4770A2000000LEGS5D4770A1000000A25D4770FEET5D4770A15D4770A25D4770BACKPACKBACKPACK2BOOTSFOURTH0GLOVESVANILLA1HEADVTOP1LEGSGEK100000011";
    internal const string TAG_VYKEEN_6 = "ARMOURFOURTHBOOTSFOURTHCAPENULLGLOVESFOURTHLEGSVANILLATORSOFOURTHVYKEENBASEVYKEENHEAD5HEADAD85CFA1AD85CFA2000000TORSOFFFFFFA1803939A2803939ARMOURFFFFFFA1803939A2803939BACKPACK803939A1803939A2803939HANDS000000A1AAAAAAA2DD6767LEGSFFFFFFA1803939A2AAAAAAFEETAAAAAAA1AAAAAAA2AAAAAAARMOURFOURTH1BACKPACKBACKPACK2BOOTSFOURTH0HEADVTOP5LEGSVANILLA1TORSOFOURTH100000011";
    internal const string TAG_VYKEEN_7 = "ARMOURASTROBOOTSVANILLACAPENULLGLOVESVANILLALEGSVANILLATORSOGEKVYKEENBASEVYKEENHEAD1HEAD3D6F70A18E546CA25D4770TORSO8E546CA1000000A2FFFFFFARMOUR000000A18E546CA2000000BACKPACKE484C1A1E484C1A2E484C1HANDS000000A18E546CA2000000LEGS8E546CA1000000A2000000FEET000000A18E546CA2000000ARMOURASTROCHESTARMOUR2BACKPACKBACKPACK2GLOVESVANILLA1HEADVTOP6LEGSVANILLA1TORSOGEK300080011";
    internal const string TAG_VYKEEN_8 = "ARMOURVANILLABOOTSGEKCAPENULLGLOVESGEKLEGSFOURTHTORSOASTROVYKEENBASEVYKEENHEAD6HEAD3C90DDA1C09D70A274C9BATORSO000000A13C90DDA2FFFFFFARMOUR3C90DDA1000000A2000000BACKPACK3C90DDA13C90DDA23C90DDHANDS3C90DDA13B4A66A2000000LEGS000000A13C90DDA2FF8400FEET000000A13B4A66A2000000ARMOURVANILLA1BACKPACKBACKPACK2BOOTSGEK1HEADVTOP6TORSOASTROTORSO300080011";
    internal const string TAG_FOURTH_DEFAULT = "ARMOURFOURTHBOOTSFOURTHCAPENULLFOURTHBASEFOURTHHEAD1GLOVESFOURTHLEGSFOURTHTORSOFOURTHHEADE484C1A1000000A2AAAAAATORSO8E546CA1AD85CFA2C09D70ARMOUR8E546CA18E546CA2FFFFFFBACKPACK8E546CA18E546CA28E546CHANDSFFFFFFA18E546CA28E546CLEGS8E546CA1AD85CFA28E546CFEETFFFFFFA18E546CA28E546CARMOURFOURTH0BACKPACKBACKPACK3BOOTSFOURTH0GLOVESFOURTH1LEGSFOURTH1TORSOFOURTH000030010";
    internal const string TAG_FOURTH_0 = "ARMOURFOURTHBOOTSASTROCAPENULLFOURTHBASEFOURTHHEAD2GLOVESASTROLEGSASTROTORSOASTROHEADFF8400A1FF8400A2AAAAAATORSO000000A1FF8400A2FFFFFFARMOURFF8400A1FF8400A2FF8400BACKPACKFF8400A1000000A2000000HANDSFF8400A1FF8400A2FF8400LEGSFF8400A1000000A2000000FEETFF8400A1000000A2FFFFFFARMOURFOURTH0BACKPACKBACKPACK2BOOTSASTROBOOTS2GLOVESASTROGLOVES0LEGSASTROLEGS2TORSOASTROTORSO300030010";
    internal const string TAG_FOURTH_1 = "ARMOURFOURTHBOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD3GLOVESVANILLALEGSVANILLATORSOVANILLAHEAD803939A1AAAAAAA2AAAAAATORSO4B4B4BA1000000A2000000ARMOUR000000A1000000A2000000BACKPACK000000A1000000A2000000HANDS000000A1000000A2000000LEGS000000A1000000A2000000FEET000000A1000000A2000000ARMOURFOURTH0BACKPACKBACKPACK2BOOTSVANILLA1GLOVESVANILLA1LEGSVANILLA1TORSOVANILLA100030010";
    internal const string TAG_FOURTH_2 = "ARMOURVANILLABOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD4GLOVESGEKLEGSGEKTORSOGEKHEAD803939A1AAAAAAA2AAAAAATORSOC09D70A1C09D70A2C09D70ARMOUR895D47A1000000A2000000BACKPACK000000A1895D47A2FF8400HANDS895D47A1000000A2000000LEGS895D47A1895D47A2895D47FEET000000A1000000A2000000ARMOURVANILLA1BACKPACKBACKPACK2BOOTSVANILLA1GLOVESGEK1LEGSGEK1TORSOGEK300030010";
    internal const string TAG_FOURTH_3 = "ARMOURGEKBOOTSGEKCAPENULLFOURTHBASEFOURTHHEAD5GLOVESGEKLEGSFOURTHTORSOFOURTHHEAD803939A1FFFFFFA2AAAAAATORSO803939A151BB7EA23D7A57ARMOUR3D7A57A13D7A57A2FFFFFFBACKPACK803939A1803939A2803939HANDS803939A1803939A2803939LEGS3D7A57A13D7A57A2DD6767FEET803939A1803939A2803939ARMOURGEK1BACKPACKBACKPACK2BOOTSGEK1GLOVESGEK1LEGSFOURTH1TORSOFOURTH000030010";
    internal const string TAG_FOURTH_4 = "ARMOURASTROBOOTSFOURTHCAPENULLFOURTHBASEFOURTHHEAD6GLOVESASTROLEGSGEKTORSOGEKHEAD3C90DDA13B4A66A2AAAAAATORSOFF8400A13C90DDA2FF8400ARMOUR3C90DDA13C90DDA2895D47BACKPACK3C90DDA1FF8400A23C90DDHANDSFF8400A1FF8400A2FF8400LEGS3C90DDA13C90DDA23C90DDFEET3C90DDA13B4A66A23C90DDARMOURASTROCHESTARMOUR0BACKPACKBACKPACK4BOOTSFOURTH0GLOVESASTROGLOVES0LEGSGEK1TORSOGEK100030010";
    internal const string TAG_FOURTH_5 = "ARMOURASTROBOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD7GLOVESGEKLEGSASTROTORSOFOURTHHEAD5D4770A151BB7EA2AAAAAATORSO74C9BAA174C9BAA274C9BAARMOUR5D4770A15D4770A25D4770BACKPACK5D4770A13D6F70A274C9BAHANDS3D6F70A15D4770A23D6F70LEGS5D4770A15D4770A2000000FEET5D4770A15D4770A25D4770ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK1BOOTSVANILLA1GLOVESGEK1LEGSASTROLEGS2TORSOFOURTH000030010";
    internal const string TAG_FOURTH_6 = "ARMOURVANILLABOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD2GLOVESGEKLEGSVANILLATORSOFOURTHHEAD3C90DDA13C90DDA2AAAAAATORSO3C90DDA13C90DDA2803939ARMOUR803939A13C90DDA23C90DDBACKPACK803939A13B4A66A2FFFFFFHANDS3B4A66A1803939A2FF8400LEGS3C90DDA13B4A66A23B4A66FEETFFFFFFA1803939A2FFFFFFARMOURVANILLA1BACKPACKBACKPACK2BOOTSVANILLA1GLOVESGEK1LEGSVANILLA1TORSOFOURTH100030010";
    internal const string TAG_FOURTH_7 = "ARMOURASTROBOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD10GLOVESVANILLALEGSVANILLATORSOASTROHEADC09D70A13C90DDA2AAAAAATORSO3B4A66A1895D47A2895D47ARMOURF2D064A1F2D064A2895D47BACKPACK895D47A1895D47A2895D47HANDS895D47A1895D47A2895D47LEGS3B4A66A13B4A66A2895D47FEET895D47A1895D47A2895D47ARMOURASTROCHESTARMOUR0BACKPACKBACKPACK2BOOTSVANILLA1GLOVESVANILLA1LEGSVANILLA1TORSOASTROTORSO300030010";
    internal const string TAG_FOURTH_8 = "ARMOURVANILLABACKPACKVANILLBOOTSVANILLACAPENULLFOURTHBASEFOURTHHEAD3GLOVESGEKLEGSASTROTORSOGEKHEADFFFFFFA1FFFFFFA2AAAAAATORSOFFFFFFA1FFFFFFA2FFFFFFARMOURFFFFFFA1FFFFFFA2FFFFFFBACKPACKFFFFFFA1FFFFFFA2FFFFFFHANDSFFFFFFA1FFFFFFA2FFFFFFLEGSFFFFFFA1FFFFFFA2FFFFFFFEETFFFFFFA1FFFFFFA2FFFFFFARMOURVANILLA1BACKPACKBACKPACK2BOOTSVANILLA1GLOVESGEK1LEGSASTROLEGS2TORSOGEK100030010";

    #endregion

    #region Property

    public override string JsonPath // { get; }
    {
        get
        {
            if (_useMapping)
                return _index < 0 ? "PlayerStateData.CharacterCustomisationData[0].CustomData" : $"PlayerStateData.Outfits[{_index}]";

            return _index < 0 ? "6f=.l:j[0].wnR" : $"6f=.cf5[{_index}]";
        }
    }
    public override string Name // { get; set; }
    {
        get => Description;
        set => Description = value;
    }

    public override string Tag => GetTag(Data);

    #endregion

    #region Getter

    internal static new string GetTag(Dictionary<string, JToken?> data)
    {
        var builder = new StringBuilder();

        // DescriptorGroups
        builder.Append(data["Outfit"]!.GetValue<JArray>("SMP", "DescriptorGroups")!.ConcatOrderedValues());
        // Colours
        builder.Append(data["Outfit"]!.GetValue<JArray>("Aak", "Colours")!.ConcatColours());
        // TextureOptions
        foreach (var texture in data["Outfit"]!.GetValue<JArray>("T>1", "TextureOptions")!.OrderBy(t => t.GetValue<string>("@6c", "TextureOptionGroupName")))
        {
            builder.Append(texture.GetValue<string>("@6c", "TextureOptionGroupName"));
            builder.Append(texture.GetValue<string>("=Cv", "TextureOptionName"));
        }
        // BoneScales
        builder.Append(string.Concat(data["Outfit"]!.GetValue<JArray>("gsg", "BoneScales")!.Select(i => i.GetValue<double>("unY", "Scale").ToString("F1"))));
        // Scale
        builder.Append(data["Outfit"]!.GetValue<double>("unY", "Scale")!.ToString("F1"));

        var result = builder.ToAlphaNumericString();
        return result switch
        {
            TAG_START_01 => "START_01",
            TAG_START_02 => "START_02",
            TAG_START_03 => "START_03",
            TAG_START_04 => "START_04",
            TAG_ASTRO_DEFAULT => "ASTRO_DEFAULT",
            TAG_ASTRO_0 => "ASTRO_0",
            TAG_ASTRO_1 => "ASTRO_1",
            TAG_ASTRO_2 => "ASTRO_2",
            TAG_ASTRO_3 => "ASTRO_3",
            TAG_ASTRO_4 => "ASTRO_4",
            TAG_ASTRO_5 => "ASTRO_5",
            TAG_ASTRO_6 => "ASTRO_6",
            TAG_ASTRO_7 => "ASTRO_7",
            TAG_ASTRO_8 => "ASTRO_8",
            TAG_KORVAX_DEFAULT => "KORVAX_DEFAULT",
            TAG_KORVAX_0 => "KORVAX_0",
            TAG_KORVAX_1 => "KORVAX_1",
            TAG_KORVAX_2 => "KORVAX_2",
            TAG_KORVAX_3 => "KORVAX_3",
            TAG_KORVAX_4 => "KORVAX_4",
            TAG_KORVAX_5 => "KORVAX_5",
            TAG_KORVAX_6 => "KORVAX_6",
            TAG_KORVAX_7 => "KORVAX_7",
            TAG_KORVAX_8 => "KORVAX_8",
            TAG_GEK_DEFAULT => "GEK_DEFAULT",
            TAG_GEK_0 => "GEK_0",
            TAG_GEK_1 => "GEK_1",
            TAG_GEK_2 => "GEK_2",
            TAG_GEK_3 => "GEK_3",
            TAG_GEK_4 => "GEK_4",
            TAG_GEK_5 => "GEK_5",
            TAG_GEK_6 => "GEK_6",
            TAG_GEK_7 => "GEK_7",
            TAG_GEK_8 => "GEK_8",
            TAG_VYKEEN_DEFAULT => "VYKEEN_DEFAULT",
            TAG_VYKEEN_0 => "VYKEEN_0",
            TAG_VYKEEN_1 => "VYKEEN_1",
            TAG_VYKEEN_2 => "VYKEEN_2",
            TAG_VYKEEN_3 => "VYKEEN_3",
            TAG_VYKEEN_4 => "VYKEEN_4",
            TAG_VYKEEN_5 => "VYKEEN_5",
            TAG_VYKEEN_6 => "VYKEEN_6",
            TAG_VYKEEN_7 => "VYKEEN_7",
            TAG_VYKEEN_8 => "VYKEEN_8",
            TAG_FOURTH_DEFAULT => "FOURTH_DEFAULT",
            TAG_FOURTH_0 => "FOURTH_0",
            TAG_FOURTH_1 => "FOURTH_1",
            TAG_FOURTH_2 => "FOURTH_2",
            TAG_FOURTH_3 => "FOURTH_3",
            TAG_FOURTH_4 => "FOURTH_4",
            TAG_FOURTH_5 => "FOURTH_5",
            TAG_FOURTH_6 => "FOURTH_6",
            TAG_FOURTH_7 => "FOURTH_7",
            TAG_FOURTH_8 => "FOURTH_8",
            _ => result,
        };
    }

    #endregion

    #region Setter

    protected override void SetData(JObject json)
    {
        Data = new()
        {
            { "Outfit", json.SelectDeepClonedToken(JsonPath) },
        };
    }

    #endregion

    // //

    #region Constructor

    public Outfit() : base() { }

    public Outfit(JObject json, int index) : base(json, index) { }

    #endregion

    // //

    #region Export

    public override void Export(JObject json, FormatEnum format, string path)
    {
        if (!OutfitCollection.SUPPORTED_FORMATS.Contains(format))
            throw new IndexOutOfRangeException("The specified format is not supported.");

        base.Export(json, format, path);
    }

    #endregion

    #region Import

    public override void Import(JObject json, int index)
    {
        _useMapping = json.UseMapping();

        // Apdat mapping in data to match with the JSON object.
        AdaptMapping(Data, _useMapping);

        if (Data.TryGetValue("Outfit", out var outfit) && outfit is not null)
        {
            if (_useMapping)
            {
                if (index < 0)
                {
                    json["PlayerStateData"]!["CharacterCustomisationData"]![0]!["SelectedPreset"] = "^";
                    json["PlayerStateData"]!["CharacterCustomisationData"]![0]!["CustomData"] = outfit;
                }
                else
                {
                    json["PlayerStateData"]!["Outfits"]![index] = outfit;
                }
            }
            else
            {
                if (index < 0)
                {
                    json["6f="]!["l:j"]![0]!["VFd"] = "^";
                    json["6f="]!["l:j"]![0]!["wnR"] = outfit;
                }
                else
                {
                    json["6f="]!["cf5"]![index] = outfit;
                }
            }
        }
    }

    #endregion

    protected override string GetExtension(FormatEnum format)
    {
        if (!OutfitCollection.SUPPORTED_FORMATS.Contains(format))
            return string.Empty;

        return ".ott";
    }

    protected override string GetFilename()
    {
        if (string.IsNullOrEmpty(Name))
        {
            // DescriptorGroups
            // Colours
            // TextureOptions
            // BoneScales
            // Scale
            return string.Join("_", Data["Outfit"]!.GetValue<JArray>("SMP", "DescriptorGroups")!.Select(d => d.Value<string>())).Remove("^");
        }
        return Name;
    }
}