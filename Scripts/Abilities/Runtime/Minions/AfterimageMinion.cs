namespace CardBase.Scripts.Abilities;

public partial class AfterimageMinion : Minion
{
    protected override MinionKind DefaultKind => MinionKind.Afterimage;
    protected override bool FadeOutOverLifetime => true;
}
