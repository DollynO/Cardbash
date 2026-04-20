using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GameFlowController : Node
{
    private GameModeSettings settings;
    private IGameMode _mode;
    private readonly GameContext _ctx;

    private MatchPhase _phase = MatchPhase.None;
    private int _roundIndex;
    private double _phaseTime;

    private readonly List<(long id, string guid)> pickedCards =  new();
    private int drawRoundIndex;
    private bool allCardsDrawn;
    
    public GameFlowController(GameContext ctx, GameModeSettings settings)
    {
        _ctx = ctx;
        this.settings = settings;
    }
    
    public override void _Ready()
    {
        // Node lifecycle reference: :contentReference[oaicite:3]{index=3}
        if (Multiplayer.IsServer())
        {
            
            _mode = new LastTeamStandingMode(settings);
            _mode.ServerInitialize(_ctx);
            
        }
        
        _ctx.GameManager.Hud.CardLocked += HudOnCardLocked;
    }

    private void HudOnCardLocked(int playerId, string cardGuid)
    {
        RpcId(1, MethodName.SyncSelectedCard, playerId, cardGuid);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncSelectedCard(int playerId, string cardGuid)
    {
        if (Multiplayer.IsServer())
        {
           pickedCards.Add((playerId, cardGuid));
           
           if (pickedCards.Count == _ctx.Players.Count)
           {
                   ServerFinishDraw();
           }
        }
        else
        {
            GD.PrintErr("Synced Card not to server.");
        }
    }

    public void Start()
    {
        _phase = MatchPhase.RoundSetup;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.IsServer()) return;

        _phaseTime += delta;

        switch (_phase)
        {
            case MatchPhase.RoundSetup:
                ServerEnterRoundSetup();
                break;

            case MatchPhase.CardDraw:
                if (ServerIsCardSelectionComplete())
                    ServerAdvance(MatchPhase.CardApply);
                break;

            case MatchPhase.CardApply:
                ServerApplyCards();
                ServerAdvance(MatchPhase.CardDrawEnd);
                break;
            
            case MatchPhase.CardDrawEnd:
                if (ServerCheckDrawEnd())
                {
                    StartCombatRound();
                    ServerAdvance(MatchPhase.Combat);
                }
                else
                {
                    ServerAdvance(MatchPhase.CardDraw);                    
                }
                break;

            case MatchPhase.Combat:
                drawRoundIndex = 0;
                _mode.ServerTick(delta);

                if (_mode.ServerIsRoundOver(out var rr))
                    ServerEndRound(rr);
                break;

            case MatchPhase.RoundEnd:
                if (_mode.ServerIsGameOver(out var gr))
                    ServerEndGame(gr);
                else
                    ServerAdvance(MatchPhase.RoundSetup);
                break;
        }
    }

    private bool ServerCheckDrawEnd()
    {
        return drawRoundIndex == settings.CardsPerRound;
    }
    
    private bool ServerIsCardSelectionComplete()
    {
        return allCardsDrawn;
    }
    
    private void ServerEnterRoundSetup()
    {
        _roundIndex++;
        updateGameInfo();
        _ctx.TeamSystem.UpdateTeams();
        _mode.ServerStartRound(_roundIndex);

        _phaseTime = 0;
        Rpc(nameof(ClientRoundSetup), _roundIndex);

        ServerAdvance(MatchPhase.CardDraw);
    }

    private void StartCombatRound()
    {
        _mode.ServerStartCombat();
    }

    private void ServerAdvance(MatchPhase next)
    {
        _phase = next;
        _phaseTime = 0;

        Rpc(nameof(ClientPhaseChanged), (int)_phase);

        if (next == MatchPhase.CardDraw)
        {
            allCardsDrawn = false;
            ServerBeginCardDraw();
        }
    }

    private void ServerBeginCardDraw()
    {
        drawRoundIndex++;
        foreach (var kvp in _ctx.Players)
        {
            var cards = _ctx.CardSystem.DrawCards(kvp.Value.Deck, 5);
            var cardArray = new Array<string>(cards);
            RpcId(kvp.Key, MethodName.OpenDrawOnClient, kvp.Key, cardArray);
        }
    }

    [Rpc(CallLocal = true,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OpenDrawOnClient(long id, Array<string> cardArray)
    {
        var player = _ctx.Players[id];
        var cards = cardArray.Select(cardGuid => player.Deck.Cards.FirstOrDefault(kvp => kvp.Key.EffectGUID == cardGuid).Key).ToList();
        _ctx.GameManager.Hud.ShowDrawUi(true, cards);
    }

    private void ServerFinishDraw()
    {
        Rpc(MethodName.CloseDrawOnClient);
        allCardsDrawn = true;
    }

    [Rpc(CallLocal = true,  TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CloseDrawOnClient()
    {
        _ctx.GameManager.Hud.ShowDrawUi(false, null);
    }
    
    private void ServerApplyCards()
    {
        foreach (var kvp in _ctx.Players)
        {
            var selectedCards = pickedCards.Where(p => p.id == kvp.Key).Select(p => p.guid).ToList();
            _ctx.CardSystem.ServerApplyCards(selectedCards, kvp.Value);
        }
        
        pickedCards.Clear();
    }
    

    private void ServerEndRound(RoundResult rr)
    {
        _mode.RoundResults.Add(rr);
        _phase = MatchPhase.RoundEnd;
        _phaseTime = 0;
    }
    
    [Rpc] private void ClientPhaseChanged(int phase) { /* update HUD */ }
    
    [Rpc]
    private void ClientRoundSetup(int roundIndex) { /* show */ }
    
    [Rpc]
    private void ClientCardsApplied(Variant dto) { /* show */ }
    
    [Rpc]
    private void ClientRoundEnded(Variant dto) { /* scoreboard */ }

    private void ServerForceRoundEnd_Time()
    {
        // e.g., decide by points, or “most alive”, or “flag progress”, depending on mode
        ServerEndRound(_mode.ServerIsRoundOver(out var rr)
            ? rr
            : RoundResult.DrawByTimeout(_ctx.TeamSystem.GetTeamsWithAlivePlayers()));
    }

    private void ServerEndGame(GameResult gr)
    {
        _phase = MatchPhase.GameEnd;
    }

    [Rpc] private void ClientGameEnded(Variant dto) { /* end screen */ }

    private void updateGameInfo()
    {
        var info = string.Empty;
        foreach (var kvp in _ctx.ScoreSystem.PlayerScores)
        {
            info += $"/n{kvp.Key.PlayerName}:{kvp.Value}";
        }
        _ctx.GameManager.Hud.DisplayRoundInfo(info);
    }
}
