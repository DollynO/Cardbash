namespace CardBase.Scripts.Abilities.TriggerStrategy;

public interface ITriggerStrategy
{
    public void OnKeyJustPressed(Ability ability);
    public void OnKeyPressed(Ability ability, double delta);
    public void OnKeyReleased(Ability ability);
}

public class ChargeTriggerStrategy : ITriggerStrategy
{
    public void OnKeyJustPressed(Ability ability)
    {
        ability.Activate();
    }

    public void OnKeyPressed(Ability ability, double delta)
    {
        ability.Charge(delta);
    }

    public void OnKeyReleased(Ability ability)
    {
        ability.Use();
    }
}

public class SimpleTriggerStrategy : ITriggerStrategy
{
    public void OnKeyJustPressed(Ability ability)
    {
        if (ability.Activate())
        {
            ability.Use();
        }
    }

    public void OnKeyPressed(Ability ability, double delta)
    {
    }

    public void OnKeyReleased(Ability ability)
    {
    }
}