using Godot;

namespace CardBase.Scripts;

public record ReplicationProperties(NodePath PropertyPath, SceneReplicationConfig.ReplicationMode ReplicationMode)
{
    public readonly NodePath PropertyPath = PropertyPath;
    public readonly SceneReplicationConfig.ReplicationMode ReplicationMode = ReplicationMode;
}

public static class ReplicationHelper
{
    public static MultiplayerSynchronizer CreateSynchronizer(
        Node2D owner,
        params ReplicationProperties[] properties)
    {
        var sync = new MultiplayerSynchronizer()
        {
            Name = "sync",
            RootPath = owner.GetPath(),
        };

        var config = new SceneReplicationConfig();

        foreach (var property in properties)
        {
            config.AddProperty(property.PropertyPath);
            config.PropertySetReplicationMode(
                property.PropertyPath,
                property.ReplicationMode);
            config.PropertySetSpawn(property.PropertyPath,true);
        }
        
        sync.ReplicationConfig = config;
        owner.AddChild(sync);
        return sync;
    }
}