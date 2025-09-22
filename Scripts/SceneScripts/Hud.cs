using Godot;
using System;
using System.Collections.Generic;
using CardBase.Scripts;
using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot.Collections;

public partial class Hud : CanvasLayer
{
	[Export] Array<AbilityFrame> _abilityFrames;
	[Export] private Container _drawUiContainer;
	[Export] private TextEdit _statsText;
	[Export] private Label _waitLabel;
	[Export] private HBoxContainer _cardBox;
	[Export] private ColorRect _darknessEffect;
	
	private PackedScene _abilityCardTemplate;
	private PackedScene _itemCardTemplate;

	[Signal]
	public delegate void CardLockedEventHandler(int playerId, Dictionary cardDict);
	
	private Card _selectedCard;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_clear_card_box();
		_abilityCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/AbiltyCardTemlate.res") as PackedScene;
		_itemCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/ItemCardTemlate.res") as PackedScene;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ShowDrawUi(bool visible, List<Card> cards)
	{
		_drawUiContainer.Visible = visible;
		if (visible)
		{
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
		
		for (var i = 0; i < _abilityFrames.Count; i++)
		{
			if (player.Abilities.Count > i)
			{
				_abilityFrames[i].UpdateUi(player.Abilities[i]);
			}
			else
			{
				_abilityFrames[i].UpdateUi(null);
			}
		}
		
		((ShaderMaterial)_darknessEffect.Material).SetShaderParameter("fill_amount", Math.Clamp(player.StatBlock.GetStat(StatType.Darkness) * 0.1, 0, 1));
		PrintStats(player.StatBlock);
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
				CardType.Ability => _abilityCardTemplate.Instantiate() as CardTemlate,
				CardType.Item => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.Spell => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.WorldModifier => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.Modifier => _itemCardTemplate.Instantiate() as CardTemlate,
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
		EmitSignal(SignalName.CardLocked, Multiplayer.GetUniqueId(), _selectedCard.ToDict());
	}
	
	private void on_card_clicked(Card card)
	{
		_selectedCard = card;
	}
	
	public void PrintStats(StatBlockComponent stats)
	{
		_statsText.Text = $"Movement Speed: {stats.GetStat(StatType.MovementSpeed)}\n" +
		                  $"Armor: {stats.GetStat(StatType.Armor)}\n" +
		                  $"Life: {stats.GetStat(StatType.Life)}\n" +
		                  $"Energy Shield: {stats.GetStat(StatType.EnergyShield)}\n";
	}
}
