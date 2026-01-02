using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class PoisonJab : Ability
{
    public PoisonJab(PlayerCharacter creator) : base(AbilityIds.PoisonJabGuid, creator)
    {
        this.DisplayName = "Poison Jab";
        this.Description = "Well poison jab";
        this.IconPath = "res://Sprites/SkillIcons/Poison/20_Poison_Bone.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Poison;
    }

    public override void InternalUse()
    {
        var stats = new AoeBaseStats()
        {
            Angle = 120,
            ActivationTime = 0.8f,
            Radius = 40,
            AngleOffset = 0,
            Owner = Caller,
            AbilityGUID = GUID,
            OnActivation = OnActivation,
        };
        globalAbilitySpawner.SpawnAoe(stats);
    }
    
    private void OnActivation(List<PlayerCharacter> arg1, AoeBase arg2)
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
            playerCharacter.ReceiveHit(hit);
        }
    }

    protected override void InternalUpdate()
    {
    }
}