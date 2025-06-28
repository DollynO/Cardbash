using System;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Cards;

public enum CardType {
    Ability = 0,
    Item,
    Spell,
    WorldModifier,
    Modifier,
}

public interface ICard : IBaseProperty
{
    public void ApplyEffect(IContext context);
}

public partial class Card : Node, ICard
{
    public CardType CardType { get; set; }
    public string EffectGUID { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string IconPath { get; set; }

    public Card()
    {
        
    }
    
    public Card(CardType type)
    {
        this.CardType = type;
    }

    public virtual void ApplyEffect(IContext context)
    {
        
    }

    public override void _Ready()
    {
        
    }

    public Dictionary ToDict()
    {
        var dict = new Dictionary();
        dict.Add("CardType", (int)CardType);
        dict.Add("EffectGUID", EffectGUID);
        dict.Add("DisplayName", DisplayName);
        dict.Add("Description", Description);
        dict.Add("IconPath", IconPath);
        return dict;
    }

    public static Card FromDict(Dictionary dict)
    {
        Card card = (CardType)(int)dict["CardType"] switch
        {
            CardType.Item => (ItemCard)GlobalCardManager.Instance.GetCard((string)dict["EffectGUID"], (CardType)(int)dict["CardType"]),
            CardType.Ability => (AbilityCard)GlobalCardManager.Instance.GetCard((string)dict["EffectGUID"], (CardType)(int)dict["CardType"]),
            CardType.Spell => throw new NotImplementedException(),
            CardType.WorldModifier => throw new NotImplementedException(),
            CardType.Modifier => throw new NotImplementedException(),
            _ => throw new Exception()
        };
        return card;
    }
}