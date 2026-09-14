using System;
using System.Collections.Generic;
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

    private readonly List<string> logLines = new();
    private readonly List<string> consoleLines = new();

    private RichTextLabel logOutput;
    private RichTextLabel consoleOutput;
    private LineEdit commandInput;

    public override void _Ready()
    {
        Name = nameof(DebugConsoleWindow);
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
        SetProcessInput(true);

        BuildUi();
        EventBus.Instance.EventTraceEventHandler += OnEventTrace;
        AddConsoleLine("Debug console ready. Type 'help' for commands.");
    }

    public override void _ExitTree()
    {
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

        switch (command.ToLowerInvariant())
        {
            case "clear":
                logLines.Clear();
                consoleLines.Clear();
                RefreshText(logOutput, logLines);
                RefreshText(consoleOutput, consoleLines);
                AddConsoleLine("Cleared.");
                break;
            case "help":
                AddConsoleLine("Commands: clear, events, hide, help");
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
