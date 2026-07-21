using Godot;
using CardBase.Scripts.PlayerScripts;

namespace CardBase.Scripts.Abilities;

public static class CombatTargeting
{
    public static bool ShouldAbilityAffect(IEntityComponent source, IEntityComponent target)
    {
        if (target == null)
        {
            return false;
        }

        if (target is PlayerCharacter player && !player.IsTargetable)
        {
            return false;
        }

        if (source == null || IsFriendlyFireEnabled(source))
        {
            return true;
        }

        if (source == target)
        {
            return false;
        }

        if (source is ITeamAffiliation sourceTeam
            && target is ITeamAffiliation targetTeam
            && sourceTeam.TeamId >= 0
            && targetTeam.TeamId >= 0)
        {
            return sourceTeam.TeamId != targetTeam.TeamId;
        }

        return true;
    }

    public static bool IsFriendlyFireEnabled(IEntityComponent source)
    {
        return source is Node node && IsFriendlyFireEnabled(node);
    }

    public static bool IsFriendlyFireEnabled(Node context)
    {
        var gameManager = context.GetTree()?.Root.GetNodeOrNull<global::GameManager>("/root/Main/Game");
        return gameManager?.Settings.FriendlyFire ?? false;
    }
}
