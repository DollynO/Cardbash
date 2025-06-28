using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Items;

namespace CardBase.Scripts.Cards;

public partial class ItemCard : Card
{
    public ItemCard() : base(CardType.Item)
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

        var item = ItemManager.Create(EffectGUID) as Item;
        item?.ApplyItem(playerContext.player);
    }
}