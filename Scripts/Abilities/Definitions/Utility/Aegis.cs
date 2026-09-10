using System.Collections.Generic;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Aegis : PassiveStackAbility, IHitInterceptor
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
        Description = "Passively creates Aegis charges that block incoming projectiles.";
        IconPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";

        if (creator != null)
        {
            ringRadius = ConfigParam("ringRadius", ringRadius);
            var maxCharges = ConfigParam("maxCharges", 3);
            InitializePassiveStacks(maxCharges);

            if (creator.TryGetComponent(out AbilityComponent abilityComponent))
            {
                ring = abilityComponent.RingContainer.AddRing(
                    ringRadius,
                    ConfigParam("ringScale", 0.5f),
                    maxCharges);
            }

            if (creator.TryGetComponent(out DamageAbleComponent dac))
            {
                dac._HitInterceptors.Add(this);
            }

            buff = new AegisDamageIncreaseBuff(creator, creator);
        }
    }

    protected override bool CreatePassiveStack()
    {
        EnsureDetectRing();
        if (ring == null)
        {
            return false;
        }

        var scale = ConfigParam("shieldScale", spriteScale.X);
        ring.AddTextureNode(shieldPath, new Vector2(scale, scale));
        return true;
    }

    private void EnsureDetectRing()
    {
        if (detectRing != null && GodotObject.IsInstanceValid(detectRing))
        {
            return;
        }

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
            CanAffectOwner = false,
            Owner = Caller,
        };
        detectRing = GlobalAbilitySpawner.SpawnAoe(stats);
    }

    private void OnPlayerExit(IEntityComponent arg1, AoeBase arg2)
    {
        if (charactersInRange.Contains(arg1))
        {
            charactersInRange.Remove(arg1);
        }

        if (!isCharInRange)
        {
            detectRing?.ChangeFillColor(emptyColor);
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
            detectRing?.ChangeFillColor(occupiedColor);
        }
    }

    private void OnActivation(List<IEntityComponent> arg1, AoeBase arg2)
    {
        charactersInRange = arg1;

        if (isCharInRange)
        {
            detectRing?.ChangeFillColor(occupiedColor);
        }
    }

    public override void RoundReset()
    {
        base.RoundReset();
        charactersInRange.Clear();
        CancelDetectRing();
    }

    public override void ClearAbility()
    {
        base.ClearAbility();
        if (Caller != null && Caller.TryGetComponent(out DamageAbleComponent dac))
        {
            dac._HitInterceptors.Remove(this);
        }

        CancelDetectRing();
    }

    protected override bool CanCreatePassiveStack()
    {
        EnsureDetectRing();
        return ring != null && base.CanCreatePassiveStack() && !isCharInRange;
    }

    protected override void ClearPassiveStacks()
    {
        ring?.ClearNodes();
        base.ClearPassiveStacks();
    }

    protected override void ApplyUpdate1()
    {
    }

    protected override void ApplyUpdate2()
    {
    }

    public bool TryBlock(in Hit hit)
    {
        if (hit.Source is Projectile projectile && PassiveStackCount > 0)
        {
            var slotIndex = (ring?.GetStackCount() ?? 0) - 1;
            if (slotIndex >= 0)
            {
                ring.RemoveNodeAtSlot(slotIndex);
            }

            ConsumePassiveStack();
            if (buff != null && Caller.TryGetComponent(out BuffManagerComponent buffManagerComponent))
            {
                buffManagerComponent.ApplyBuff(buff);
            }

            return true;
        }

        return false;
    }

    private void CancelDetectRing()
    {
        if (detectRing != null && GodotObject.IsInstanceValid(detectRing))
        {
            detectRing.Cancel();
        }

        detectRing = null;
    }
}
