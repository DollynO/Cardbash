using System;
using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities;

public static class AbilityManager
{
    public static Dictionary<string, Func<PlayerCharacter, BaseCardableObject>> Abilities = new()
    {
        {"EE277E3F-A8D2-4AE8-9DE4-01B8158DD000", creator=>new FireballAbility(creator)},
        {"8E481FBF-DE0A-4673-BDF1-50EE9CC041D4", creator => new IceArrowAbility(creator)},
        {"1DD05202-6BE7-489E-9411-CC968BF5BCB5", creator => new FireSlash(creator)},
    };
    
    public static BaseCardableObject Create(string GUID, PlayerCharacter creator)
    {
        return Abilities.TryGetValue(GUID, out var constructor) ? constructor(creator) : null;
    }
}