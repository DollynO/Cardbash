using System;
using System.Collections.Generic;
using CardBase.Scripts.GameSettings;
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