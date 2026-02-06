namespace CardBase.Scripts.Abilities.ProjectileBehavior;

public interface IProjectileBehavior
{
    public void AssignProjectile(Projectile projectile);
    public void OnProcess(float deltaTime);
}