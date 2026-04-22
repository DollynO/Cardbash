using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class RingTextureNode : Sprite2D
{
    public void Init(Texture2D texture, Vector2 scale)
    {
        this.Texture = texture;
        this.Scale = scale;
    }

    public override void _Ready()
    {
        SetMultiplayerAuthority(1);
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
        }

        ReplicationHelper.CreateSynchronizer(this,
            new ReplicationProperties(":position", SceneReplicationConfig.ReplicationMode.Always),
            new ReplicationProperties(":rotation", SceneReplicationConfig.ReplicationMode.Always));
    }

    public override void _Process(double delta)
    {
    }

    public new void QueueFree()
    {
        base.QueueFree();
    }
}
