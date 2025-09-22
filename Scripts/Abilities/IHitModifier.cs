using System.Collections.Generic;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public interface IHitModifier
{
    public void ApplyBefore(HitContext ctx);
    public void ApplyAfter(HitContext ctx);
}

public class HitContext
{
    public PlayerCharacter Source { get; set; }
    public string AbilityGuid { get; set; }
    public PlayerCharacter Target { get; set; }
    public Dictionary<DamageType, Damage> Damages { get; set; }

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        var dict = new Godot.Collections.Dictionary<string, Variant>();
        dict["source_id"] = Source.PlayerId;
        dict["target_id"] = Target.PlayerId;
        dict["ability_guid"] = AbilityGuid;
        var damageDict = new Godot.Collections.Dictionary<DamageType, Variant>();
        foreach (var dmg in Damages)
        {
            damageDict.Add(dmg.Key, dmg.Value.ToDict());
        }
        dict["damage"] = damageDict;
        return dict;
    }
    
    public static HitContext FromDict(Godot.Collections.Dictionary<string, Variant> dict, GameManager manager)
    {
        var ctx = new HitContext();
        ctx.Source = manager.GetPlayerCharacter((long)dict["source_id"]);
        ctx.Target = manager.GetPlayerCharacter((long)dict["target_id"]);
        ctx.AbilityGuid = (string)dict["ability_guid"];
        ctx.Damages = new System.Collections.Generic.Dictionary<DamageType, Damage>();
        foreach (var entry in (Godot.Collections.Dictionary<Variant, Variant>)dict["damage"])
        {
            ctx.Damages.Add((DamageType)(int)entry.Key,
                Damage.FromDict((Godot.Collections.Dictionary<string, Variant>)entry.Value));
        }
        return ctx;
    }
}