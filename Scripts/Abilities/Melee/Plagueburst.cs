using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs.DoTs;
using CardBase.Scripts.Abilities.HitMods;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Plagueburst : Ability
{
    public Plagueburst(PlayerCharacter creator) : base(AbilityIds.PlagueburstGuid, creator)
    {
        this.DisplayName = "Plagueburst";
        this.Description = "Bursts all corruption and agony buffs to cause damage.";
        this.IconPath = "res://Sprites/SkillIcons/Dark/15_Rod_of_Darkness.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 10;
        this.BaseType = DamageType.Darkness;
        this._hitModifiers.Add(new ConsumeBuffHitModifier(onBuffConsumed, typeof(Corruption), true));
        this._hitModifiers.Add(new ConsumeBuffHitModifier(onBuffConsumed, typeof(Agony), true));
    }

    private void onBuffConsumed(int count, HitContext context)
    {
        context.Damages[DamageType.Darkness].DamageNumber += (float)(count * BaseDamage);
    }

    public override void InternalUse()
    {
        globalAbilitySpawner.SpawnAoe(new AoeBaseStats()
        {
            ActivationTime = 3f,
            Radius = 100,
            Duration = 0,
            OnActivation = OnActivation,
            Owner = Caller,
            AbilityGUID = GUID,
            Angle = 90,
        });
    }

    private void OnActivation(List<IEntityComponent> playersHit, AoeBase source)
    {
        if (playersHit.Count > 0)
        {
            foreach (var player in playersHit)
            {
                    var damage = new Damage
                    {
                        DamageNumber = (float)BaseDamage,
                        AilmentChance = Damage.DEFAULT_AILMENT_CHANGE,
                        Type = BaseType
                    };
                    var damageDict = new Dictionary<DamageType, Damage> { { damage.Type, damage } };
                    var ctx = new HitContext()
                    {
                        AbilityGuid = GUID,
                        Source = Caller,
                        Target = player,
                        Damages = damageDict
                    };
                    var hit = new Hit(source, ctx);
                    if (player.TryGetComponent(out DamageAbleComponent dac))
                    {
                        dac.ReceiveHit(hit);
                    }
            }
        }
    }

    protected override void InternalUpdate()
    {
        
    }
}