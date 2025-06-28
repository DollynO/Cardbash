using System;
using System.Collections.Generic;

namespace CardBase.Scripts.Abilities;

public static class AbilityManager
{
    public static Dictionary<string, Func<BaseCardableObject>> Abilities = new Dictionary<string, Func<BaseCardableObject>>()
    {
        {"EE277E3F-A8D2-4AE8-9DE4-01B8158DD000", ()=>new FireballAbility()},
        {"8E481FBF-DE0A-4673-BDF1-50EE9CC041D4", () => new IceArrowAbility()},
    };
    
    public static BaseCardableObject Create(string GUID)
    {
        return Abilities.TryGetValue(GUID, out var constructor) ? constructor() : null;
    }
}