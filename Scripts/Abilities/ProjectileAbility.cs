using Godot;

namespace CardBase.Scripts.Abilities;

public partial class ProjectileAbility : Ability
{
    protected ProjectileStats ProjectileStats;
    private PackedScene ProjectileScene = GD.Load<PackedScene>("res://Scenes//Projectile.tscn");
    protected ProjectileAbility(string guid) : base(guid)
    {
    }

    protected virtual void InternalUse()
    {
        
    }
    
    public override void Use()
    {
        SpawnProjectile();
        InternalUse();
    }

    private void SpawnProjectile()
    {
        if (ProjectileStats == null)
        {
            return;
        }

        var projectile = ProjectileScene.Instantiate() as Projectile;
        if (projectile != null)
        {
            projectile.SetStats(ProjectileStats);
            projectile.GlobalPosition = Caller.GlobalPosition;
        }
        Caller.GetTree().Root.AddChild(projectile);
    }
}