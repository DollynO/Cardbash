using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class SpawnerBaseProperties : IDictAble<SpawnerBaseProperties>
{
    public SpawnType SpawnType { get; set; }
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>
        {
            { "spawn_type", (int)SpawnType },
            { "creator_id", CreatorId },
            { "ability_guid", AbilityGuid }
        };
    }
	
    public static SpawnerBaseProperties FromDict(Dictionary<string, Variant> dict)
    {
        return new SpawnerBaseProperties
        {
            SpawnType = (SpawnType)(int)dict["spawn_type"],
            CreatorId = (long)dict["creator_id"],
            AbilityGuid = (string)dict["ability_guid"],
        };
    }
}