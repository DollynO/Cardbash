using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class FireSlash : Ability
{
    public FireSlash(PlayerCharacter creator) : base(AbilityIds.FireSlashGuid, creator)
    {
        this.DisplayName = "Fire Slash";
        this.Description = "Melee fire strike. ";
        this.IconPath = "res://Sprites/SkillIcons/Fire/10_Fire_Tongue.png";
        this.BaseCooldown = 1;
        this.BaseDamage = 50;
        this.BaseType = DamageType.Fire;
    }

    public override void RoundReset()
    {
        return;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = 120,
            ActivationTime = 0.2f,
            Radius = 80,
            AngleOffset = 0,
            Owner = Caller,
            Callbacks = new AoeBaseCallbacks  { OnActivation = OnActivation },
        };
        GlobalAbilitySpawner.SpawnAoe(stats);
    }
    
    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {
        
        foreach (var playerCharacter in arg1)
        {
            var damage = new Damage
            {
                DamageNumber = (float)BaseDamage,
                AilmentChance = BaseAilmentChance,
                Type = BaseType
            };
            var damageDict = new Dictionary<DamageType, Damage>()
            {
                { BaseType, damage }
            };
            var ctx = new HitContext()
            {
                AbilityGuid = GUID,
                Target = playerCharacter,
                Damages = damageDict,
                Source = Caller,
            };
            var hit = new Hit(arg2, ctx);
            if (playerCharacter.TryGetComponent(out DamageAbleComponent dac))
            {
                dac.ReceiveHit(hit);
            }
        }
    }

    protected override void InternalUpdate()
    {
    }
}