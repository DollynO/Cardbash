using CardBase.Scripts.PlayerScripts;
using Godot;
using System.Collections.Generic;

namespace CardBase.Scripts.Abilities.Utility;

public class Blink : Ability
{
    private readonly List<Minion> activeAfterimages = new();
    private bool afterimageEnabled;

    public Blink(PlayerCharacter creator) : base(AbilityIds.BlinkGuid, creator)
    {
        this.DisplayName = "Blink";
        this.Description = "Teleports to the target position.";
        this.IconPath = "res://Sprites/SkillIcons/Lightning/9_Lightning_Strike.png";
    }

    public override void RoundReset()
    {
        ClearAfterimages();
    }

    public override void InternalUse()
    {
        if (Caller.TryGetComponent(out AimComponent aimComponent)
            && Caller.TryGetComponent(out MoveComponent moveComponent))
        {
            var startPosition = aimComponent.GetCharacterCenterPosition();
            var targetPosition = aimComponent.GetPlayerMouesPosition(ConfigParam("range", 400f));

            if (afterimageEnabled)
            {
                SpawnAfterimage(startPosition);
            }

            moveComponent.RequestReposition(targetPosition, ConfigParam("repositionDelay", 0f));
        }
    }

    public override void ClearAbility()
    {
        ClearAfterimages();
    }

    private void SpawnAfterimage(Vector2 position)
    {
        if (Caller is not Node2D callerNode)
        {
            return;
        }

        var scaleMultiplier = ConfigParam("afterimageScale", 1f);
        var life = ConfigParam("afterimageLife", 20f);
        if (Caller.TryGetComponent(out StatblockComponent statblock))
        {
            life *= 1 + statblock.GetStat(StatType.IncreasedMinionLife);
        }

        var afterimage = GlobalAbilitySpawner.SpawnMinion(new MinionSpawnStats
        {
            Owner = Caller,
            Kind = MinionKind.Afterimage,
            AbilityGUID = GUID,
            Position = position,
            Scale = callerNode.Scale * scaleMultiplier,
            AnimationOffset = new Vector2(
                ConfigParam("afterimageAnimationOffsetX", 0f),
                ConfigParam("afterimageAnimationOffsetY", 0f)),
            Duration = ConfigParam("afterimageDuration", 2.5f),
            Life = Mathf.Max(life, 1f),
            Alpha = ConfigParam("afterimageAlpha", 0.45f),
            CollisionRadius = ConfigParam("afterimageCollisionRadius", 12f),
            TeamId = Caller is ITeamAffiliation team ? team.TeamId : -1,
        });

        if (afterimage != null)
        {
            activeAfterimages.Add(afterimage);
            afterimage.TreeExited += () => activeAfterimages.Remove(afterimage);
        }
    }

    private void ClearAfterimages()
    {
        foreach (var afterimage in new List<Minion>(activeAfterimages))
        {
            if (GodotObject.IsInstanceValid(afterimage))
            {
                afterimage.Destroy();
            }
        }

        activeAfterimages.Clear();
    }

    protected override void ApplyUpdate1()
    {
        afterimageEnabled = true;
    }

    protected override void ApplyUpdate2()
    {
    }
}
