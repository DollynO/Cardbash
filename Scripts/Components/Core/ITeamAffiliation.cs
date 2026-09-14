namespace CardBase.Scripts;

public interface ITeamAffiliation
{
    int TeamId { get; }
}

public enum MinionKind
{
    Afterimage,
    StationaryTower,
}

public interface ITargetableEntity
{
    bool IsTargetable { get; }
}

public interface IMinionEntity : IEntityComponent, ITeamAffiliation, ITargetableEntity
{
    IEntityComponent MinionOwner { get; }
    MinionKind MinionKind { get; }
}

public static class CombatCollisionLayers
{
    public const uint World = 1;
    public const uint Wall = 2;
    public const uint Player = 4;
    public const uint Projectile = 8;
    public const uint Minion = 16;

    public const uint TargetableEntities = Player | Minion;
    public const uint ProjectileTargets = TargetableEntities | Projectile;
}
