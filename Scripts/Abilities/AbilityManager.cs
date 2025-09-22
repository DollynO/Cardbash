using System;
using System.Collections.Generic;
using CardBase.Scripts.Abilities.Utility;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities;

public static class AbilityManager
{
    public static Dictionary<string, Func<PlayerCharacter, BaseCardableObject>> Abilities = new()
    {
        // projectile
        { "EE277E3F-A8D2-4AE8-9DE4-01B8158DD000", creator => new FireballAbility(creator) },
        { "8E481FBF-DE0A-4673-BDF1-50EE9CC041D4", creator => new IceArrowAbility(creator) },

        // melee
        { "1DD05202-6BE7-489E-9411-CC968BF5BCB5", creator => new FireSlash(creator) },
        { "2970A4C5-0C86-4EB2-8CB3-067750622083", creator => new LightningStrike(creator) },
        { "C9BA2A80-C3D3-4EB7-B6E9-92D6020B62CE", creator => new ConsecratedSlash(creator) },
        { "DC432A01-3394-4AF4-BE1B-9289A4E88268", creator => new PoisonJab(creator) },
        { "92DF5DEB-48D7-48A7-B884-9DF358573157", creator => new DarkEdge(creator)},
        { "61B5DDB4-A447-4942-87F9-BBF93C3E125A", creator => new ShatterStrike(creator)},

        // util
        { "ECD03AF9-850B-457C-A799-2D9E99D29B7F", creator => new Blink(creator) }
    };

    public static BaseCardableObject Create(string GUID, PlayerCharacter creator)
        {
            return Abilities.TryGetValue(GUID, out var constructor) ? constructor(creator) : null;
        }
    }