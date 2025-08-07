namespace CardBase.Scripts.Abilities;

public interface ICustomSpawnObject
{
    public long CreatorId { get; set; }
    public string AbilityGuid { get; set; }
}