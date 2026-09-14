using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardBase.Prefabs.Cards;
using CardBase.Scripts.Cards;
using Godot;

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
    [Export] private HBoxContainer CardSelectionIndicator;
    private List<ColorRect> cardTypeCountIndicator = new();
    [Export] private ConfirmationDialog _unsavedDialog;
    private bool _shouldSaveChanges = false;
    private string _selectedDeckGuid;
    private bool _selectedDeckHasChanges = false;

    private int _selectedDeckIconNumber = 0;
    private Godot.Collections.Dictionary<Card, Counter> _cards = new();

    private PackedScene _cardTemplate;
    private PackedScene _deckCardDisplayLineTemplate;
    private PackedScene _mainScreen;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        _deleteButton.Disabled = true;
        _mainScreen = ResourceLoader.Load("res://Scenes/MainScreen.tscn") as PackedScene;

        _cardTemplate = ResourceLoader.Load("res://Prefabs/Cards/CardTemplate.res") as PackedScene;
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

        for (var i = 0; i < 12; i++)
        {
            var cr = new ColorRect();
            cr.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            CardSelectionIndicator.AddChild(cr);
            cardTypeCountIndicator.Add(cr);
        }

        reset_selected_deck();
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

    private async void _on_back_pressed()
    {
        await check_deck_changes();

        GlobalCardManager.Instance.SaveDecks();
        var sceneManager = GetNode<SceneManager>("..");
        sceneManager?.LoadMenuScene();
    }

    private void _on_new_deck_pressed()
    {
        var deck = new Deck();
        deck.DisplayName = "New Deck";
        deck.GUID = Guid.NewGuid().ToString();
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

    private TaskCompletionSource<bool> _tcs;
    private Task<bool> AskDiscardChangesAsync()
    {
        _tcs = new TaskCompletionSource<bool>();

        void OnConfirmed()
        {
            Cleanup();
            _tcs.TrySetResult(true);
        }

        void OnCanceled()
        {
            Cleanup();
            _tcs.TrySetResult(false);
        }

        void Cleanup()
        {
            _unsavedDialog.Confirmed -= OnConfirmed;
            _unsavedDialog.Canceled -= OnCanceled;
        }


        _unsavedDialog.Confirmed += OnConfirmed;
        _unsavedDialog.Canceled += OnCanceled;
        _unsavedDialog.PopupCentered();

        return _tcs.Task;
    }

    private async Task check_deck_changes()
    {
        if (_selectedDeckHasChanges)
        {
            // show dialog
            var shouldSave = await AskDiscardChangesAsync();
            if (shouldSave)
            {
                _on_save_deck_pressed();
            }
            else
            {
                reset_selected_deck();
            }
        }
    }

    private async void _on_deck_selected(Deck deck)
    {
        await check_deck_changes();

        if (deck != null)
        {
            reset_selected_deck();
            _deleteButton.Disabled = false;
            _selectedDeckGuid = deck.GUID;
            _selectedDeckName.Text = deck.DisplayName;
            _selectedDeckIcon.Texture = deck.Icon ?? IconLoader.Instance.LoadImage("res://Sprites/Cards/CardTypeIcon/AbilityTypeIcon.png");

            _cards.Clear();
            foreach (var kvp in deck.Cards)
            {
                _cards.Add(kvp.Key, kvp.Value);
            }

            display_selected_deck_cards(_cards);

        }
    }

    private void reset_selected_deck()
    {
        _selectedDeckName.Text = string.Empty;
        _selectedDeckIcon.Texture = null;
        _selectedDeckGuid = string.Empty;

        foreach (var child in _selectedDeckCardsContainer.GetChildren())
        {
            _selectedDeckCardsContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var indicator in cardTypeCountIndicator)
        {
            var colorRect = (ColorRect)indicator;
            colorRect.Color = ColorPlate.GetColor((int)ColorPlateName.Red);
        }

        _selectedDeckHasChanges = false;
    }

    private void display_selected_deck_cards(Godot.Collections.Dictionary<Card, Counter> cards)
    {
        if (cards.Count == 0)
        {
            return;
        }

        foreach (var child in _selectedDeckCardsContainer.GetChildren())
        {
            _selectedDeckCardsContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var entry in cards)
        {
            if (_deckCardDisplayLineTemplate.Instantiate() is DeckCardDisplayLine template)
            {
                template.UpdateDisplay(entry.Key, entry.Value);
                _selectedDeckCardsContainer.AddChild(template);
                template.CardRemoved += on_card_removed;
            }
        }

        for (var i = 0; i < cardTypeCountIndicator.Count; i++)
        {
            cardTypeCountIndicator[i].Color = i < _cards.Count ? ColorPlate.GetColor((int)ColorPlateName.Green) : ColorPlate.GetColor((int)ColorPlateName.Red);
        }
    }

    private void create_cards_type(CardType type, GridContainer container)
    {
        Godot.Collections.Dictionary<string, Card> cards = new();
        switch (type)
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
            var cardTemplate = _cardTemplate.Instantiate() as CardTemplate;
            cardTemplate.CardType = type;

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
        if (string.IsNullOrEmpty(_selectedDeckGuid))
        {
            return;
        }

        if (_cards.TryGetValue(card, out var counter))
        {
            if (counter.Count < 4)
            {
                counter.Count++;
            }
        }
        else
        {
            _cards.Add(card, new Counter());
        }

        _selectedDeckHasChanges = true;

        display_selected_deck_cards(_cards);
    }

    private void on_card_removed(Card card)
    {
        if (string.IsNullOrEmpty(_selectedDeckGuid))
        {
            return;
        }

        if (_cards.TryGetValue(card, out var counter))
        {
            counter.Count--;
            if (counter.Count <= 0)
            {
                _cards.Remove(card);
            }
        }

        _selectedDeckHasChanges = true;

        display_selected_deck_cards(_cards);
    }

    private void _on_delete_deck_pressed()
    {
        var selectedDeckId = get_deck_index(_selectedDeckGuid);
        if (selectedDeckId < 0)
        {
            return;
        }

        var selectedDeck = GlobalCardManager.Instance.Decks.FirstOrDefault(d => d.GUID == _selectedDeckGuid);
        if (selectedDeck != null)
        {
            GlobalCardManager.Instance.Decks.Remove(selectedDeck);
            _on_deck_selected(null);
            _deckList.RemoveItem(selectedDeckId);
        }
    }

    private void _on_save_deck_pressed()
    {
        var selectedDeckId = get_deck_index(_selectedDeckGuid);
        if (selectedDeckId < 0)
        {
            return;
        }

        var selectedDeck = GlobalCardManager.Instance.Decks.FirstOrDefault(d => d.GUID == _selectedDeckGuid);
        if (selectedDeck != null)
        {
            selectedDeck.DisplayName = _selectedDeckName.Text;
            selectedDeck.SetIcon(_selectedDeckIconNumber);
            selectedDeck.Cards.Clear();
            foreach (var card in _cards)
            {
                selectedDeck.Cards.Add(card.Key, card.Value);
            }

            _deckList.SetItemText(selectedDeckId, selectedDeck.DisplayName);
            _deckList.SetItemIcon(selectedDeckId, selectedDeck.Icon);
        }

        _selectedDeckHasChanges = false;
    }

    private int get_deck_index(string deckGuid)
    {
        if (string.IsNullOrEmpty(deckGuid))
        {
            return -1;
        }

        for (var i = 0; i < _deckList.ItemCount; i++)
        {
            var curDeck = (Deck)_deckList.GetItemMetadata(i);
            if (curDeck != null && curDeck.GUID == deckGuid)
            {
                return i;
            }
        }

        return -1;
    }
}
