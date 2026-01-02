using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class Aegis : Ability, IHitInterceptor
{
    private Ring ring;
    
    public Aegis(PlayerCharacter creator) : base(AbilityIds.AegisGuid, creator)
    {
        DisplayName = "Aegis";
        Description = "AAAEEEGIIIS";
        IconPath = "res://Sprites/SkillIcons/Holy/15_Holy_Shield.png";
        MaxStack = 1;
        BaseCooldown = 5;
        BaseDamage = 0;
        BaseType = DamageType.Holy;
        BaseAilmentChance = 0;
        AutoCast =  true;
        
        if (creator != null)
        {
            ring = creator.RingContainer.AddRing(50, 0.5f, 3);
            creator._HitInterceptors.Add(this);
        }
    }

    public override void InternalUse()
    {
        var iconNode = new Sprite2D();
        iconNode.Scale = new Vector2(0.2f, 0.2f);
        iconNode.Texture = IconLoader.Instance.LoadImage("res://Sprites/SkillIcons/Holy/15_Holy_Shield.png");
        ring.AddNode(iconNode);
    }

    protected override bool preventAutoCast()
    {
        return ring.GetStackCount() == ring.MaxStacks;
    }

    protected override void InternalUpdate()
    {
        
    }

    public bool TryBlock(in Hit hit)
    {
        var stackCount = ring.GetStackCount();
        if (hit.Source is Projectile projectile && stackCount > 0)
        {
            ring.RemoveNodeAtSlot(stackCount);
            return true;
        }

        return false;
    }
}