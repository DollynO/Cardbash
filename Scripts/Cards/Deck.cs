using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using CollectionExtensions = System.Collections.Generic.CollectionExtensions;

namespace CardBase.Scripts.Cards;

public partial class Counter : Resource
{
    public int Count { get; set; }

    public Counter()
    {
        Count = 1;
    }
    
    public Counter(int count)
    {
        Count = count;
    }
}

public enum DeckIconNumber
{
    Red = 0,
    Green = 1,
    Blue = 2,
    Violet = 3,
    Gold = 4,
}

public partial class Deck : Node
{
    public string DisplayName;
    public string GUID;
    public Texture2D Icon { get; private set; }
    public int IconNumber { get; private set; }
    public Dictionary<Card, Counter> Cards = new();

    public Dictionary ToDict()
    {
        var dict = new Dictionary();
        dict["dName"] = DisplayName;
        dict["Guid"] = GUID;
        dict["IconNumber"] = IconNumber;
        var cardDict = new Dictionary();
        foreach (var (card, counter) in Cards)
        {
            cardDict[card.EffectGUID] = (int)card.CardType * 1000 + counter.Count;
        }
        dict["Cards"] = cardDict;
        
        return dict;
    }

    public static Deck FromDict(Dictionary dict)
    {
        var deck = new Deck();
        deck.DisplayName = dict["dName"].ToString();
        deck.GUID = dict["Guid"].ToString();
        deck.IconNumber = (int)dict["IconNumber"];
        foreach (var cardPair in (Dictionary<string, int>)dict["Cards"])
        {
            var cardGUID = cardPair.Key;
            var cardType = cardPair.Value / 1000;
            var count = cardPair.Value % 1000;
            Card card = (CardType)cardType switch
            {
                CardType.Ability => GlobalCardManager.Instance.AbilityCards[cardGUID],
                CardType.Item => GlobalCardManager.Instance.ItemCards[cardGUID],
                _ => throw new Exception()
            };
            deck.Cards.Add(card, new Counter(count));
        }

        return deck;
    }
    
    public void LoadDeckFromJson(string json)
    {
        
    }

    public void AddCard(Card card)
    {
        if (!CollectionExtensions.TryAdd(Cards, card, new Counter(1)))
        {
            Cards[card].Count += 1;
        }
    }

    public bool RemoveCard(Card card)
    {
        if (Cards.TryGetValue(card, out var card1) && card1.Count > 0)
        {
            card1.Count -= 1;
            return false;
        }
        
        Cards.Remove(card);
        return true;
    }

    public void SetIcon(int number)
    {
        IconNumber = (int)number;
        Icon = GetDeckIcon(number);
    }

    public static Texture2D GetDeckIcon(int number)
    {
        return number switch
        {
            (int)DeckIconNumber.Red => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png"),
            (int)DeckIconNumber.Green => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png"),
            (int)DeckIconNumber.Blue => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png"),
            (int)DeckIconNumber.Violet => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png"),
            (int)DeckIconNumber.Gold => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png"),
            _ => GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png")
        };
    }
} 