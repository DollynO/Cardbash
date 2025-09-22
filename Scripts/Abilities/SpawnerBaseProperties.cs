using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities;

public class SpawnerBaseProperties : IDictAble<SpawnerBaseProperties>
{
    public SpawnType SpawnType { get; set; }
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }

    public int SpawnCount;
    
    public float SpawnDelay;

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>
        {
            { nameof(SpawnType), (int)SpawnType },
            { nameof(CreatorId), CreatorId },
            { nameof(AbilityGuid), AbilityGuid },
            {nameof(SpawnDelay), SpawnDelay},
            {nameof(SpawnCount), SpawnCount}
        };
    }
	
    public static SpawnerBaseProperties FromDict(Dictionary<string, Variant> dict)
    {
        return new SpawnerBaseProperties
        {
            SpawnType = (SpawnType)(int)dict[nameof(SpawnType)],
            CreatorId = (long)dict[nameof(CreatorId)],
            AbilityGuid = (string)dict[nameof(AbilityGuid)],
            SpawnCount = (int)dict[nameof(SpawnCount)],
            SpawnDelay = (float)dict[nameof(SpawnDelay)],
        };
    }
}