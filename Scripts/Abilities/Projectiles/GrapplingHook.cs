using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Abilities;

public class GrapplingHookStats : ProjectileStats
{
}

public partial class GrapplingHook : Projectile
{
    [Export] private Line2D _line;
    private readonly List<Vector2> _points = new() {new  Vector2(0, 0)};
    private float _updateTime = 0.1f;
    private float _currentUpdateTime;
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        _currentUpdateTime += (float)delta;
        if (_currentUpdateTime >= _updateTime)
        {
            _currentUpdateTime = 0;
            var startLocal = ToLocal(this.stats.StartPosition);
            var endLocal = Vector2.Zero; // projectile's own position

            _line.Points = new Vector2[] { startLocal, endLocal };
        }

    }

    protected override void HitableObjectCollided(IHitableObject hitObject)
    {
        base.HitableObjectCollided(hitObject);
        if (hitObject is PlayerCharacter player)
        {
            player.MoveController.RequestReposition(this.stats.Caller.GetCharacterCenterPosition(), 1.0f);
        }
    }
}