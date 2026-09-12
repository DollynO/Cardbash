using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;

public partial class Hud : CanvasLayer
{
    private const int DebugConsoleLayer = 100;
    private const uint FpsToggleUnicode = 35;
    private const double FpsRefreshInterval = 0.25;
    private const double KillFeedEntryLifetime = 8.0;
    private const int KillFeedMaxEntries = 6;
    private const float DarknessStacksForFullEffect = 10f;
    private static readonly string[] AbilityTierLabels = { "I", "II", "III" };

    [Export] private Container _drawUiContainer;
    [Export] private Label _waitLabel;
    [Export] private HBoxContainer _cardBox;
    [Export] private ColorRect _darknessEffect;
    [Export] private Label _roundLabel;
    [Export] private ButtonPrefab _lockButton;
    [Export] private ButtonPrefab _rerollButton;
    [Export] private HBoxContainer _itemContainer;
    [Export] private HealthBar _healthBar;
    [Export] private GameManager _gameManager;
    
    [Export] private VBoxContainer _pointsContainer;
    Dictionary<int, PointOverview> pointOverviews = new();
    [Export] private PackedScene pointOverviewTemplate;
    
    private PackedScene _cardDrawTemplate;
    private List<CardDrawTemplate> cardTemplates = new();
    private ItemManagerComponent _displayedItemManager;
    private string _displayedItemSignature = string.Empty;
    private Label _fpsLabel;
    private double _fpsRefreshTimer;
    private VBoxContainer _killFeedContainer;
    private readonly List<KillFeedEntry> _killFeedEntries = new();
    private PanelContainer _damageHistoryPanel;
    private Label _damageHistoryLabel;
    private double _damageHistoryHideAt;

    [Signal]
    public delegate void CardLockedEventHandler(int playerId, string cardGuid);

    private Card _selectedCard;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _clear_card_box();
        _cardDrawTemplate = ResourceLoader.Load("res://Scenes/CardDrawTemplate.tscn") as PackedScene;
        _healthBar.AllowGreater = true;
        _gameManager.CardSystem.UpdateHandCardsEventHandler += update_hand_cards;
        _gameManager.EventBus.MatchEventBus.ScoreChangedEventHandler += score_changed;

        _gameManager.EventBus.MatchEventBus.RoundStartEventHandler += on_round_start;
        SetProcessInput(true);
        AddKillFeed();
        AddDamageHistoryPanel();
        AddFpsLabel();
        AddDebugConsole();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        if (!IsFpsToggleKey(keyEvent))
        {
            return;
        }

