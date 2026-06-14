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
    private Dictionary<int, List<CardState>> serverHandCards = new();
    private List<CardState> handCards = new();
    public GameManager gameManager;
    
    public event EventHandler<DrawCardEventArgs> UpdateHandCardsEventHandler;
    
    public CardSystem(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void DrawCards(int amount)
    {
        if (!Multiplayer.IsServer()) return;
        
        serverHandCards.Clear();
        foreach (var player in gameManager.GetPlayers())
        {
            serverHandCards.Add((int)player.PlayerId, DrawCardsPlayer(amount, player));
            UpdateCardsServer((int)player.PlayerId, serverHandCards[(int)player.PlayerId]);
        }
    }

    private void UpdateCardsServer(int id, List<CardState> cards)
    {
        var dict = new Godot.Collections.Dictionary<string, bool>();
        foreach (var card in cards)
        {
            dict.Add(card.guid, card.locked);
        }

        RpcId(id, MethodName.UpdateCards, dict);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,  CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UpdateCards(Godot.Collections.Dictionary<string, bool> dict)
    {
        handCards.Clear();
        foreach (var card in dict)
        {
            handCards.Add(new CardState{guid = card.Key, locked = card.Value});
        }
        
        UpdateHandCardsEventHandler?.Invoke(this, new DrawCardEventArgs(handCards));
    }
    
    private List<CardState> DrawCardsPlayer(int amount, PlayerCharacter player)
    {
        player.Deck.Cards.Keys.Where(c => c.ExhaustionCount > 0).ToList().ForEach(c => c.ExhaustionCount--);

        var rng = new Random();
        var cards = new List<Card>();
        foreach (var deckCard in player.Deck.Cards.Where(c => c.Key.ExhaustionCount == 0))
        {
            for (var i = 0; i < deckCard.Value.Count; i++)
            {
                cards.Add(deckCard.Key);
            }
        }
        
        var handCards = new List<CardState>();
        if (lockedCards.TryGetValue(player, out var cardGuid))
        {
            handCards.Add(new CardState
            {
                guid = cardGuid,
                locked = true
            });
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
            handCards.Add(new CardState
            {
                guid = guid,
                locked = false
            });
            cards.RemoveAll(c => c.EffectGUID == guid);
        }

        return handCards;
    }

    public void LockCard(PlayerCharacter player, Card card)
    {
        if (card == null || player == null) return;
        
        Rpc(MethodName.lockCardServer, player.PlayerId, card.EffectGUID);
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void lockCardServer(int playerId, string cardGuid)
    {
        if (!Multiplayer.IsServer()) return;
        
        var player = gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;
        
        if (serverHandCards[playerId].FirstOrDefault(c => c.guid == cardGuid) is not { } state)
        {
            return;
        }

        // other card already locked
        if (lockedCards.ContainsKey(player) && serverHandCards[playerId].FirstOrDefault(c => c.guid == lockedCards[player]) is { } oldState)
        {
            oldState.locked = false;
            lockedCards[player] = cardGuid;
        }
        else
        {
            if (!gameManager.ScoreSystem.TryRemoveFromTeamScore(player, gameManager.Settings.CardLockCosts))
            {
                return;
            }

            lockedCards.TryAdd(player, cardGuid);
        }

        state.locked = true;

        UpdateCardsServer(playerId, serverHandCards[playerId]);
    }

    public void UnlockCard(PlayerCharacter player, Card card)
    {
        if (card == null || player == null) return;
        
        Rpc(MethodName.unlockCardServer, player.PlayerId, card.EffectGUID);
    }
        
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void unlockCardServer(int playerId, string cardGuid)
    {
        if (!Multiplayer.IsServer()) return;
        
        var player = gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        if (serverHandCards[playerId].FirstOrDefault(c => c.guid == cardGuid) is not { } state)
        {
            return;
        }
        
        gameManager.ScoreSystem.AddToTeamScore(player, gameManager.Settings.CardLockCosts);
        lockedCards.Remove(player);
        state.locked = false;
        UpdateCardsServer(playerId, serverHandCards[playerId]);
    }

    private void NotifyCardUnlocked(string cardGuid)
    {
        EventBus.Instance.CardSystemEventBus.EmitCardUnlocked(new CardEventArgs(cardGuid));
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

public sealed class CardState
{
    public string guid;
    public bool locked;
}

public class DrawCardEventArgs : EventArgs
{
    public List<CardState> cards;
    
    public DrawCardEventArgs(List<CardState> cards)
    {
        this.cards = cards;
    }
}
