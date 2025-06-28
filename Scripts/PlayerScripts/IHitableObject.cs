namespace CardBase.Scripts.PlayerScripts;

public interface IHitableObject
{
    public void ApplyDamage(double damage, PlayerCharacter attacker);
}