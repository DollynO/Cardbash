using System;
using System.Collections.Generic;
using CardBase.Scripts.Cards;

namespace CardBase.Scripts.Items;

public class ItemManager
{
    public readonly static Dictionary<string, Func<BaseCardableObject>> Items = new Dictionary<string, Func<BaseCardableObject>>()
    {
        {"798BBD60-2903-41B1-927F-06A27007B282", ()=>new ChestArmor()},
        {"3DB74D75-0CDD-4B69-B38C-337ED4CD0A42", ()=>new EnergyCore()},
        {"166D1A7F-8322-420F-A0A8-EC8DBE9FFA54", ()=>new CritDagger()},
        {"E852D4A6-1630-4548-A23F-8570F21E41704", ()=>new SwiftBoots()},
    };
    
    public static BaseCardableObject Create(string guid)
    {
        return Items.TryGetValue(guid, out var constructor) ? constructor() : null;
    }
}