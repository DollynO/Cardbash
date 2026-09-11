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
        if (_fpsLabel == null || !_fpsLabel.Visible)
        {
            return;
        }

        _fpsRefreshTimer += delta;
        if (_fpsRefreshTimer < FpsRefreshInterval)
        {
            return;
        }

        _fpsRefreshTimer = 0;
        _fpsLabel.Text = $"FPS: {(int)Math.Round(Engine.GetFramesPerSecond())}";
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
}
