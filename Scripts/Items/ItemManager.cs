using System;
using System.Collections.Generic;
using CardBase.Scripts.Cards;

namespace CardBase.Scripts.Items;

public class ItemManager
{
    public readonly static Dictionary<string, Func<BaseCardableObject>> Items = new Dictionary<string, Func<BaseCardableObject>>()
    {
        {ItemIds.ChestArmorGuid, ()=>new ChestArmor()},
        {ItemIds.EnergyCoreGuid, ()=>new EnergyCore()},
        {ItemIds.CritDagger, ()=>new CritDagger()},
        {ItemIds.SwiftBoots, ()=>new SwiftBoots()},
        {ItemIds.DarknessOrbGuid, () =>new DarknessOrb()},
        {ItemIds.FireOrbGuid, () =>new FireOrb()},
        {ItemIds.HolyOrbGuid, () =>new HolyOrb()},
        {ItemIds.IceOrbGuid, () =>new IceOrb()},
        {ItemIds.LightningOrbGuid, () =>new LightningOrb()},
        {ItemIds.PhysicalOrbGuid, () =>new PhysicalOrb()},
        {ItemIds.PoisonOrbGuid, () =>new PoisonOrb()},
    };
    
    public static BaseCardableObject Create(string guid)
    {
        return Items.TryGetValue(guid, out var constructor) ? constructor() : null;
    }
}

public static class ItemIds
{
    public static string ChestArmorGuid = "798BBD60-2903-41B1-927F-06A27007B282";
    public static string EnergyCoreGuid = "3DB74D75-0CDD-4B69-B38C-337ED4CD0A42";
    public static string CritDagger = "166D1A7F-8322-420F-A0A8-EC8DBE9FFA54";
    public static string SwiftBoots = "E852D4A6-1630-4548-A23F-8570F21E41704";
    public static string FireOrbGuid = "7C413FE5-8AA4-4BAE-B7DE-134EB160DFCB";
    public static string DarknessOrbGuid = "8B005B38-934B-455F-BC82-404D7A8E4B95";
    public static string HolyOrbGuid = "24168D82-FCF4-4EBE-BB9E-CD6F377C2D95";
    public static string IceOrbGuid = "794C949A-0F0D-4DB4-8FB4-EED63B388E5F";
    public static string LightningOrbGuid = "80EF0A7B-D41C-4E07-AFF9-97578ABB48B0";
    public static string PhysicalOrbGuid = "1E6D23C1-02C5-4F0C-B73A-1BF56986C72F";
    public static string PoisonOrbGuid = "67868616-1BC8-4C53-A8DE-2C9BFA16E719";
}