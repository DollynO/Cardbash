using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public partial class MovementPredictionComponent : Node2D, IComponent
{
    private const int ServerPeerId = 1;

    [Export]
    private float positionCorrectionThreshold = 2.0f;

    [Export]
    private float velocityCorrectionThreshold = 2.0f;

    [Export]
    private float stunTimeCorrectionThreshold = 0.05f;

    [Export]
    private int snapshotIntervalTicks = 1;

    [Export]
    private int maxServerInputsPerPhysics = 4;

    [Export]
    private int maxStoredClientTicks = 256;

    private readonly SortedDictionary<int, PlayerMoveInput> serverPendingInputs = new();
    private readonly Dictionary<int, PlayerMoveInput> clientInputHistory = new();
    private readonly Dictionary<int, MovementSnapshot> clientPredictedHistory = new();

    private PlayerCharacter character;
    private PlayerInput playerInput;
    private MoveComponent moveComponent;

    private int localTick;
    private int lastServerProcessedTick;
    private int lastServerSnapshotTick;

    public IEntityComponent Parent { get; set; }

    public MovementPredictionComponent()
    {
        Name = nameof(MovementPredictionComponent);
    }

    public void SetParent(IEntityComponent component)
    {
        Parent = component;
        character = (PlayerCharacter)component;
        playerInput = character.PlayerInput;

        if (!character.TryGetComponent(out moveComponent))
        {
            throw new System.ArgumentNullException(nameof(moveComponent));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (moveComponent == null || character == null || playerInput == null)
        {
            return;
        }

        if (Multiplayer.IsServer())
        {
            ProcessServerMovement(delta);
            return;
        }

        if (IsLocalOwner())
        {
            ProcessClientPrediction(delta);
        }
    }

    private void ProcessClientPrediction(double delta)
    {
        var input = CreateLocalInput(++localTick);

        clientInputHistory[input.Tick] = input;
        moveComponent.ProcessMovement(delta, input);
        clientPredictedHistory[input.Tick] = moveComponent.CreateSnapshot(input.Tick);

        PruneClientHistory();
        RpcId(ServerPeerId, MethodName.SubmitMoveInput, input.Tick, input.Direction);
    }

    private void ProcessServerMovement(double delta)
    {
        if (IsLocalOwner())
        {
            var input = CreateLocalInput(++localTick);
            lastServerProcessedTick = input.Tick;
            moveComponent.ProcessMovement(delta, input);
            return;
        }

        var processedCount = 0;
        MovementSnapshot latestSnapshot = default;
        var processedInput = false;

        while (serverPendingInputs.Count > 0 && processedCount < maxServerInputsPerPhysics)
        {
            var input = DequeueNextServerInput();
            if (input.Tick <= lastServerProcessedTick)
            {
                continue;
            }

            lastServerProcessedTick = input.Tick;
            moveComponent.ProcessMovement(delta, input);
            latestSnapshot = moveComponent.CreateSnapshot(input.Tick);

            processedInput = true;
            processedCount++;
        }

        if (processedInput && ShouldSendServerSnapshot(latestSnapshot.Tick))
        {
            lastServerSnapshotTick = latestSnapshot.Tick;
            SendServerSnapshot(latestSnapshot);
        }
    }

    private PlayerMoveInput CreateLocalInput(int tick)
    {
        return new PlayerMoveInput(
            tick,
            new Vector2(playerInput.XDirection, playerInput.YDirection));
    }

    private PlayerMoveInput DequeueNextServerInput()
    {
        var first = serverPendingInputs.First();
        serverPendingInputs.Remove(first.Key);
        return first.Value;
    }

    private bool ShouldSendServerSnapshot(int tick)
    {
        return snapshotIntervalTicks <= 1 || tick - lastServerSnapshotTick >= snapshotIntervalTicks;
    }

    private void SendServerSnapshot(MovementSnapshot snapshot)
    {
        RpcId(
            character.PlayerId,
            MethodName.ReceiveServerSnapshot,
            snapshot.Tick,
            snapshot.Position,
            snapshot.Velocity,
            snapshot.Stun,
            snapshot.StunTime,
            snapshot.MaxStunTime,
            snapshot.Hitstun,
            snapshot.Rooted,
            snapshot.MovementDisabled,
            snapshot.ImpulseVector,
            snapshot.ForceVector,
            snapshot.ForceDecayTime,
            snapshot.RepositionPosition,
            snapshot.RepositionBlock);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void SubmitMoveInput(int tick, Vector2 direction)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        var senderId = Multiplayer.GetRemoteSenderId();
        if (senderId != 0 && senderId != character.PlayerId)
        {
            return;
        }

        if (tick <= lastServerProcessedTick)
        {
            return;
        }

        serverPendingInputs[tick] = new PlayerMoveInput(tick, direction);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void ReceiveServerSnapshot(
        int tick,
        Vector2 position,
        Vector2 velocity,
        bool stun,
        float stunTime,
        float maxStunTime,
        bool hitstun,
        bool rooted,
        bool movementDisabled,
        Vector2 impulseVector,
        Vector2 forceVector,
        float forceDecayTime,
        Vector2 repositionPosition,
        bool repositionBlock)
    {
        if (Multiplayer.IsServer() || !IsLocalOwner())
        {
            return;
        }

        var snapshot = new MovementSnapshot(
            tick,
            position,
            velocity,
            stun,
            stunTime,
            maxStunTime,
            hitstun,
            rooted,
            movementDisabled,
            impulseVector,
            forceVector,
            forceDecayTime,
            repositionPosition,
            repositionBlock);

        HandleServerSnapshot(snapshot);
    }

    private void HandleServerSnapshot(MovementSnapshot snapshot)
    {
        if (!clientPredictedHistory.TryGetValue(snapshot.Tick, out var predictedSnapshot))
        {
            PruneAcknowledgedClientTicks(snapshot.Tick);
            return;
        }

        var replayInputs = GetReplayInputsAfter(snapshot.Tick);
        if (NeedsReconciliation(snapshot, predictedSnapshot))
        {
            var replayedSnapshots = moveComponent.ReconcileFromSnapshot(
                snapshot,
                replayInputs,
                GetPhysicsProcessDeltaTime());

            foreach (var replayedSnapshot in replayedSnapshots)
            {
                clientPredictedHistory[replayedSnapshot.Tick] = replayedSnapshot;
            }
        }

        PruneAcknowledgedClientTicks(snapshot.Tick);
    }

    private List<PlayerMoveInput> GetReplayInputsAfter(int tick)
    {
        return clientInputHistory
            .Where(kvp => kvp.Key > tick)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Value)
            .ToList();
    }

    private bool NeedsReconciliation(MovementSnapshot serverSnapshot, MovementSnapshot predictedSnapshot)
    {
        if (serverSnapshot.Position.DistanceTo(predictedSnapshot.Position) > positionCorrectionThreshold)
        {
            return true;
        }

        if (serverSnapshot.Velocity.DistanceTo(predictedSnapshot.Velocity) > velocityCorrectionThreshold)
        {
            return true;
        }

        return serverSnapshot.Stun != predictedSnapshot.Stun
               || Mathf.Abs(serverSnapshot.StunTime - predictedSnapshot.StunTime) > stunTimeCorrectionThreshold
               || serverSnapshot.Hitstun != predictedSnapshot.Hitstun
               || serverSnapshot.Rooted != predictedSnapshot.Rooted
               || serverSnapshot.MovementDisabled != predictedSnapshot.MovementDisabled
               || serverSnapshot.RepositionBlock != predictedSnapshot.RepositionBlock;
    }

    private void PruneAcknowledgedClientTicks(int acknowledgedTick)
    {
        foreach (var tick in clientInputHistory.Keys.Where(t => t <= acknowledgedTick).ToList())
        {
            clientInputHistory.Remove(tick);
        }

        foreach (var tick in clientPredictedHistory.Keys.Where(t => t <= acknowledgedTick).ToList())
        {
            clientPredictedHistory.Remove(tick);
        }
    }

    private void PruneClientHistory()
    {
        while (clientInputHistory.Count > maxStoredClientTicks)
        {
            clientInputHistory.Remove(clientInputHistory.Keys.Min());
        }

        while (clientPredictedHistory.Count > maxStoredClientTicks)
        {
            clientPredictedHistory.Remove(clientPredictedHistory.Keys.Min());
        }
    }

    private bool IsLocalOwner()
    {
        return character.PlayerId == Multiplayer.GetUniqueId();
    }
}
