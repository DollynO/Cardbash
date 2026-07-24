using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class GameFlowController : Node
{
    private GameModeSettings settings;
    private IGameMode _mode;
    private readonly GameContext _ctx;

    private MatchPhase _phase = MatchPhase.None;
    private int _roundIndex;

    private readonly List<(long id, string guid)> pickedCards = new();
    private int drawRoundIndex;
    private bool allCardsDrawn;
    private bool gameStarted;

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
        if (Multiplayer.IsServer() && !gameStarted)
        {
            _mode?.ServerStartGame();
            gameStarted = true;
        }

        _phase = MatchPhase.RoundSetup;
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.IsServer()) return;

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
        _ctx.TeamSystem.UpdateTeams();
        _mode.ServerStartRound(_roundIndex);

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
        _ctx.CardSystem.DrawCards(5);

        foreach (var kvp in _ctx.Players)
        {
            RpcId(kvp.Key, MethodName.OpenDrawOnClient);
        }
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void OpenDrawOnClient()
    {
        _ctx.GameManager.Hud.ShowDrawUi(true);
    }

    private void ServerFinishDraw()
    {
        Rpc(MethodName.CloseDrawOnClient);
        allCardsDrawn = true;
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void CloseDrawOnClient()
    {
        _ctx.GameManager.Hud.ShowDrawUi(false);
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
    }

    [Rpc] private void ClientPhaseChanged(int phase) { /* update HUD */ }

    [Rpc]
    private void ClientRoundSetup(int roundIndex) { /* show */ }

    private void ServerEndGame(GameResult gr)
    {
        _phase = MatchPhase.GameEnd;
    }
}
