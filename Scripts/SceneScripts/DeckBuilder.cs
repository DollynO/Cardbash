using System;
using System.Diagnostics;
using System.Linq;
using CardBase.Prefabs.Cards;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.Cards;
using CardBase.Scripts.Items;
using Godot;
using Godot.Collections;
using SGeneric = System.Collections.Generic;

namespace CardBase.Scripts.SceneScripts;

public partial class DeckBuilder : Control
{
	[Export] private TextureButton _saveButton;
	[Export] private TextureButton _deleteButton;
	
	[Export] private GridContainer _abilityCardContainer;
	[Export] private GridContainer _itemCardContainer;
	[Export] private ItemList _deckList;
	[Export] private Panel _selectedDeckPanel;
	[Export] private VBoxContainer _selectedDeckCardsContainer;
	[Export] private LineEdit _selectedDeckName;
	[Export] private TextureRect _selectedDeckIcon;
	private int _selectedDeckIconNumber = 0;
	
	private PackedScene _abilityCardTemplate;
	private PackedScene _itemCardTemplate;
	private PackedScene _deckCardDisplayLineTemplate;
	private PackedScene _mainScreen;
	private Array<Card> _cards = new();
	private Deck _selectedDeck;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_deleteButton.Disabled = true;
		_mainScreen = ResourceLoader.Load("res://Scenes/MainScreen.tscn") as PackedScene;
		_cards.Clear();
		_abilityCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/AbiltyCardTemlate.res") as PackedScene;
		_itemCardTemplate = ResourceLoader.Load("res://Prefabs/Cards/ItemCardTemlate.res") as PackedScene;
		_deckCardDisplayLineTemplate = ResourceLoader.Load("res://Prefabs/Cards/DeckCardDisplayLine.res") as PackedScene;
		
		_deckList.ItemSelected += id => _on_deck_selected((Deck)_deckList.GetItemMetadata((int)id));

		foreach (var card in _abilityCardContainer.GetChildren())
		{
			_abilityCardContainer.RemoveChild(card);
			card.QueueFree();
		}
		create_cards_type(CardType.Ability, _abilityCardContainer);
		
		foreach (var card in _itemCardContainer.GetChildren())
		{
			_itemCardContainer.RemoveChild(card);
			card.QueueFree();
		}
		create_cards_type(CardType.Item, _itemCardContainer);

		load_deck_list();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void load_deck_list()
	{
		foreach (var deck in GlobalCardManager.Instance.Decks)
		{
			add_deck_to_list(deck);
		}
	}
	
	private void _on_back_pressed()
	{
		GlobalCardManager.Instance.SaveDecks();
		var sceneManager = GetNode<SceneManager>("..");
		sceneManager?.LoadMenuScene();
	}

	private void _on_new_deck_pressed()
	{
		var deck = new Deck();
		deck.DisplayName = "New Deck";
		deck.SetIcon((int)DeckIconNumber.Red);
		GlobalCardManager.Instance.Decks.Add(deck);
		add_deck_to_list(deck);
		_on_deck_selected(deck);
	}

	private void add_deck_to_list(Deck deck)
	{
		var index = _deckList.AddItem(deck.DisplayName, deck.Icon);
		_deckList.SetItemMetadata(index, deck);
		_deckList.Select(index);
	}

	private void _on_deck_selected(Deck deck)
	{
		if (deck != null)
		{
			_selectedDeckPanel.Visible = true;
			_deleteButton.Disabled = false;
			_selectedDeck = deck;
			_selectedDeckName.Text = _selectedDeck.DisplayName;
			_selectedDeckIcon.Texture = _selectedDeck.Icon ?? GD.Load<Texture2D>("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png");
			
			display_selected_deck_cards();
			return;
		}
		
		_selectedDeckPanel.Visible = false;
		_deleteButton.Disabled = true;
		_selectedDeck = null;
		_selectedDeckName.Text = string.Empty;
		_selectedDeckIcon.Texture = null;
	}

	private void display_selected_deck_cards()
	{
		if (_selectedDeck?.Cards == null)
		{
			return;
		}

		foreach (var child in _selectedDeckCardsContainer.GetChildren())
		{
			_selectedDeckCardsContainer.RemoveChild(child);	
		}
		
		foreach (var entry in _selectedDeck.Cards)
		{
			var template = _deckCardDisplayLineTemplate.Instantiate() as DeckCardDisplayLine;
			if (template != null)
			{
				template.UpdateDisplay(entry.Key, entry.Value);
				_selectedDeckCardsContainer.AddChild(template);
				template.CardRemoved += on_card_removed;
			}
		}
	}
	
	private void create_cards_type(CardType type, GridContainer container)
	{
		Dictionary<string, Card> cards = new();
		switch(type)
		{
			case CardType.Ability:
				foreach (var kvp in GlobalCardManager.Instance.AbilityCards)
				{
					cards.Add(kvp.Key, kvp.Value);
				}
				break;
			case CardType.Item:
				foreach (var kvp in GlobalCardManager.Instance.ItemCards)
				{
					cards.Add(kvp.Key, kvp.Value);
				}
				break;
			case CardType.Spell:
			case CardType.WorldModifier:
			case CardType.Modifier:
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
		foreach (var card in cards.Values)
		{
			var cardTemplate = type switch
			{
				CardType.Ability => _abilityCardTemplate.Instantiate() as CardTemlate,
				CardType.Item => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.Spell => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.WorldModifier => _itemCardTemplate.Instantiate() as CardTemlate,
				CardType.Modifier => _itemCardTemplate.Instantiate() as CardTemlate,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
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
	}

	// -- button clicks
	private void on_card_clicked(Card card)
	{
		if (_selectedDeck == null)
		{
			return;
		}
		
		_selectedDeck.AddCard(card);
		display_selected_deck_cards();
	}

	private void on_card_removed(Card card)
	{
		if (_selectedDeck == null)
		{
			return;
		}
		
		if (_selectedDeck.RemoveCard(card))
		{
			_selectedDeckCardsContainer.RemoveChild(card);
		}
	}

	private void _on_delete_deck_pressed()
	{
		var selectedDeckId = get_deck_index(_selectedDeck);
		if (selectedDeckId < 0)
		{
			return;
		}
		
		GlobalCardManager.Instance.Decks.Remove(_selectedDeck);
		_on_deck_selected(null);
		_deckList.RemoveItem(selectedDeckId);
	}

	private void _on_save_deck_pressed()
	{
		var selectedDeckId = get_deck_index(_selectedDeck);
		if (selectedDeckId < 0)
		{
			return;
		}
		
		_selectedDeck.DisplayName = _selectedDeckName.Text;
		_selectedDeck.SetIcon(_selectedDeckIconNumber);
		_deckList.SetItemText(selectedDeckId, _selectedDeck.DisplayName);
		_deckList.SetItemIcon(selectedDeckId, _selectedDeck.Icon);
	}

	private int get_deck_index(Deck deck)
	{
		if (deck == null)
		{
			return -1;
		}
		
		for (var i = 0; i < _deckList.ItemCount; i++)
		{
			var curDeck = (Deck)_deckList.GetItemMetadata(i);
			if (curDeck == deck)
			{
				return i;
			}
		}

		return -1;
	}
}