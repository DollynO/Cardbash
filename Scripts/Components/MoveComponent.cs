using System;
using System.Collections.Generic;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class MoveComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public void SetParent(IEntityComponent component)
    {
        this.Parent = component;
        if (!Parent.TryGetComponent<HealthComponent>(out healthComponent) 
            || !Parent.TryGetComponent<StatblockComponent>(out statBlockComponent))
        {
            throw new ArgumentNullException();
        }
    }
    
    private HealthComponent healthComponent;
    private StatblockComponent statBlockComponent;
    
    private bool hitstun;
    public bool Stun { get; private set; }
    public float StunTime { get; private set; }

    public float MaxStunTime { get; private set; }

    private bool rooted;
    private bool disableMove => Stun || hitstun || rooted;
    private bool isStunned => Stun || hitstun;
    
    private Tween? _impulsTween;
    private Vector2 _impulsVector;
    private float _impulsStrength;
    private float _impulsDecayTime;

    private Vector2 _forceVector;
    private float  _forceStrength;
    private float _forceDecayTime;

    private Vector2 _repositionPosition;

    private bool repositionBlock;

    public MoveComponent()
    {
        Name = "MoveComponent";
    }
    
    public void ProcessMovement(double delta, Vector2 direction)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        if (Stun)
        {
            StunTime -= (float)delta;
            if (StunTime <= 0)
            {
                RemoveStun();
                
            }
        }
        
        if (!healthComponent.IsDead)
        {
            if (repositionBlock)
            {
                ((Node2D)Parent).GlobalPosition = _repositionPosition;
                return;
            }
            
            var moveVelocity = Vector2.Zero;
            if (!disableMove)
            {
                moveVelocity = direction * statBlockComponent.GetStat(StatType.MovementSpeed);
            }
            
            _forceDecayTime -= (float)delta;
            if (_forceDecayTime >= 0)
            {
                moveVelocity += _forceVector * (float)delta;
            }
            
            ((PlayerCharacter)Parent).Velocity = moveVelocity + _impulsVector;
            if (_repositionPosition != Vector2.Zero)
            {
                ((PlayerCharacter)Parent).GlobalPosition = _repositionPosition;
                _repositionPosition = Vector2.Zero;
            }
                
            ((PlayerCharacter)Parent).MoveAndSlide();
        }
        else
        {
            ((PlayerCharacter)Parent).Velocity = Vector2.Zero;
        }
    }

    public async void RequestReposition(Vector2 globalPosition, float travelTime)
    {
        if (!Multiplayer.IsServer() || repositionBlock)
        {
            return;
        }

        if (travelTime == 0)
        {
            _repositionPosition = globalPosition;
        }
        else
        {
            var tween = CreateTween();
            
            repositionBlock = true;
            _repositionPosition = ((PlayerCharacter)Parent).GlobalPosition;
            tween.TweenProperty(this, "_repositionPosition", globalPosition, travelTime)
                .SetTrans(Tween.TransitionType.Linear); 
            
            await ToSignal(tween, Tween.SignalName.Finished);
            repositionBlock = false;
            _repositionPosition = Vector2.Zero;
        }
    }

    public void Knockback(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        impulsServer(sourceGlobalPosition, force, decayTime, true);
    }
    
    public void ForcePull(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        impulsServer(sourceGlobalPosition, force, decayTime, false);
    }

    private async void impulsServer(Vector2 sgp, float force, float decayTime, bool isKnockback)
    {
        if (Multiplayer.IsServer() && _impulsTween == null)
        {
            hitstun = true;
            _impulsStrength = force;
            var pos = Parent.TryGetComponent(out AimComponent aimComponent) ? aimComponent.GetCharacterCenterPosition() : ((Node2D)Parent).GlobalPosition;
            var dir = isKnockback 
                ? (pos - sgp).Normalized() 
                : (sgp - pos).Normalized();
            _impulsVector = dir * _impulsStrength;
            
            _impulsTween = CreateTween();
            _impulsTween.TweenProperty(this, "_impulsVector", Vector2.Zero, decayTime)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out); 
            
            await ToSignal(_impulsTween, Tween.SignalName.Finished);
            
            _impulsTween = null;
            hitstun = false;
        }  
    }

    public void Drag(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        _forceStrength = force;
        var pos = Parent.TryGetComponent(out AimComponent aimComponent) ? aimComponent.GetCharacterCenterPosition() : ((Node2D)Parent).GlobalPosition;
        var dir = (sourceGlobalPosition - pos).Normalized();
        _forceVector = dir * _forceStrength;
        _forceDecayTime = decayTime;
    }

    public void ApplyRoot()
    {
        this.rooted =  true;    
    }

    public void RemoveRoot()
    {
        this.rooted = false;
    }

    public void ApplyStun(float duration)
    {
        if (this.Stun)
        {
            return;
        }

        this.Stun = true;
        this.MaxStunTime = duration;
        this.StunTime = duration;
    }

    public void RemoveStun()
    {
        this.Stun = false;
        this.StunTime = 0;
    }
    
}