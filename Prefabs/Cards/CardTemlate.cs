using Godot;
using System;
using CardBase.Scripts;
using CardBase.Scripts.Cards;

public partial class CardTemlate : TextureRect
{
    [Export] Label DescriptionLabel;
    [Export] Label NameLabel;
    [Export] TextureRect Image;
    [Export] private TextureRect HoverFrame;
    [Export] private Panel DetailDescriptionPanel;
    [Export] private Label DetailDescriptionLabel;
    
    
    [Signal]
    public delegate void CardClickedEventHandler(Card card);
    
    
    private Card card;
    public Card Card { 
        get => card;
        set
        {
            card = value;
            NameLabel.Text = card.DisplayName;
            DescriptionLabel.Text = card.Description;
            DetailDescriptionLabel.Text = card.Description;

            if (card.IconPath != null)
            {
                Image.Texture = IconLoader.Instance.LoadImage(card.IconPath);
            }
        }
    }

    public override void _Ready()
    {
        
    }

    private void _on_mouse_entered()
    {
        HoverFrame.Visible = true;
        DetailDescriptionPanel.Visible = true;
    }

    private void _on_mouse_exited()
    {
        HoverFrame.Visible = false;
        DetailDescriptionPanel.Visible = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left})
        {
            EmitSignal(SignalName.CardClicked, Card);
        }
    }
}
