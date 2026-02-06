using Godot;

namespace CardBase.Scripts.Abilities.ProjectileBehavior;

public class SizeIncreaseBehavior : IProjectileBehavior
{
    private Projectile projectile;
    private float sizeIncrease;
    
    public SizeIncreaseBehavior(float sizeIncrease)
    {
        this.sizeIncrease = sizeIncrease;
    }

    public void AssignProjectile(Projectile projectile)
    {
        this.projectile = projectile;
    }

    public void OnProcess(float deltaTime)
    {
        if (projectile != null)
        {
            CharacterBody2D body = projectile;
            body.Scale += body.Scale * (sizeIncrease * deltaTime);
        }
    }
}