using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;

public sealed class GameContext
{
    public GameManager GameManager { get; }
    public TeamSystem TeamSystem { get; }
    
    public CardSystem CardSystem { get; }
    
    public ScoreSystem ScoreSystem { get; }

    public IReadOnlyDictionary<long, PlayerCharacter> Players => _players;
    private readonly Dictionary<long, PlayerCharacter> _players;

    public GameContext(GameManager gm, Dictionary<long, PlayerCharacter> players, TeamSystem ts, CardSystem cs, ScoreSystem ss)
    {
        GameManager = gm;
        _players = players;
        TeamSystem = ts;
        CardSystem = cs;
        ScoreSystem = ss;
    }
}

public sealed class TeamSystem
{
    public GameManager GameManager { get; }
    private readonly Dictionary<long, PlayerCharacter> _players;
    public Dictionary<int, Team> Teams { get; } = new ();
    public TeamSystem(GameManager gm, Dictionary<long, PlayerCharacter> players)
    {
        GameManager = gm;
        _players = players;
    }

    public List<Team> GetTeamsWithAlivePlayers()
    {
        return Teams.Values.Where(t => t.HasAlivePlayers()).ToList();
    }

    public void UpdateTeams()
    {
        foreach (var player in _players.Values)
        {
            if (!Teams.TryGetValue(player.TeamId, out var team))
            {
                team = new Team
                {
                    TeamId = player.TeamId
                };
                Teams.Add(team.TeamId, team);
            }
            
            team.AddPlayer(player);
        }
    }
}

public sealed class Team
{
    public List<PlayerCharacter> Players { get; } = new List<PlayerCharacter>();
    public int TeamId;

    public bool AddPlayer(PlayerCharacter player)
    {
        if  (Players.Contains(player)) return false;
        Players.Add(player);
        return true;
    }

    public bool HasAlivePlayers()
    {
        return Players.Count > 0 && Players.Any(p => !p.HealthComponent.IsDead);
    }
}

public sealed class ScoreSystem
{
    public readonly Dictionary<PlayerCharacter, int> PlayerScores = new();

    public void AddToPlayerScore(PlayerCharacter player, int score)
    {
        if (!PlayerScores.TryAdd(player, score))
        {
            PlayerScores[player] += score;
        }
    }
}

public sealed class CardSystem
{
    public List<string> DrawCards(List<Card> deck, int amount)
    {
        var handCards = new List<string>();
        if (deck.Count <= amount)
        {
            handCards.AddRange(deck.Select(c => c.EffectGUID));
        }
        else
        {
            var tmpDeck = new Card[deck.Count];
            deck.CopyTo(tmpDeck);
            var tmpList = tmpDeck.ToList();
            var rnd = new Random();
            while (handCards.Count < amount)
            {

                var index = rnd.NextInt64(0, tmpList.Count);
                handCards.Add(tmpList[(int)index].EffectGUID);
                tmpList.RemoveAt((int)index);
            }
        }

        return handCards;
    }
    
    public void ServerApplyCards(List<string> cardGuids, PlayerCharacter player)
    {
        foreach (var cardGuid in cardGuids)
        {
            if (GlobalCardManager.Instance.AbilityCards.ContainsKey(cardGuid))
            {
                applyAbility(cardGuid, player);
            } else if (GlobalCardManager.Instance.ItemCards.TryGetValue(cardGuid, out var item))
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