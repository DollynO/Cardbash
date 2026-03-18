using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public partial class RingTextureNode : Sprite2D
{
    private MultiplayerSynchronizer sync;
    
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

        sync = new MultiplayerSynchronizer();
        sync.Name = "SYNC";
        sync.RootPath = new NodePath("..");

        var config = new SceneReplicationConfig();
        var posPath = new NodePath(":position");
        config.AddProperty(posPath);
        config.PropertySetReplicationMode(
            posPath,
            SceneReplicationConfig.ReplicationMode.Always);
        config.PropertySetSpawn(posPath, true);
     
        var rotPath = new NodePath(":rotation");
        config.AddProperty(rotPath);
        config.PropertySetReplicationMode(
            rotPath,
            SceneReplicationConfig.ReplicationMode.Always);
        config.PropertySetSpawn(rotPath, true);

        sync.ReplicationConfig = config;
        AddChild(sync);
    }

    public override void _Process(double delta)
    {
    }

    public new void QueueFree()
    {
        base.QueueFree();
    }
}