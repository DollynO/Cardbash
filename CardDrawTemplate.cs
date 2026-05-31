using CardBase.Scripts;
using CardBase.Scripts.Cards;
using Godot;

public partial class CardDrawTemplate : Control
{
	[Export] private Panel _selectIndicator;
	[Export] private CardTemplate _cardTemplate;
	[Export] private ButtonPrefab _lockCard;
	[Export] private ButtonPrefab _unlockCard;

	private bool is_selected;
	private bool is_locked;
	private StyleBoxFlat indicatorStyle;
	
	[Signal]
	public delegate void CardClickedEventHandler(Card card);
	
	[Signal]
	public delegate void LockCardClickedEventHandler(Card card);
	
	[Signal]
	public delegate void UnLockCardClickedEventHandler(Card card);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		is_selected = false;
		indicatorStyle = _selectIndicator.GetThemeStylebox("panel") as StyleBoxFlat;
		indicatorStyle = indicatorStyle?.Duplicate() as StyleBoxFlat;
		_selectIndicator.AddThemeStyleboxOverride("panel", indicatorStyle);

		_cardTemplate.CardClicked += CardTemplateOnCardClicked;
		_lockCard.ButtonDown += LockCardOnButtonDown;
		_unlockCard.ButtonDown += UnlockCardOnButtonDown;
		
		SetProcess(false);
	}

	private void LockCardOnButtonDown()
	{
		EmitSignal(SignalName.LockCardClicked, _cardTemplate.Card);
	}

	private void UnlockCardOnButtonDown()
	{
		EmitSignal(SignalName.UnLockCardClicked, _cardTemplate.Card);
	}

	private void CardTemplateOnCardClicked(Card card)
	{
		setSelectedState(true);
		EmitSignal(SignalName.CardClicked, card);
	}

	public void SetCard(Card card)
	{
		_cardTemplate.Card = card;
		_cardTemplate.CardType = card.CardType;
	}
	
	public void NotifyCardSelected(string card_guid)
	{
		if (_cardTemplate.Card == null || card_guid != _cardTemplate.Card.EffectGUID)
		{
			setSelectedState(false);
		}
		else
		{
			setSelectedState(true);
		}
	}

	public void SetLockState(bool locked)
	{
		_unlockCard.Visible = locked;
		_lockCard.Visible = !locked;
	}

	public void NotifyCardUnlocked(string card_guid)
	{
		_lockCard.Visible = true;
		_unlockCard.Visible = true;
	}
	
	private void setSelectedState(bool selected)
	{
		is_selected = selected;
		indicatorStyle.BgColor = ColorPlate.Colors[selected ? (int)ColorPlateName.Green : (int)ColorPlateName.Red];
	}
}
