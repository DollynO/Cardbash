using System;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities.ProjectileBehavior;

public class SizeIncreaseBehavior : IProjectileBehavior
{
    private Projectile projectile;
    private float sizeIncrease;
    private float healthIncrease;
    private Vector2 baseScale;
    private string guid = Guid.NewGuid().ToString();
    
    public SizeIncreaseBehavior(float sizeIncrease, float healthIncrease = 1)
    {
        this.sizeIncrease = sizeIncrease;
        this.healthIncrease = healthIncrease;
    }

    public void AssignProjectile(Projectile projectile)
    {
        this.projectile = projectile;
        this.baseScale = this.projectile.Scale;
        
    }

    public void OnProcess(float deltaTime)
    {
        if (projectile != null)
        {
            CharacterBody2D body = projectile;
            body.Scale += this.baseScale * (sizeIncrease * deltaTime);
            if (projectile.TryGetComponent(out HealthComponent hc))
            {
                var sm = new StatModifier(guid, StatType.Life, StatOp.PercentMult, healthIncrease * deltaTime);
                hc.ApplyMod(sm);
            }
        }
    }

    public void OnDamagedReceived(Damage damage)
    {
        
    }
}