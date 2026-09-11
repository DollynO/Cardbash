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
    private Dictionary<int, Dictionary<string, int>> serverExhaustionCounts = new();
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
            SyncDeckExhaustion(player);
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
    
    private List<CardState> DrawCardsPlayer(
        int amount,
        PlayerCharacter player,
        bool includeLockedCard = true,
        bool advanceExhaustion = true,
        HashSet<string> excludedCardGuids = null)
    {
        var playerId = (int)player.PlayerId;
        if (advanceExhaustion)
        {
            AdvanceServerExhaustion(playerId, player);
        }

        var rng = new Random();
        var cards = new List<Card>();
        foreach (var deckCard in player.Deck.Cards.Where(c =>
                     GetServerExhaustionCount(playerId, c.Key.EffectGUID) == 0
                     && (excludedCardGuids == null || !excludedCardGuids.Contains(c.Key.EffectGUID))))
        {
            for (var i = 0; i < deckCard.Value.Count; i++)
            {
                cards.Add(deckCard.Key);
            }
        }
        
        var handCards = new List<CardState>();
        if (includeLockedCard && lockedCards.TryGetValue(player, out var cardGuid))
        {
            handCards.Add(new CardState
            {
                guid = cardGuid,
                locked = false
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

            var number = rng.NextInt64(0, cards.Count);
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

    public void RerollHand(PlayerCharacter player)
    {
        if (player == null) return;

        Rpc(MethodName.rerollHandServer, player.PlayerId);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void rerollHandServer(int playerId)
    {
        if (!Multiplayer.IsServer()) return;

        var senderId = Multiplayer.GetRemoteSenderId();
        if (senderId != 0 && senderId != playerId)
        {
            return;
        }

        var player = gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        if (!serverHandCards.TryGetValue(playerId, out var playerHand) || playerHand.Count == 0)
        {
            return;
        }

        var currentHandGuids = playerHand.Select(c => c.guid).ToHashSet();
        var rerolledHand = DrawCardsPlayer(
            playerHand.Count,
            player,
            includeLockedCard: false,
            advanceExhaustion: false,
            excludedCardGuids: currentHandGuids);
        if (rerolledHand.Count == 0)
        {
            return;
        }

        if (!gameManager.ScoreSystem.TryRemoveFromTeamScore(player, gameManager.Settings.CardRerollCosts))
        {
            return;
        }

        lockedCards.Remove(player);
        serverHandCards[playerId] = rerolledHand;
        UpdateCardsServer(playerId, serverHandCards[playerId]);
    }

    private Dictionary<string, int> GetServerExhaustionCounts(int playerId)
    {
        if (!serverExhaustionCounts.TryGetValue(playerId, out var exhaustionCounts))
        {
            exhaustionCounts = new Dictionary<string, int>();
            serverExhaustionCounts[playerId] = exhaustionCounts;
        }

        return exhaustionCounts;
    }

    private int GetServerExhaustionCount(int playerId, string cardGuid)
    {
        return GetServerExhaustionCounts(playerId).GetValueOrDefault(cardGuid, 0);
    }

    private void SetServerExhaustionCount(int playerId, string cardGuid, int count)
    {
        var exhaustionCounts = GetServerExhaustionCounts(playerId);
        if (count <= 0)
        {
            exhaustionCounts.Remove(cardGuid);
            return;
        }

        exhaustionCounts[cardGuid] = count;
    }

    private void AdvanceServerExhaustion(int playerId, PlayerCharacter player)
    {
        foreach (var card in player.Deck.Cards.Keys)
        {
            var count = GetServerExhaustionCount(playerId, card.EffectGUID);
            if (count > 0)
            {
                SetServerExhaustionCount(playerId, card.EffectGUID, count - 1);
            }
        }
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
        
        if (!serverHandCards.TryGetValue(playerId, out var playerHand) ||
            playerHand.FirstOrDefault(c => c.guid == cardGuid) is not { } state)
        {
            return;
        }

        // other card already locked
        if (lockedCards.ContainsKey(player) && playerHand.FirstOrDefault(c => c.guid == lockedCards[player]) is { } oldState)
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
        EventBus.Instance.CardSystemEventBus.EmitCardLocked(new CardEventArgs(cardGuid, player));

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

        if (!lockedCards.TryGetValue(player, out var lockedCardGuid) || lockedCardGuid != cardGuid)
        {
            return;
        }

        if (!serverHandCards.TryGetValue(playerId, out var playerHand) ||
            playerHand.FirstOrDefault(c => c.guid == cardGuid) is not { } state)
        {
            return;
        }
        
        gameManager.ScoreSystem.AddToTeamScore(player, gameManager.Settings.CardLockCosts);
        lockedCards.Remove(player);
        state.locked = false;
        EventBus.Instance.CardSystemEventBus.EmitCardUnlocked(new CardEventArgs(cardGuid, player));
        UpdateCardsServer(playerId, serverHandCards[playerId]);
    }

    private void NotifyCardUnlocked(string cardGuid)
    {
        EventBus.Instance.CardSystemEventBus.EmitCardUnlocked(new CardEventArgs(cardGuid));
    }

    public bool CanSelectCard(int playerId, string cardGuid)
    {
        if (!Multiplayer.IsServer())
        {
            return false;
        }

        return serverHandCards.TryGetValue(playerId, out var playerHand)
               && playerHand.Any(card => card.guid == cardGuid);
    }
    
    public void ServerApplyCards(List<string> cardGuids, PlayerCharacter player)
    {
        var selectableCardGuids = cardGuids
            .Distinct()
            .Where(cardGuid => CanSelectCard((int)player.PlayerId, cardGuid))
            .ToList();

        if (lockedCards.TryGetValue(player, out var lockedCardGuid) && selectableCardGuids.Contains(lockedCardGuid))
        {
            lockedCards.Remove(player);
        }

        foreach (var cardGuid in selectableCardGuids)
        {
            SetServerExhaustionCount((int)player.PlayerId, cardGuid, 2);
            EventBus.Instance.CardSystemEventBus.EmitCardPicked(new CardEventArgs(cardGuid, player));
            if (GlobalCardManager.Instance.AbilityCards.ContainsKey(cardGuid))
            {
                applyAbility(cardGuid, player);
            }
            else if (GlobalCardManager.Instance.ItemCards.TryGetValue(cardGuid, out var item))
            {
                applyItem(item, player);
            }
        }

        SyncDeckExhaustion(player);
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

    private void SyncDeckExhaustion(PlayerCharacter player)
    {
        var exhaustionCounts = new Godot.Collections.Dictionary<string, int>();
        foreach (var card in player.Deck.Cards.Keys)
        {
            exhaustionCounts[card.EffectGUID] = GetServerExhaustionCount((int)player.PlayerId, card.EffectGUID);
        }

        RpcId(player.PlayerId, MethodName.UpdateDeckExhaustion, exhaustionCounts);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UpdateDeckExhaustion(Godot.Collections.Dictionary<string, int> exhaustionCounts)
    {
        var player = gameManager.GetPlayerCharacter(Multiplayer.GetUniqueId());
        if (player?.Deck == null)
        {
            return;
        }

        foreach (var card in player.Deck.Cards.Keys)
        {
            card.ExhaustionCount = exhaustionCounts.TryGetValue(card.EffectGUID, out var count)
                ? count
                : 0;
        }
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
