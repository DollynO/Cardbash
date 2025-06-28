using CardBase.Scripts.Cards;
using Godot;
using Godot.Collections;

namespace CardBase.Prefabs.Cards;

public partial class DeckCardDisplayLine : ColorRect
{
	[Export] private Array<TextureRect> _cardTypeIcons;

	[Export] private Label _cardNameLabel;

	[Export] private Label _cardCountLabel;

	private Card _card;
	private Counter _counter;

	[Signal]
	public delegate void CardRemovedEventHandler(Card card);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateDisplay(Card card, Counter cnt)
	{
		_cardNameLabel.Text = card.DisplayName;
		_cardTypeIcons[(int)card.CardType].Visible = true;
		_cardCountLabel.Text = cnt.Count.ToString();
		_counter = cnt;
		_card = card;
	}

	private void _on_card_added()
	{
		if (_counter == null)
		{
			return;
		}
		
		_counter.Count++;
		_cardCountLabel.Text = _counter.Count.ToString();
	}

	private void _on_card_removed()
	{
		if (_counter == null)
		{
			return;
		}
		
		_counter.Count--;
		if (_counter.Count == 0)
		{
			EmitSignal(SignalName.CardRemoved, _card);
			QueueFree();
		}
		else
		{
			_cardCountLabel.Text = _counter.Count.ToString();
		}
	}
}