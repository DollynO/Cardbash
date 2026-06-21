using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Aegis : Ability, IHitInterceptor
{
    private Ring ring;
    private AoeBase detectRing;
    private float ringRadius = 50;
    private List<IEntityComponent> charactersInRange = new();
    private bool isCharInRange => charactersInRange.Count > 0;

    private Color emptyColor = new(0f, 1f, 0f);
    private Color occupiedColor = new(1f, 0f, 0f);

    private Vector2 spriteScale = new(0.2f, 0.2f);
    private string shieldPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";

    private AegisDamageIncreaseBuff buff;

    public Aegis(IEntityComponent creator) : base(AbilityIds.AegisGuid, creator)
    {
        DisplayName = "Aegis";
        Description = "AAAEEEGIIIS";
        IconPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";
        AutoCast = true;

        if (creator != null
            && creator.TryGetComponent(out AbilityComponent abilityComponent)
            && creator.TryGetComponent(out DamageAbleComponent dac))
        {
            ringRadius = ConfigParam("ringRadius", ringRadius);
            var ringScale = ConfigParam("ringScale", 0.5f);
            var maxCharges = ConfigParam("maxCharges", 3);
            ring = abilityComponent.RingContainer.AddRing(ringRadius, ringScale, maxCharges);
            dac._HitInterceptors.Add(this);
            buff = new AegisDamageIncreaseBuff(creator, creator);
        }
    }

    public override void InternalUse()
    {
        if (detectRing == null)
        {
            var stats = new AoeBaseStats()
            {
                Radius = ConfigParam("ringRadius", ringRadius),
                ActivationTime = ConfigParam("activationTime", 0.1f),
                Duration = ConfigParam("duration", -1f),
                Callbacks = new AoeBaseCallbacks
                {
                    OnActivation = OnActivation,
                    OnEntityEnter = OnPlayerEnter,
                    OnEntityExit = OnPlayerExit,
                },
                AbilityGUID = GUID,
                IsStationary = false,
                Owner = Caller,
            };
            detectRing = GlobalAbilitySpawner.SpawnAoe(stats);
        }

        var scale = ConfigParam("shieldScale", spriteScale.X);
        ring.AddTextureNode(shieldPath, new Vector2(scale, scale));
    }

    private void OnPlayerExit(IEntityComponent arg1, AoeBase arg2)
    {
        if (charactersInRange.Contains(arg1))
        {
            charactersInRange.Remove(arg1);
        }

        if (!isCharInRange)
        {
            detectRing.ChangeFillColor(emptyColor);
        }
    }

    private void OnPlayerEnter(IEntityComponent arg1, AoeBase arg2)
    {
        if (!charactersInRange.Contains(arg1))
        {
            charactersInRange.Add(arg1);
        }

        if (isCharInRange)
        {
            detectRing.ChangeFillColor(occupiedColor);
        }
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {
        charactersInRange = arg1;

        if (isCharInRange)
        {
            detectRing.ChangeFillColor(occupiedColor);
        }
    }

    public override void RoundReset()
    {
        return;
    }

    protected override bool preventAutoCast()
    {
        return ring.GetStackCount() == ring.MaxStacks || isCharInRange;
    }


    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }

    public bool TryBlock(in Hit hit)
    {
        var stackCount = ring.GetStackCount();
        if (hit.Source is Projectile projectile && stackCount > 0)
        {
            ring.RemoveNodeAtSlot(stackCount - 1);
            if (buff != null && Caller.TryGetComponent(out BuffManagerComponent buffManagerComponent))
            {
                buffManagerComponent.ApplyBuff(buff);
            }

            return true;
        }

        return false;
    }
}
