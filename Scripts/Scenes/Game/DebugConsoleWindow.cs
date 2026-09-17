using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Abilities.Buffs;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot;

public partial class DebugConsoleWindow : PanelContainer
{
    private const int MaxLogLines = 400;
    private const uint CaretUnicode = 94;
    public static bool BlocksGameplayInput { get; private set; }

    private readonly List<string> logLines = new();
    private readonly List<string> consoleLines = new();

    private RichTextLabel logOutput;
    private RichTextLabel consoleOutput;
    private LineEdit commandInput;

    public override void _Ready()
    {
        Name = nameof(DebugConsoleWindow);
        Visible = false;
        BlocksGameplayInput = false;
        ProcessMode = ProcessModeEnum.Always;
        SetProcessInput(true);

        BuildUi();
        EventBus.Instance.EventTraceEventHandler += OnEventTrace;
        AddConsoleLine("Debug console ready. Type 'help' for commands.");
    }

    public override void _ExitTree()
    {
        BlocksGameplayInput = false;
        EventBus.Instance.EventTraceEventHandler -= OnEventTrace;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        if (!IsToggleKey(keyEvent))
        {
            return;
        }

        Toggle();
        GetViewport().SetInputAsHandled();
    }

    private void BuildUi()
    {
        AnchorLeft = 0f;
        AnchorTop = 0f;
        AnchorRight = 1f;
        AnchorBottom = 0f;
        OffsetLeft = 0f;
        OffsetTop = 0f;
        OffsetRight = 0f;
        OffsetBottom = 360f;
        ZIndex = 4096;
        MouseFilter = MouseFilterEnum.Stop;

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.03f, 0.035f, 0.92f),
            BorderColor = new Color(0.25f, 0.32f, 0.36f, 0.95f),
            BorderWidthLeft = 0,
            BorderWidthTop = 0,
            BorderWidthRight = 0,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
        };
        AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var split = new HSplitContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SplitOffset = 760,
        };
        margin.AddChild(split);

        split.AddChild(CreateLogPane());
        split.AddChild(CreateConsolePane());
    }

    private Control CreateLogPane()
    {
        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        container.AddThemeConstantOverride("separation", 6);

        container.AddChild(CreateHeader("LOG"));

        logOutput = new RichTextLabel
        {
            FitContent = false,
            ScrollFollowing = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Text = string.Empty,
        };
        logOutput.AddThemeFontSizeOverride("normal_font_size", 14);
        container.AddChild(logOutput);

        return container;
    }

    private Control CreateConsolePane()
    {
        var container = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(320, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        container.AddThemeConstantOverride("separation", 6);

        container.AddChild(CreateHeader("CONSOLE"));

        consoleOutput = new RichTextLabel
        {
            FitContent = false,
            ScrollFollowing = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Text = string.Empty,
        };
        consoleOutput.AddThemeFontSizeOverride("normal_font_size", 14);
        container.AddChild(consoleOutput);

        commandInput = new LineEdit
        {
            PlaceholderText = "command",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        commandInput.TextSubmitted += ExecuteCommand;
        container.AddChild(commandInput);

        return container;
    }

    private static Label CreateHeader(string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(0.72f, 0.86f, 0.9f));
        return label;
    }

    private static bool IsToggleKey(InputEventKey keyEvent)
    {
        return keyEvent.Unicode == CaretUnicode
               || keyEvent.Keycode == (Key)CaretUnicode
               || keyEvent.PhysicalKeycode == (Key)CaretUnicode;
    }

    private void Toggle()
    {
        Visible = !Visible;
        BlocksGameplayInput = Visible;
        if (Visible)
        {
            commandInput?.GrabFocus();
        }
        else
        {
            commandInput?.ReleaseFocus();
        }
    }

    private void OnEventTrace(object sender, EventBusTraceEventArgs args)
    {
        if (!Multiplayer.IsServer() && Multiplayer.MultiplayerPeer != null)
        {
            return;
        }

        var line = $"{TimeStamp()} [{args.BusName}] {args.EventName} {FormatArgs(args)}";
        if (Multiplayer.IsServer())
        {
            Rpc(MethodName.AppendNetworkLog, line);
            return;
        }

        AddLogLine(line);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AppendNetworkLog(string line)
    {
        AddLogLine(line);
    }

    private void ExecuteCommand(string command)
    {
        command = command.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        AddConsoleLine($"> {command}");
        commandInput.Clear();

        var lowerCommand = command.ToLowerInvariant();
        if (lowerCommand == "stats")
        {
            AddConsoleLine(FormatEditableStats());
            return;
        }

        if (lowerCommand == "players"
            || lowerCommand.StartsWith("stat ", StringComparison.OrdinalIgnoreCase))
        {
            RouteServerCommand(command);
            return;
        }

        switch (lowerCommand)
        {
            case "clear":
                logLines.Clear();
                consoleLines.Clear();
                RefreshText(logOutput, logLines);
                RefreshText(consoleOutput, consoleLines);
                AddConsoleLine("Cleared.");
                break;
            case "help":
                AddConsoleLine("Commands: clear, events, hide, help, players, stats, stat <player> <stat|list> <value|reset>");
                AddConsoleLine("Examples: stat me movementSpeed 250 | stat me health 75 | stat me maxHealth 150 | stat me bounceCount 5 | stat me pierceCount 5");
                break;
            case "events":
                AddConsoleLine($"{logLines.Count} log entries buffered.");
                break;
            case "hide":
                Toggle();
                break;
            default:
                AddConsoleLine($"Unknown command: {command}");
                break;
        }
    }

    private void RouteServerCommand(string command)
    {
        if (Multiplayer.IsServer() || Multiplayer.MultiplayerPeer == null)
        {
            AddConsoleLine(ExecuteServerCommand(command, Multiplayer.GetUniqueId()));
            return;
        }

        RpcId(1, MethodName.ExecuteServerConsoleCommand, command);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ExecuteServerConsoleCommand(string command)
    {
        if (!Multiplayer.IsServer())
        {
            return;
        }

        var senderId = Multiplayer.GetRemoteSenderId();
        var result = ExecuteServerCommand(command, senderId);
        RpcId(senderId, MethodName.ReceiveConsoleCommandResult, result);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveConsoleCommandResult(string line)
    {
        AddConsoleLine(line);
    }

    private string ExecuteServerCommand(string command, long senderId)
    {
        var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0)
        {
            return string.Empty;
        }

        if (args[0].Equals("players", StringComparison.OrdinalIgnoreCase))
        {
            return FormatPlayers();
        }

        return args[0].Equals("stat", StringComparison.OrdinalIgnoreCase)
            ? ExecuteStatCommand(args, senderId)
            : $"Unknown server command: {command}";
    }

    private string ExecuteStatCommand(string[] args, long senderId)
    {
        if (args.Length < 3)
        {
            return "Usage: stat <player> <stat|list> <value|reset>";
        }

        if (!TryResolvePlayer(args[1], senderId, out var player, out var error))
        {
            return error;
        }

        if (args[2].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            return FormatPlayerStats(player);
        }

        if (args.Length < 4)
        {
            return "Usage: stat <player> <stat|list> <value|reset>";
        }

        if (!player.TryGetComponent(out StatblockComponent statBlock))
        {
            return $"{FormatPlayerLabel(player)} has no stat block.";
        }

        if (IsCurrentHealthAlias(args[2]))
        {
            return SetPlayerCurrentHealth(player, statBlock, args[3]);
        }

        if (!TryResolveStat(args[2], out var statType, out var resetValue))
        {
            return $"Unknown stat '{args[2]}'. Try movementSpeed, health, maxHealth, bounceCount, pierceCount, or a StatType name.";
        }

        if (!TryParseStatValue(args[3], resetValue, out var value, out error))
        {
            return error;
        }

        statBlock.SetBaseStat(statType, value);
        if (statType == StatType.Life && player.TryGetComponent(out HealthComponent health))
        {
            health.Reset(value);
        }

        return $"{FormatPlayerLabel(player)} {statType} = {statBlock.GetStat(statType):0.###}";
    }

    private string SetPlayerCurrentHealth(PlayerCharacter player, StatblockComponent statBlock, string valueText)
    {
        if (!player.TryGetComponent(out HealthComponent health))
        {
            return $"{FormatPlayerLabel(player)} has no health component.";
        }

        if (valueText.Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            health.Reset(statBlock.GetStat(StatType.Life));
            return $"{FormatPlayerLabel(player)} health = {health.CurrentHealth:0.###}/{health.MaxHealth:0.###}";
        }

        if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return $"Invalid stat value '{valueText}'.";
        }

        health.SetCurrentHealth(value);
        return $"{FormatPlayerLabel(player)} health = {health.CurrentHealth:0.###}/{health.MaxHealth:0.###}";
    }

    private bool TryResolvePlayer(string value, long senderId, out PlayerCharacter player, out string error)
    {
        player = null;
        error = string.Empty;

        var gameManager = GetNodeOrNull<GameManager>("/root/Main/Game");
        if (gameManager == null)
        {
            error = "GameManager not found.";
            return false;
        }

        if (value.Equals("me", StringComparison.OrdinalIgnoreCase)
            || value.Equals("self", StringComparison.OrdinalIgnoreCase)
            || value.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            player = gameManager.GetPlayerCharacter(senderId);
        }
        else if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var playerId))
        {
            player = gameManager.GetPlayerCharacter(playerId);
        }
        else
        {
            player = gameManager.GetPlayers().FirstOrDefault(candidate =>
                string.Equals(candidate.PlayerName, value, StringComparison.OrdinalIgnoreCase)
                || candidate.Name.ToString().Equals(value, StringComparison.OrdinalIgnoreCase));
        }

        if (player != null)
        {
            return true;
        }

        error = $"Player '{value}' not found. Use 'players' to list valid targets.";
        return false;
    }

    private static bool TryResolveStat(string value, out StatType statType, out float resetValue)
    {
        resetValue = 0f;
        if (value.Equals("moveSpeed", StringComparison.OrdinalIgnoreCase)
            || value.Equals("movementSpeed", StringComparison.OrdinalIgnoreCase)
            || value.Equals("movement_speed", StringComparison.OrdinalIgnoreCase))
        {
            statType = StatType.MovementSpeed;
            resetValue = 150f;
            return true;
        }

        if (value.Equals("bounce", StringComparison.OrdinalIgnoreCase)
            || value.Equals("bounces", StringComparison.OrdinalIgnoreCase)
            || value.Equals("bounceCount", StringComparison.OrdinalIgnoreCase)
            || value.Equals("projectileBounceCount", StringComparison.OrdinalIgnoreCase)
            || value.Equals("projectile_bounce_count", StringComparison.OrdinalIgnoreCase))
        {
            statType = StatType.ProjectileBounceCount;
            resetValue = -1f;
            return true;
        }

        if (value.Equals("pierce", StringComparison.OrdinalIgnoreCase)
            || value.Equals("pierces", StringComparison.OrdinalIgnoreCase)
            || value.Equals("pierceCount", StringComparison.OrdinalIgnoreCase)
            || value.Equals("projectilePierceCount", StringComparison.OrdinalIgnoreCase)
            || value.Equals("projectile_pierce_count", StringComparison.OrdinalIgnoreCase))
        {
            statType = StatType.ProjectilePierceCount;
            resetValue = -1f;
            return true;
        }

        if (value.Equals("life", StringComparison.OrdinalIgnoreCase)
            || value.Equals("maxHealth", StringComparison.OrdinalIgnoreCase)
            || value.Equals("max_health", StringComparison.OrdinalIgnoreCase))
        {
            statType = StatType.Life;
            resetValue = 100f;
            return true;
        }

        if (Enum.TryParse(value, true, out statType))
        {
            return true;
        }

        statType = default;
        return false;
    }

    private static bool IsCurrentHealthAlias(string value)
    {
        return value.Equals("health", StringComparison.OrdinalIgnoreCase)
               || value.Equals("currentHealth", StringComparison.OrdinalIgnoreCase)
               || value.Equals("current_health", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseStatValue(string valueText, float resetValue, out float value, out string error)
    {
        if (valueText.Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            value = resetValue;
            error = string.Empty;
            return true;
        }

        if (float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            error = string.Empty;
            return true;
        }

        error = $"Invalid stat value '{valueText}'.";
        return false;
    }

    private string FormatPlayers()
    {
        var gameManager = GetNodeOrNull<GameManager>("/root/Main/Game");
        if (gameManager == null)
        {
            return "GameManager not found.";
        }

        var players = gameManager.GetPlayers()
            .OrderBy(player => player.PlayerId)
            .Select(player => $"{FormatPlayerLabel(player)} team={player.TeamId}");

        return "Players:\n" + string.Join("\n", players);
    }

    private static string FormatEditableStats()
    {
        var statNames = Enum.GetNames<StatType>();
        return "Editable stats:"
               + "\nmovementSpeed - player movement speed"
               + "\nhealth - current health"
               + "\nmaxHealth / life - max health"
               + "\nbounceCount - projectile bounce override, reset = ability default"
               + "\npierceCount - projectile pierce override, reset = ability default"
               + "\nAny StatType name is also accepted:"
               + $"\n{string.Join(", ", statNames)}";
    }

    private static string FormatPlayerStats(PlayerCharacter player)
    {
        var movementSpeed = player.StatBlock?.GetStat(StatType.MovementSpeed) ?? 0f;
        var bounceCount = player.StatBlock?.GetStat(StatType.ProjectileBounceCount) ?? -1f;
        var pierceCount = player.StatBlock?.GetStat(StatType.ProjectilePierceCount) ?? -1f;
        var life = player.StatBlock?.GetStat(StatType.Life) ?? 0f;
        var health = player.HealthComponent;

        return $"{FormatPlayerLabel(player)} stats:"
               + $"\nMovementSpeed = {movementSpeed:0.###}"
               + $"\nProjectileBounceCount = {(bounceCount < 0 ? "default" : bounceCount.ToString("0", CultureInfo.InvariantCulture))}"
               + $"\nProjectilePierceCount = {(pierceCount < 0 ? "default" : pierceCount.ToString("0", CultureInfo.InvariantCulture))}"
               + $"\nLife = {life:0.###}"
               + $"\nHealth = {health?.CurrentHealth:0.###}/{health?.MaxHealth:0.###}";
    }

    private static string FormatPlayerLabel(PlayerCharacter player)
    {
        return $"{PlayerName(player)}/{player.PlayerId}";
    }

    private void AddLogLine(string line)
    {
        logLines.Add(line);
        Trim(logLines);
        RefreshText(logOutput, logLines);
    }

    private void AddConsoleLine(string line)
    {
        consoleLines.Add($"{TimeStamp()} {line}");
        Trim(consoleLines);
        RefreshText(consoleOutput, consoleLines);
    }

    private static void Trim(List<string> lines)
    {
        while (lines.Count > MaxLogLines)
        {
            lines.RemoveAt(0);
        }
    }

    private static void RefreshText(RichTextLabel output, IEnumerable<string> lines)
    {
        if (output == null)
        {
            return;
        }

        output.Text = string.Join("\n", lines);
    }

    private static string FormatArgs(EventBusTraceEventArgs trace)
    {
        return trace.Args switch
        {
            DamageEventArgs damage => $"{FormatEntity(damage.Source)} -> {FormatEntity(damage.Target)} {FormatDamage(damage.Damage)}",
            KilledEventArgs killed => $"{FormatEntity(killed.Source)} killed {FormatEntity(killed.Target)}",
            CardBase.Scripts.BuffEventArgs buff => $"{FormatBuff(buff.Buff)}",
            AbilityEventArgs ability => $"{FormatEntity(ability.Source)} cast {ability.Ability?.DisplayName ?? ability.Ability?.GUID ?? "unknown"}",
            CardEventArgs card => $"{FormatEntity(card.Player)} {CardAction(trace.EventName)} {FormatCard(card.CardGuid)}",
            MatchEventArgs match => $"round={match.RoundNumber}",
            ScoreEventArgs score => $"team={score.TeamId} score={score.Score}",
            NotificationArgs notification => $"{notification.EventType}: {notification.Message}",
            null => string.Empty,
            _ => trace.Args.ToString(),
        };
    }

    private static string FormatDamage(Damage damage)
    {
        if (damage == null)
        {
            return "0 damage";
        }

        return $"{damage.DamageNumber:0.#} {damage.Type}";
    }

    private static string FormatBuff(Buff buff)
    {
        if (buff == null)
        {
            return "unknown buff";
        }

        return $"{buff.DisplayName} type={buff.BuffType} target={FormatEntity(buff.Target)} stacks={buff.StackCount}";
    }

    private static string FormatCard(string cardGuid)
    {
        var cardName = TryGetCardName(cardGuid);
        return string.IsNullOrWhiteSpace(cardName) ? cardGuid : $"{cardName} ({ShortGuid(cardGuid)})";
    }

    private static string TryGetCardName(string cardGuid)
    {
        if (string.IsNullOrWhiteSpace(cardGuid))
        {
            return string.Empty;
        }

        if (GlobalCardManager.Instance.AbilityCards.TryGetValue(cardGuid, out var abilityCard))
        {
            return abilityCard.DisplayName;
        }

        return GlobalCardManager.Instance.ItemCards.TryGetValue(cardGuid, out var itemCard)
            ? itemCard.DisplayName
            : string.Empty;
    }

    private static string FormatEntity(IEntityComponent entity)
    {
        return entity switch
        {
            PlayerCharacter player => $"{PlayerName(player)}/{player.PlayerId}",
            Node node => node.Name,
            null => "unknown",
            _ => entity.GetType().Name,
        };
    }

    private static string PlayerName(PlayerCharacter player)
    {
        return string.IsNullOrWhiteSpace(player.PlayerName) ? "Player" : player.PlayerName;
    }

    private static string CardAction(string eventName)
    {
        return eventName switch
        {
            nameof(CardSystemEventBus.CardPickedEventHandler) => "picked",
            nameof(CardSystemEventBus.CardLockedEventHandler) => "locked",
            nameof(CardSystemEventBus.CardUnlockedEventHandler) => "unlocked",
            _ => "card",
        };
    }

    private static string ShortGuid(string guid)
    {
        return string.IsNullOrWhiteSpace(guid)
            ? string.Empty
            : guid.Length > 8
                ? guid[^8..]
                : guid;
    }

    private static string TimeStamp()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }
}
