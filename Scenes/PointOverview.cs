using Godot;
using CardBase.Scripts;

public partial class PointOverview : HBoxContainer
{
	[Export] private Label _placeLabel;
	[Export] private ColorRect _teamColor;
	[Export] private Label _scoreLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LoadNodes();
	}

	public void Update(int place, int color, int score)
	{
		if (!LoadNodes())
		{
			return;
		}
		
		_placeLabel.Text = $"{place}";
		_scoreLabel.Text = $"{score}";
		_teamColor.Color = ColorPlate.GetColor(color);

	}

	private bool LoadNodes()
	{
		_placeLabel ??= GetNodeOrNull<Label>("Place");
		_teamColor ??= GetNodeOrNull<ColorRect>("TeamColor");
		_scoreLabel ??= GetNodeOrNull<Label>("Points");

		return _placeLabel != null && _teamColor != null && _scoreLabel != null;
	}
}
