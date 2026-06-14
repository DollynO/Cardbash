using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;

public partial class Hud : CanvasLayer
{
    [Export] private Container _drawUiContainer;
    [Export] private Label _waitLabel;
    [Export] private HBoxContainer _cardBox;
    [Export] private ColorRect _darknessEffect;
    [Export] private Label _roundLabel;
    [Export] private ButtonPrefab _lockButton;
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
    }
    
    private void on_round_start(object sender, MatchEventArgs args)
    {
        updatePointsTable();
    }
    
    private void score_changed(object sender, ScoreEventArgs args)
    {
        updatePointsTable();
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
            cardTemplate.SetCard(card);
            cardTemplate.SetLockState(kvp.locked);
            cardTemplate.CardClicked += on_card_clicked;
            cardTemplate.LockCardClicked += on_lock_clicked;
            cardTemplate.UnLockCardClicked += on_unlock_clicked;
            cardTemplates.Add(cardTemplate);
            _cardBox.AddChild(cardTemplate);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
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

        ((ShaderMaterial)_darknessEffect.Material).SetShaderParameter("fill_amount", Math.Clamp(player.StatBlock.GetStat(StatType.Darkness) * 0.1, 0, 1));
        if (player.TryGetComponent(out HealthComponent healthComponent))
        {
            _healthBar.SetHealth(healthComponent.CurrentHealth, healthComponent.MaxHealth);
        }

        if (player.TryGetComponent(out ItemManagerComponent imc))
        {
            RefreshItemsIfChanged(imc);
        }
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
        EmitSignal(SignalName.CardLocked, Multiplayer.GetUniqueId(), _selectedCard.EffectGUID);
    }

    private void on_card_clicked(Card card)
    {
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
