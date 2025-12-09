using Godot;
using Godot.Collections;

namespace CardBase.Scripts.PlayerScripts;

public partial class MoveController : Node
{
    private PlayerCharacter character;
    
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
    
    public MoveController(PlayerCharacter character)
    {
        this.character = character;
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

        if (!character.IsDead())
        {
            if (repositionBlock)
            {
                character.GlobalPosition = _repositionPosition;
                return;
            }
            
            var moveVelocity = Vector2.Zero;
            if (!disableMove)
            {
                moveVelocity = direction * character.StatBlock.GetStat(StatType.MovementSpeed);
            }
            
            _forceDecayTime -= (float)delta;
            if (_forceDecayTime >= 0)
            {
                moveVelocity += _forceVector * (float)delta;
            }
            
            character.Velocity = moveVelocity + _impulsVector;
            if (_repositionPosition != Vector2.Zero)
            {
                character.GlobalPosition = _repositionPosition;
                _repositionPosition = Vector2.Zero;
            }
                
            character.MoveAndSlide();
        }
        else
        {
            character.Velocity = Vector2.Zero;
        }
    }

    public void RequestReposition(Vector2 globalPosition, float travelTime)
    {
        RpcId(1, MethodName.repositionServer, globalPosition, travelTime);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private async void repositionServer(Vector2 globalPosition, float travelTime)
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
            _repositionPosition = character.GlobalPosition;
            tween.TweenProperty(this, "_repositionPosition", globalPosition, travelTime)
                .SetTrans(Tween.TransitionType.Linear); 
            
            await ToSignal(tween, Tween.SignalName.Finished);
            repositionBlock = false;
            _repositionPosition = Vector2.Zero;
        }
    }

    public void RequestKnockback(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        var dict = new Dictionary<string, Variant>();
        dict.Add("sgp", sourceGlobalPosition);
        dict.Add("force", force);
        dict.Add("decayTime", decayTime);
        dict.Add("is_knockback", true);

        RpcId(1, MethodName.impulsServer, dict);
    }
    
    public void RequestForcePull(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        var dict = new Dictionary<string, Variant>();
        dict.Add("sgp", sourceGlobalPosition);
        dict.Add("force", force);
        dict.Add("decayTime", decayTime);
        dict.Add("is_knockback", false);
        
        RpcId(1, MethodName.impulsServer, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private async void impulsServer(Variant data)
    {
        if (Multiplayer.IsServer() && _impulsTween == null)
        {
            var dict = data.AsGodotDictionary<string, Variant>();
            var sourceGlobalPosition = (Vector2)dict["sgp"];
            var force = (float)dict["force"];
            var decayTime = (float)dict["decayTime"];
            var isKnockback = (bool)dict["is_knockback"];

            hitstun = true;
            _impulsStrength = force;
            var dir = isKnockback 
                ? (character.GetCharacterCenterPosition() - sourceGlobalPosition).Normalized() 
                : (sourceGlobalPosition - character.GetCharacterCenterPosition()).Normalized();
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

    public void RequestDrag(Vector2 sourceGlobalPosition, float force, float decayTime)
    {
        var dict = new Dictionary<string, Variant>
        {
            { "sgp", sourceGlobalPosition },
            { "force", force },
            { "decayTime", decayTime }
        };

        RpcId(1, MethodName.forceServer, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void forceServer(Variant data)
    {
        if (Multiplayer.IsServer() && _impulsTween == null)
        {
            var dict = data.AsGodotDictionary<string, Variant>();
            var sourceGlobalPosition = (Vector2)dict["sgp"];
            var force = (float)dict["force"];
            var decayTime = (float)dict["decayTime"];

            _forceStrength = force;
            var dir = (sourceGlobalPosition - character.GetCharacterCenterPosition()).Normalized();
            _forceVector = dir * _forceStrength;
            _forceDecayTime = decayTime;
        }     
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