using System.Linq;
using System.Threading.Tasks;
using CardBase.Scripts.Abilities;
using Godot;

namespace CardBase.Scripts.Cards;

public partial class AbilityCard : Card
{
    public AbilityCard() : base(CardType.Ability)
    {
    }

    public override void ApplyEffect(IContext context)
    {
        if (context is not PlayerContext playerContext)
        {
            return;
        }

        if (playerContext.player != null && playerContext.player.TryGetComponent(out AbilityComponent abilityComponent))
        {
            abilityComponent.AddUpdateAbility(EffectGUID);
        }
    }
}