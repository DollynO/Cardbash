using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class AimComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public void SetParent(IEntityComponent component)
    {
        Parent = component;
    }

    private Node2D _lookAtDirectionPoint;
    private Vector2 _lookAtDirectionCorrection;
    private Node2D _characterCenterPoint;

    public AimComponent(Node2D centerPoint, Node2D lookAtDirection, Vector2 lookAtCorrection)
    {
        _characterCenterPoint = centerPoint;
        _lookAtDirectionPoint = lookAtDirection;
        _lookAtDirectionCorrection = lookAtCorrection;
        Name = "AimComponent";
    }

    public Vector2 GetLookAtDirection()
    {
        return (_lookAtDirectionPoint.GlobalPosition - _characterCenterPoint.GlobalPosition).Normalized();
    }

    public Vector2 GetProjectileStartPosition()
    {
        return _lookAtDirectionPoint.GlobalPosition;
    }

    public Vector2 GetCharacterCenterPosition()
    {
        return _characterCenterPoint.GlobalPosition;
    }

    public Node2D GetCharacterCenterPoint()
    {
        return _characterCenterPoint;
    }

    public Vector2 GetPlayerMouesPosition(float maxLength)
    {
        if (Parent is PlayerCharacter character)
        {
            var gmp = character.PlayerInput.ClientGlobalMousePosition;
            var direction = GetLookAtDirection();
            var distance = _characterCenterPoint.GlobalPosition - gmp;
            if (distance.Length() > maxLength)
            {
                return GetCharacterCenterPosition() + direction * maxLength;
            }

            return gmp;
        }

        return new Vector2(-10000, -10000);
    }
}