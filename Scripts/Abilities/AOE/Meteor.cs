using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities.AOE;

public class Meteor : Ability
{
    public Meteor(PlayerCharacter creator) : base(AbilityIds.MeteorGuid, creator)
    {
        this.DisplayName = "Meteor";
        this.Description = "Meteor";
        this.IconPath = "res://Sprites/SkillIcons/Fire/1_Meteorite.png";
        this.BaseCooldown = 5;
        this.BaseDamage = 75;
        this.BaseType = DamageType.Fire;
    }

    protected override void InternalUpdate()
    {
    }

    public override void RoundReset()
    {
        
    }

    public override void InternalUse()
    {
        Caller.TryGetComponent(out AimComponent aimComponent);
        
        var aoe = GlobalAbilitySpawner.SpawnAoe(new AoeBaseStats()
        {
            ActivationTime = 2f,
            Radius = 200,
            Duration = 0,
            Callbacks = new AoeBaseCallbacks  { OnActivation = OnActivation },
            Owner = Caller,
            AbilityGUID = GUID,
            StationaryPosition = aimComponent.GetPlayerMouesPosition(600),
            IsStationary = true,
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
}