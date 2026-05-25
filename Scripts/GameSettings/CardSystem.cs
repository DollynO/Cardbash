using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.GameSettings;

public partial class CardSystem : Node
{
    private Dictionary<PlayerCharacter, string> lockedCards = new();
    public GameManager gameManager;
    
    public CardSystem(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }
    
    public List<string> DrawCards(Deck deck, int amount, PlayerCharacter player)
    {
        deck.Cards.Keys.Where(c => c.ExhaustionCount > 0).ToList().ForEach(c => c.ExhaustionCount--);

        var rng = new Random();
        var cards = new List<Card>();
        foreach (var deckCard in deck.Cards.Where(c => c.Key.ExhaustionCount == 0))
        {
            for (var i = 0; i < deckCard.Value.Count; i++)
            {
                cards.Add(deckCard.Key);
            }
        }

        var handCards = new List<string>();
        if (lockedCards.TryGetValue(player, out var cardGuid))
        {
            handCards.Add(cardGuid);
            amount--;
            cards.RemoveAll(c => c.EffectGUID == cardGuid);
            lockedCards.Remove(player);
        }
        
        for (var i = 0; i < amount; i++)
        {
            if (cards.Count <= 0)
            {
                break;
            }

            var number = rng.NextInt64(0, cards.Count - 1);
            var guid = cards[(int)number].EffectGUID;
            handCards.Add(guid);
            cards.RemoveAll(c => c.EffectGUID == guid);
        }

        return handCards;
    }

    public void LockCard(PlayerCharacter player, Card card)
    {
        if (card == null || player == null) return;
        
        Rpc(MethodName.lockCardServer, player.PlayerId, card.EffectGUID);
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void lockCardServer(int playerId, string cardGuid)
    {
        if (!Multiplayer.IsServer()) return;
        
        var player = gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        if (!gameManager.ScoreSystem.TryRemoveFromPlayerScore(player, gameManager.Settings.CardLockCosts))
        {
            
        }
        
        lockedCards.TryAdd(player, cardGuid);
    }
    
    public void ServerApplyCards(List<string> cardGuids, PlayerCharacter player)
    {
        foreach (var cardGuid in cardGuids)
        {
            player.Deck.Cards.FirstOrDefault(c => c.Key.EffectGUID == cardGuid).Key.ExhaustionCount = 2;
            if (GlobalCardManager.Instance.AbilityCards.ContainsKey(cardGuid))
            {
                applyAbility(cardGuid, player);
            }
            else if (GlobalCardManager.Instance.ItemCards.TryGetValue(cardGuid, out var item))
            {
                applyItem(item, player);
            }
        }

    }

    private void applyAbility(string guid, PlayerCharacter player)
    {
        if (player.TryGetComponent(out AbilityComponent abilityComponent))
        {
            abilityComponent.AddUpdateAbility(guid);
        }
    }

    private void applyItem(ItemCard item, PlayerCharacter player)
    {
        item.ApplyEffect(new PlayerContext() { player = player });
    }
}