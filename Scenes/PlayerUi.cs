using Godot;

public partial class PlayerUi : Control
{
	[Export] private Panel PlayerOverviewPanel;
	[Export] private VBoxContainer PlayerOverview;
	[Export] private PackedScene PlayerOverviewScene;
	private GameManager gameManager;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PlayerOverviewPanel.Visible = false;
		gameManager = GetNode<GameManager>("/root/Main/Game");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("HudPlayerOverview"))
		{
			CreatePlayerOverview();	
		}
		
		PlayerOverviewPanel.Visible = Input.IsActionPressed("HudPlayerOverview");
	}

	private void CreatePlayerOverview()
	{
		foreach (var child in PlayerOverview.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var player in gameManager.GetPlayers())
		{
			var po = PlayerOverviewScene.Instantiate<OverviewPlayer>();
			po.Name = player.Name;
			po.Update(player);

			PlayerOverview.AddChild(po);
		}
	}
}
