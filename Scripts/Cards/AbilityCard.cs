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

        if (playerContext.player == null)
        {
            return;
        }
        
        var ability = playerContext.player.Abilities.FirstOrDefault(x => x.GUID == EffectGUID);
        if (ability == null)
        {
            ability = (Ability)AbilityManager.Create(EffectGUID);
            ability.SetCaller(playerContext.player);
            playerContext.player.Abilities.Add(ability);
        }
        else
        {
            ability.ApplyUpdate();
        }
    }
}