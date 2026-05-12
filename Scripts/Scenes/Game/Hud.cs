using Godot;
using System;
using System.Collections.Generic;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;

public partial class Hud : CanvasLayer
{
    [Export] private Container _drawUiContainer;
    [Export] private TextEdit _statsText;
    [Export] private Label _waitLabel;
    [Export] private HBoxContainer _cardBox;
    [Export] private ColorRect _darknessEffect;
    [Export] private Label _roundLabel;
    [Export] private ButtonPrefab _lockButton;
    [Export] private HBoxContainer _itemContainer;

    [Export] private HealthBar _healthBar;

    [Export] private GridContainer statOverviewContainer;
    [Export] private PackedScene statOverviewScene;


    private PackedScene _abilityCardTemplate;
    private PackedScene _itemCardTemplate;

    [Signal]
    public delegate void CardLockedEventHandler(int playerId, string cardGuid);

    private Card _selectedCard;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _clear_card_box();
        _abilityCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/AbilityCardTemplate.res") as PackedScene;
        _itemCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/ItemCardTemplate.res") as PackedScene;
        _healthBar.AllowGreater = true;

        var element = statOverviewScene.Instantiate<StatOverviewElement>();
        var data = new StatOverviewElementData(
            "res://Sprites/StatOverview/crit.png",
            "Crit",
            "Crit change doubles damage",
            "20");
        statOverviewContainer.AddChild(element);
        element.Init(data);
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

    public void ShowDrawUi(bool visible, List<Card> cards)
    {
        _drawUiContainer.Visible = visible;
        if (visible)
        {
            _lockButton.Disabled = false;
            _clear_card_box();
            _displayDrawnCards(cards, _cardBox);
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
        PrintStats(player);

        if (player.TryGetComponent(out ItemManagerComponent imc))
        {
            RefreshItems(imc);
        }
    }

    private void _clear_card_box()
    {
        foreach (var child in _cardBox.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void _displayDrawnCards(List<Card> cards, HBoxContainer container)
    {
        foreach (var card in cards)
        {
            var cardTemplate = card.CardType switch
            {
                CardType.Ability => _abilityCardTemplate.Instantiate() as CardTemplate,
                CardType.Item => _itemCardTemplate.Instantiate() as CardTemplate,
                CardType.Spell => _itemCardTemplate.Instantiate() as CardTemplate,
                CardType.WorldModifier => _itemCardTemplate.Instantiate() as CardTemplate,
                CardType.Modifier => _itemCardTemplate.Instantiate() as CardTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(card.CardType), card.CardType, null)
            };

            if (cardTemplate == null)
            {
                continue;
            }

            cardTemplate.Name = card.EffectGUID;
            cardTemplate.Card = card;
            cardTemplate.CardClicked += on_card_clicked;
            container.AddChild(cardTemplate);
        }
        _cardBox.Visible = true;
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
    }

    private void PrintStats(PlayerCharacter player)
    {
        _statsText.Text = player.StatBlock.GetStatDebugText();
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
