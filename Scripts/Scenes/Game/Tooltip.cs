using Godot;

public partial class Tooltip : Control
{
    [Export] private Label _description;
    [Export] private Label _name;

    public void Init(string description, string name)
    {
        ZIndex = 20;
        _description.Text = description;
        _name.Text = name;
    }

    public void ShowTooltip(Vector2 position)
    {
        GlobalPosition = position;
        Visible = true;
    }

    public void HideTooltip()
    {
        Visible = false;
    }
}
