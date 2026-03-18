using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class RingTextureNode : Sprite2D
{
    private float updateCounter = 0;
    private const float fixUpdateInterval = 0.25f;
    
    public void Init(Texture2D texture, Vector2 scale)
    {
        this.Texture = texture;
        this.Scale = scale;
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        updateCounter += (float)delta;
        if (updateCounter >= fixUpdateInterval)
        {
            updateCounter = 0;
            Rpc(MethodName.updatePosition, this.GlobalPosition, this.GlobalRotation);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updatePosition(Vector2 position, float rotation)
    {
        this.GlobalPosition = position;
        this.GlobalRotation = rotation;
    }

    public new void QueueFree()
    {
        Rpc(MethodName.destroyClients);
        base.QueueFree();
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void destroyClients()
    {
        QueueFree();
    }
}