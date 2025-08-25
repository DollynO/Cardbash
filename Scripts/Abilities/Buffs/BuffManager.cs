using System;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.Buffs;


public static class BuffManager
{
    public static System.Collections.Generic.Dictionary<string, Func<PlayerCharacter, PlayerCharacter, Buff>> Abilities = new()
    {
        {"088DF86D-7101-4BBF-A331-17B625D11379", (creator, target)=>new BurnDebuff(creator, target)},
    };
    
    public static Buff Create(string GUID, PlayerCharacter creator, PlayerCharacter target)
    {
        return Abilities.TryGetValue(GUID, out var constructor) ? constructor(creator, target) : null;
    }
}