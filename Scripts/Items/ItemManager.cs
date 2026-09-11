using System;
using System.Collections.Generic;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.Items.Converter;

namespace CardBase.Scripts.Items;

public class ItemManager
{
    public readonly static Dictionary<string, Func<BaseCardableObject>> Items = new Dictionary<string, Func<BaseCardableObject>>()
    {
        {ItemIds.ChestArmorGuid, ()=>new ChestArmor()},
        {ItemIds.EnergyCoreGuid, ()=>new EnergyCore()},
        {ItemIds.LifeArmourGuid, ()=>new LifeArmour()},
        {ItemIds.ShamanPuppetGuid, ()=>new ShamanPuppet()},
        {ItemIds.CritDagger, ()=>new CritDagger()},
        {ItemIds.SwiftBoots, ()=>new SwiftBoots()},
        {ItemIds.DarknessOrbGuid, () =>new DarknessOrb()},
        {ItemIds.FireOrbGuid, () =>new FireOrb()},
        {ItemIds.HolyOrbGuid, () =>new HolyOrb()},
        {ItemIds.IceOrbGuid, () =>new IceOrb()},
        {ItemIds.LightningOrbGuid, () =>new LightningOrb()},
        {ItemIds.PhysicalOrbGuid, () =>new PhysicalOrb()},
        {ItemIds.PoisonOrbGuid, () =>new PoisonOrb()},
        {ItemIds.FireFrostConverterGuid, () =>new FireFrostConverter()},
        {ItemIds.FireLightningConverterGuid, () =>new FireLightningConverter()},
        {ItemIds.FrostFireConverterGuid, () =>new FrostFireConverter()},
        {ItemIds.FrostLightningConverterGuid, () =>new FrostLightningConverter()},
        {ItemIds.LightningFrostConverterGuid, () =>new LightningFrostConverter()},
        {ItemIds.LightningFireConverterGuid, () =>new LightningFireConverter()},
        {ItemIds.FireDarknessConverterGuid, () =>new FireDarknessConverter()},
        {ItemIds.FireHolyConverterGuid, () =>new FireHolyConverter()},
        {ItemIds.FrostDarknessConverterGuid, () =>new FrostDarknessConverter()},
        {ItemIds.FrostHolyConverterGuid, () =>new FrostHolyConverter()},
        {ItemIds.LightningDarknessConverterGuid, () =>new LightningDarknessConverter()},
        {ItemIds.LightningHolyConverterGuid, () =>new LightningHolyConverter()},
    };

    public static BaseCardableObject Create(string guid)
    {
        if (!Items.TryGetValue(guid, out var constructor))
        {
            return null;
        }

        var item = constructor();
        GameplayConfigManager.ApplyToItem(item as Item);
        return item;
    }
}

public static class ItemIds
{
    public static string ChestArmorGuid = "798BBD60-2903-41B1-927F-06A27007B282";
    public static string EnergyCoreGuid = "3DB74D75-0CDD-4B69-B38C-337ED4CD0A42";
    public static string LifeArmourGuid = "F1FCB766-E974-464D-9FA1-BB6506156E08";
    public static string ShamanPuppetGuid = "D9ED349D-1774-406D-9565-E419C70A1F53";
    public static string CritDagger = "166D1A7F-8322-420F-A0A8-EC8DBE9FFA54";
    public static string SwiftBoots = "E852D4A6-1630-4548-A23F-8570F21E41704";
    public static string FireOrbGuid = "7C413FE5-8AA4-4BAE-B7DE-134EB160DFCB";
    public static string DarknessOrbGuid = "8B005B38-934B-455F-BC82-404D7A8E4B95";
    public static string HolyOrbGuid = "24168D82-FCF4-4EBE-BB9E-CD6F377C2D95";
    public static string IceOrbGuid = "794C949A-0F0D-4DB4-8FB4-EED63B388E5F";
    public static string LightningOrbGuid = "80EF0A7B-D41C-4E07-AFF9-97578ABB48B0";
    public static string PhysicalOrbGuid = "1E6D23C1-02C5-4F0C-B73A-1BF56986C72F";
    public static string PoisonOrbGuid = "67868616-1BC8-4C53-A8DE-2C9BFA16E719";
    public static string FireFrostConverterGuid = "DC0A184A-8936-497E-BE0D-9EB3922A51AE";
    public static string FireLightningConverterGuid = "A09EFD9E-42B1-4D38-A6A2-61D79604D1E1";
    public static string FrostFireConverterGuid = "776192BB-6C76-4C32-B83A-7EF989F9DA23";
    public static string FrostLightningConverterGuid = "80E87A43-974B-4898-BFE8-8E01406D7C63";
    public static string LightningFrostConverterGuid = "0A47CB26-57EA-4FD5-8E55-18504072EA0B";
    public static string LightningFireConverterGuid = "13A59827-B8D4-41AB-81F9-E9ECEFF76092";
    public static string FireDarknessConverterGuid = "FCD60C75-10C3-403B-8908-B7F10C172EBD";
    public static string FireHolyConverterGuid = "0F1428E4-8C1B-414D-A15F-E5C01C60B97B";
    public static string FrostDarknessConverterGuid = "4689C803-2434-405F-8852-40C3F777D395";
    public static string FrostHolyConverterGuid = "6261C25A-232F-4E48-9BB6-38B4320B55C6";
    public static string LightningDarknessConverterGuid = "38941EC6-DD59-40A3-9F4F-CE9CFA3ED0B2";
    public static string LightningHolyConverterGuid = "1E0FC7DC-FE75-4452-ABAA-58E99E45337A";
}
