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
        { AbilityIds.FireballGuid, creator => new FireballAbility(creator) },
        { AbilityIds.IceArrowGuid, creator => new IceArrowAbility(creator) },
        { AbilityIds.GrapplingHookGuid, creator => new GrapplingHookAbility(creator) },
        { AbilityIds.CorruptedBoltGuid, creator => new CorruptedBolt(creator) },
        { AbilityIds.OrbitingIceShard, creator => new OrbitingIceShard(creator) },
        { AbilityIds.RicOSpamGuid, creator => new RicOSpam(creator) },

        // melee
        { AbilityIds.FireSlashGuid, creator => new FireSlash(creator) },
        { AbilityIds.LightningStrikeGuid, creator => new LightningStrike(creator) },
        { AbilityIds.ConsecratedSlashGuid, creator => new ConsecratedSlash(creator) },
        { AbilityIds.PoisonJabGuid, creator => new PoisonJab(creator) },
        { AbilityIds.DarkEdgeGuid, creator => new DarkEdge(creator) },
        { AbilityIds.ShatterStrikeGuid, creator => new ShatterStrike(creator) },
        { AbilityIds.PlagueburstGuid, creator => new Plagueburst(creator) },

        // util
        { AbilityIds.BlinkGuid, creator => new Blink(creator) },
        { AbilityIds.EscapeJumpGuid, creator => new EscapeJump(creator) },
        { AbilityIds.ShadowWalkGuid, creator => new ShadowWalk(creator) },
        { AbilityIds.AegisGuid, creator => new Aegis(creator)},
        
        // Ray
        { AbilityIds.ChargingBeamGuid, creator => new ChargingBeam(creator) },
    };

    public static BaseCardableObject Create(string GUID, PlayerCharacter creator)
    {
        return Abilities.TryGetValue(GUID, out var constructor) ? constructor(creator) : null;
    }
}

public static class AbilityIds
{
    public static string FireballGuid = "EE277E3F-A8D2-4AE8-9DE4-01B8158DD000";
    public static string IceArrowGuid = "8E481FBF-DE0A-4673-BDF1-50EE9CC041D4";
    public static string FireSlashGuid = "1DD05202-6BE7-489E-9411-CC968BF5BCB5";
    public static string LightningStrikeGuid = "2970A4C5-0C86-4EB2-8CB3-067750622083";
    public static string ConsecratedSlashGuid = "C9BA2A80-C3D3-4EB7-B6E9-92D6020B62CE";
    public static string PoisonJabGuid = "DC432A01-3394-4AF4-BE1B-9289A4E88268";
    public static string DarkEdgeGuid = "92DF5DEB-48D7-48A7-B884-9DF358573157";
    public static string ShatterStrikeGuid = "61B5DDB4-A447-4942-87F9-BBF93C3E125A";
    public static string BlinkGuid = "ECD03AF9-850B-457C-A799-2D9E99D29B7F";
    public static string EscapeJumpGuid = "B807C6E2-0061-4081-910B-1EB05891E500";
    public static string GrapplingHookGuid = "534F2A0B-3807-41F0-A05A-9955D5EF713B";
    public static string CorruptedBoltGuid = "278888C7-D0EC-49B9-AD64-BFB69C1B8348";
    public static string PlagueburstGuid = "4355909D-53F8-42E5-8E03-3647FEC6E8B5";
    public static string ShadowWalkGuid = "51741359-9A12-439B-99DA-5527407ACC71";
    public static string OrbitingIceShard = "10633462-9097-41ED-9331-6454F0AEAF93";
    public static string ChargingBeamGuid = "F760C46C-28B3-4F34-929B-7F140EB9C683";
    public static string AegisGuid = "C19D6DD4-E8D5-488B-A89C-58A20211BA26";
    public static string RicOSpamGuid = "F42A4786-A727-48B1-8CE7-9F26A222ED55";
    public static string SnowballGuid = "A40C5A0A-79D9-4B56-9897-87E61D23B902";
}