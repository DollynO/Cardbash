using System;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Items;
using Godot;
using Godot.Collections;
using SGeneric = System.Collections.Generic;

namespace CardBase.Scripts.Cards;

public partial class GlobalCardManager : Node
{
    public static GlobalCardManager Instance => instance ??= new GlobalCardManager();
    private static GlobalCardManager instance;
    public Dictionary<string, AbilityCard> AbilityCards = new();
    public Dictionary<string, ItemCard> ItemCards = new();
    public Array<Deck> Decks = new();
    private string decks_filepath = "user://decks.json";

    public GlobalCardManager()
    {
        create_cards(AbilityCards, CardType.Ability, AbilityManager.Abilities);
        create_cards(ItemCards, CardType.Item, ItemManager.Items);
        load_decks();
    }

    public void Load()
    {
        return;
    }
    
    private void create_cards<[MustBeVariant]T>(Dictionary<string, T> target, CardType type, SGeneric.Dictionary<string, Func<BaseCardableObject>> dict) where T : Card, new()
    {
        target.Clear();
        foreach (var entry in dict)
        {
            var item = entry.Value();
            
            var card = new T()
            {
                Description = item.Description,
                DisplayName = item.DisplayName,
                IconPath = item.IconPath,
                EffectGUID = item.GUID,
                CardType = type,
            };
            
            target.Add(card.EffectGUID, card);
        }
    }

    public void SaveDecks()
    {
        var decklist = new Array<Dictionary<string,Variant>>();
        foreach (var deck in Decks)
        {
          var deckJson = new Dictionary<string,Variant>();
          deckJson["name"] = deck.DisplayName;
          deckJson["icon"] = deck.IconNumber;
          deckJson["guid"] = deck.GUID == string.Empty ? Guid.NewGuid().ToString() : deck.GUID;
          var cardsJson = new Dictionary<string, int>();
          deckJson["cards"] = cardsJson;
          foreach (var card in deck.Cards)
          {
              cardsJson.Add(card.Key.EffectGUID, card.Value.Count);
          }
          decklist.Add(deckJson);  
        } 
        var deckListString = Json.Stringify(decklist);
        using var file = FileAccess.Open(decks_filepath, FileAccess.ModeFlags.Write);
        file.StoreString(deckListString);
    }

    private void load_decks()
    {
        Decks.Clear();
        using var file = FileAccess.Open(decks_filepath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return;    
        }
        
        var deckListString = file.GetAsText();
        var deckListJson = (Array<Dictionary<string,Variant>>)Json.ParseString(deckListString);
        foreach (var deckJson in deckListJson)
        {
            var deck = new Deck();
            deck.DisplayName = (string)deckJson["name"];
            deck.SetIcon((int)deckJson["icon"]);
            deck.GUID = (string)deckJson["guid"];
            deck.GUID = deck.GUID == string.Empty ? Guid.NewGuid().ToString() : deck.GUID;
            deck.Cards.Clear();
            foreach (var cardJson in (Dictionary<string, int>)deckJson["cards"])
            {
                var card = get_card_by_effect_guid(cardJson.Key);
                if (card != null)
                {
                    deck.Cards.Add(card, new Counter(cardJson.Value));
                }
            }
            Decks.Add(deck);
        }
        
    }

    private Card get_card_by_effect_guid(string effectguid)
    {
        AbilityCards.TryGetValue(effectguid, out var abilityCard);
        if (abilityCard != null)
        {
            return abilityCard;
        }
        
        ItemCards.TryGetValue(effectguid, out var itemCard);
        return itemCard ?? null;
    }

    public Card GetCard(string effectguid, CardType type)
    {
        return type switch
        {
            CardType.Ability => AbilityCards[effectguid],
            CardType.Item => ItemCards[effectguid],
            _ => null
        };
    }
}