        _fpsLabel.Visible = !_fpsLabel.Visible;
        GetViewport().SetInputAsHandled();
    }

    private void AddFpsLabel()
    {
        _fpsLabel = new Label
        {
            Name = "FpsLabel",
            Text = "FPS: 0",
            Visible = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 4096,
            AnchorLeft = 0.5f,
            AnchorTop = 0f,
            AnchorRight = 0.5f,
            AnchorBottom = 0f,
            OffsetLeft = -80f,
            OffsetTop = 8f,
            OffsetRight = 80f,
            OffsetBottom = 34f,
        };
        _fpsLabel.AddThemeFontSizeOverride("font_size", 18);
        _fpsLabel.AddThemeColorOverride("font_color", Colors.White);
        _fpsLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        _fpsLabel.AddThemeConstantOverride("outline_size", 4);
        AddChild(_fpsLabel);
    }

    private static bool IsFpsToggleKey(InputEventKey keyEvent)
    {
        return keyEvent.Unicode == FpsToggleUnicode
               || keyEvent.Keycode == (Key)FpsToggleUnicode
               || keyEvent.PhysicalKeycode == (Key)FpsToggleUnicode;
    }

    private void AddDebugConsole()
    {
        var layer = new CanvasLayer
        {
            Name = "DebugConsoleLayer",
            Layer = DebugConsoleLayer,
        };
        AddChild(layer);
        layer.AddChild(new DebugConsoleWindow { Name = nameof(DebugConsoleWindow) });
    }

    private void AddKillFeed()
    {
        if (_killFeedContainer != null)
        {
            return;
        }

        var panel = new PanelContainer
        {
            Name = "KillFeed",
            AnchorLeft = 1f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 0f,
            OffsetLeft = -380f,
            OffsetTop = 56f,
            OffsetRight = -16f,
            OffsetBottom = 356f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 32,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.025f, 0.035f, 0.25f),
            BorderColor = new Color(1f, 1f, 1f, 0.08f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
        AddChild(panel);

        var margin = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        _killFeedContainer = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _killFeedContainer.AddThemeConstantOverride("separation", 6);
        margin.AddChild(_killFeedContainer);
    }

    private void AddDamageHistoryPanel()
    {
        if (_damageHistoryPanel != null)
        {
            return;
        }

        _damageHistoryPanel = new PanelContainer
        {
            Name = "DamageHistory",
            Visible = false,
            AnchorLeft = 0.5f,
            AnchorTop = 0f,
            AnchorRight = 0.5f,
            AnchorBottom = 0f,
            OffsetLeft = -220f,
            OffsetTop = 96f,
            OffsetRight = 220f,
            OffsetBottom = 180f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 36,
        };
        _damageHistoryPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.025f, 0.035f, 0.78f),
            BorderColor = new Color(1f, 1f, 1f, 0.14f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
        AddChild(_damageHistoryPanel);

        var margin = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _damageHistoryPanel.AddChild(margin);

        var textStack = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        textStack.AddThemeConstantOverride("separation", 3);
        margin.AddChild(textStack);

        var title = new Label
        {
            Text = "Damage history",
            ClipText = true,
        };
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", Colors.White);
        title.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        title.AddThemeConstantOverride("outline_size", 3);
        textStack.AddChild(title);

        _damageHistoryLabel = new Label
        {
            ClipText = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _damageHistoryLabel.AddThemeFontSizeOverride("font_size", 12);
        _damageHistoryLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.9f, 1f, 0.9f));
        _damageHistoryLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        _damageHistoryLabel.AddThemeConstantOverride("outline_size", 2);
        textStack.AddChild(_damageHistoryLabel);
    }
    
    private void on_round_start(object sender, MatchEventArgs args)
    {
        updatePointsTable();
    }
    
    private void score_changed(object sender, ScoreEventArgs args)
    {
        updatePointsTable();
        UpdateRerollButtonText();
    }

    private void updatePointsTable()
    {
        var grouped = _gameManager.ScoreSystem.TeamScores
            .GroupBy(x => x.Value)
            .OrderByDescending(g => g.Key)
            .ToDictionary(x => x.Key, g => g.Select(x => x.Key).ToList());

        var excludeValues = new HashSet<int>(grouped.Values.SelectMany(x => x));
        var teamsList = _gameManager.TeamSystem.Teams.Values.Where(x => !excludeValues.Contains(x.TeamId)).ToList();
        
        var i = 0;
        foreach (var kvp in grouped)
        {
            i++;
            foreach (var team in kvp.Value)
            {
                if (!pointOverviews.ContainsKey(team)) {
                    pointOverviews[team] = pointOverviewTemplate.Instantiate<PointOverview>();
                    _pointsContainer.AddChild(pointOverviews[team]);
                }

                pointOverviews[team].Update(i, team, kvp.Key);
            }
        }

        i++;
        foreach (var teamId in teamsList.Select(team => team.TeamId))
        {
            if (!pointOverviews.ContainsKey(teamId)) {
                pointOverviews[teamId] = pointOverviewTemplate.Instantiate<PointOverview>();
                _pointsContainer.AddChild(pointOverviews[teamId]);
            }

            pointOverviews[teamId].Update(i, teamId, 0);
        }
    }
    
    private void update_hand_cards(object sender, DrawCardEventArgs args)
    {
        _clear_card_box();
        cardTemplates.Clear();
        _selectedCard = null;
        var player = _gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());

        foreach (var kvp in args.cards)
        {
            if (_cardDrawTemplate.Instantiate() is not CardDrawTemplate cardTemplate)
            {
                continue;
            }

            Card card = null;
            if (GlobalCardManager.Instance.AbilityCards.ContainsKey(kvp.guid))
            {
                card = GlobalCardManager.Instance.AbilityCards[kvp.guid];
            } else if (GlobalCardManager.Instance.ItemCards.ContainsKey(kvp.guid))
            {
                card = GlobalCardManager.Instance.ItemCards[kvp.guid];
            }

            if (card == null)
            {
                continue;
            }
            
            cardTemplate.Name = card.EffectGUID;
            cardTemplate.SetCard(card, GetDrawCardDisplayName(card, player));
            cardTemplate.SetLockState(kvp.locked);
            cardTemplate.CardClicked += on_card_clicked;
            cardTemplate.LockCardClicked += on_lock_clicked;
            cardTemplate.UnLockCardClicked += on_unlock_clicked;
            cardTemplates.Add(cardTemplate);
            _cardBox.AddChild(cardTemplate);
        }
    }

    private static string GetDrawCardDisplayName(Card card, PlayerCharacter player)
    {
        if (card?.CardType != CardType.Ability || player == null)
        {
            return card?.DisplayName;
        }

        var currentTierIndex = -1;
        if (player.TryGetComponent(out AbilityComponent abilityComponent)
            && abilityComponent.networkAbilities.TryGetValue(card.EffectGUID, out var ability))
        {
            currentTierIndex = ability.SkillLevel;
        }

        var nextTierIndex = Mathf.Clamp(currentTierIndex + 1, 0, AbilityTierLabels.Length - 1);
        return $"{card.DisplayName} ({AbilityTierLabels[nextTierIndex]})";
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateKillFeed();
        UpdateDamageHistory();

        if (_fpsLabel == null || !_fpsLabel.Visible)
        {
            return;
        }

        _fpsRefreshTimer += delta;
        if (_fpsRefreshTimer >= FpsRefreshInterval)
        {
            _fpsRefreshTimer = 0;
            _fpsLabel.Text = $"FPS: {(int)Math.Round(Engine.GetFramesPerSecond())}";
        }
    }

    public void ShowKillFeedEntry(string killerName, string victimName)
    {
        AddKillFeedEntry(killerName, victimName);
        Rpc(MethodName.ClientShowKillFeedEntry, killerName, victimName);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientShowKillFeedEntry(string killerName, string victimName)
    {
        AddKillFeedEntry(killerName, victimName);
    }

    public void ShowDamageHistoryForPlayer(long playerId, string damagePreview)
    {
        if (string.IsNullOrWhiteSpace(damagePreview))
        {
            return;
        }

        if (playerId == Multiplayer.GetUniqueId())
        {
            ShowLocalDamageHistory(damagePreview);
            return;
        }

        RpcId((int)playerId, MethodName.ClientShowDamageHistory, damagePreview);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientShowDamageHistory(string damagePreview)
    {
        ShowLocalDamageHistory(damagePreview);
    }

    private void ShowLocalDamageHistory(string damagePreview)
    {
        if (_damageHistoryPanel == null)
        {
            AddDamageHistoryPanel();
        }

        _damageHistoryLabel.Text = $"Last 2s: {damagePreview}";
        _damageHistoryPanel.Visible = true;
        _damageHistoryHideAt = Godot.Time.GetTicksMsec() / 1000.0 + KillFeedEntryLifetime;
    }

    private void AddKillFeedEntry(string killerName, string victimName)
    {
        if (_killFeedContainer == null)
        {
            AddKillFeed();
        }

        var entryNode = CreateKillFeedEntryNode(killerName, victimName);
        _killFeedContainer.AddChild(entryNode);
        _killFeedContainer.MoveChild(entryNode, 0);
        _killFeedEntries.Insert(0, new KillFeedEntry(entryNode, Godot.Time.GetTicksMsec() / 1000.0));

        while (_killFeedEntries.Count > KillFeedMaxEntries)
        {
            RemoveKillFeedEntry(_killFeedEntries[^1]);
        }
    }

    private static Control CreateKillFeedEntryNode(string killerName, string victimName)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 38),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.025f, 0.035f, 0.72f),
            BorderColor = new Color(1f, 1f, 1f, 0.14f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        panel.AddChild(margin);

        var textStack = new VBoxContainer();
        textStack.AddThemeConstantOverride("separation", 2);
        margin.AddChild(textStack);

        var killLine = new Label
        {
            Text = $"{SafeName(killerName)} killed {SafeName(victimName)}",
            ClipText = true,
        };
        killLine.AddThemeFontSizeOverride("font_size", 16);
        killLine.AddThemeColorOverride("font_color", Colors.White);
        killLine.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        killLine.AddThemeConstantOverride("outline_size", 3);
        textStack.AddChild(killLine);

        return panel;
    }

    private void UpdateKillFeed()
    {
        if (_killFeedEntries.Count == 0)
        {
            return;
        }

        var now = Godot.Time.GetTicksMsec() / 1000.0;
        for (var i = _killFeedEntries.Count - 1; i >= 0; i--)
        {
            if (now - _killFeedEntries[i].CreatedAt < KillFeedEntryLifetime)
            {
                continue;
            }

            RemoveKillFeedEntry(_killFeedEntries[i]);
        }
    }

    private void RemoveKillFeedEntry(KillFeedEntry entry)
    {
        _killFeedEntries.Remove(entry);
        entry.Node.QueueFree();
    }

    private void UpdateDamageHistory()
    {
        if (_damageHistoryPanel == null || !_damageHistoryPanel.Visible)
        {
            return;
        }

        if (Godot.Time.GetTicksMsec() / 1000.0 >= _damageHistoryHideAt)
        {
            _damageHistoryPanel.Visible = false;
        }
    }

    private static string SafeName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }

    public void DisplayRoundInfo(string info)
    {
        _roundLabel.Text = info;
        Rpc(MethodName.displayRoundInfoclient, info);
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void displayRoundInfoclient(string info)
    {
        _roundLabel.Text = info;
    }

    public void ShowDrawUi(bool visible)
    {
        _drawUiContainer.Visible = visible;
        if (visible)
        {
            _lockButton.Disabled = false;
            _rerollButton.Disabled = false;
            UpdateRerollButtonText();
            _cardBox.Visible = true;
            ShowWaitLabel(false);
            _selectedCard = null;
        }
    }

    public void ShowWaitLabel(bool visible)
    {
        _waitLabel.Visible = visible;
    }

    public void UpdatePlayerHud(PlayerCharacter player)
    {
        if (player == null)
        {
            return;
        }

        UpdateDarknessEffect(player.StatBlock.GetStat(StatType.Darkness));
        if (player.TryGetComponent(out HealthComponent healthComponent))
        {
            _healthBar.SetHealth(healthComponent.CurrentHealth, healthComponent.MaxHealth);
        }

        if (player.TryGetComponent(out ItemManagerComponent imc))
        {
            RefreshItemsIfChanged(imc);
        }

        if (player.PlayerId == Multiplayer.GetUniqueId())
        {
            UpdateRerollButtonText(player);
        }
    }

    private void UpdateDarknessEffect(float darknessStacks)
    {
        if (_darknessEffect?.Material is not ShaderMaterial shaderMaterial)
        {
            return;
        }

        var fillAmount = Mathf.Clamp(darknessStacks / DarknessStacksForFullEffect, 0f, 1f);
        shaderMaterial.SetShaderParameter("fill_amount", fillAmount);
    }

    private void UpdateRerollButtonText(PlayerCharacter player = null)
    {
        if (_rerollButton == null || _gameManager == null)
        {
            return;
        }

        player ??= _gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());
        var cost = _gameManager.Settings.CardRerollCosts;
        var availablePurchases = 0;

        if (player != null && cost > 0)
        {
            availablePurchases = _gameManager.ScoreSystem.GetTeamScore(player) / cost;
        }

        var purchaseText = cost > 0
            ? $"x{availablePurchases}"
            : "unlimited";
        _rerollButton.SetText($"Reroll ({cost}) {purchaseText}");
    }

    private void _clear_card_box()
    {
        foreach (var child in _cardBox.GetChildren())
        {
            child.QueueFree();
        }
    }
    
    private void on_lock_clicked(Card card)
    {
        var player = _gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());
        if (player == null) return;
        
        _gameManager.Context.CardSystem.LockCard(player, card);
    }

    private void on_unlock_clicked(Card card)
    {
        var player = _gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());
        _gameManager.Context.CardSystem.UnlockCard(player, card);
    }

    private void _on_card_lock_pressed()
    {
        if (_selectedCard == null)
        {
            return;
        }

        _cardBox.Visible = false;
        _waitLabel.Visible = true;
        _lockButton.Disabled = true;
        _rerollButton.Disabled = true;
        EmitSignal(SignalName.CardLocked, Multiplayer.GetUniqueId(), _selectedCard.EffectGUID);
    }

    private void _on_card_reroll_pressed()
    {
        var player = _gameManager.GetPlayers().FirstOrDefault(p => p.PlayerId == Multiplayer.GetUniqueId());
        if (player == null)
        {
            return;
        }

        _selectedCard = null;
        _gameManager.Context.CardSystem.RerollHand(player);
    }

    private void on_card_clicked(Card card)
    {
        if (card == null)
        {
            return;
        }

        _selectedCard = card;
        cardTemplates.ForEach(ct => ct.NotifyCardSelected(card.EffectGUID));
    }

    private void RefreshItemsIfChanged(ItemManagerComponent imc)
    {
        var itemSignature = string.Join("|", imc.NetItems
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key}:{kvp.Value.IsDisabled}"));

        if (_displayedItemManager == imc && _displayedItemSignature == itemSignature)
        {
            return;
        }

        _displayedItemManager = imc;
        _displayedItemSignature = itemSignature;
        RefreshItems(imc);
    }

    private void RefreshItems(ItemManagerComponent imc)
    {
        foreach (var child in _itemContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var kvp in imc.NetItems)
        {
            var item = new TextureRect();
            item.Texture = kvp.Value.Icon;
            item.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
            _itemContainer.AddChild(item);
        }
    }

    private readonly record struct KillFeedEntry(Control Node, double CreatedAt);
}
