using Godot;

public partial class SceneManager : Node
{
	[Export] private PackedScene MenuScene;
	[Export] private PackedScene DeckBuilderScene;
	[Export] private PackedScene LobbyScene;
	[Export] private PackedScene GameScene;

	private Node Menu;
	private Node DeckBuilder;
	private Node Lobby;
	private Node Game;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Menu = MenuScene.Instantiate();
		DeckBuilder = DeckBuilderScene.Instantiate();
		Lobby = LobbyScene.Instantiate();
		Game = GameScene.Instantiate();
		LoadMenuScene();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ToggleFullscreen"))
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.ExclusiveFullscreen
				? DisplayServer.WindowMode.Windowed
				: DisplayServer.WindowMode.ExclusiveFullscreen);
		}
	}

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadMenuScene()
	{
		Remove(DeckBuilder);
		UnloadLobby();

		if (Menu.GetParent() == null)
		{
			AddChild(Menu);
		}
	}
	
	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadDeckBuilderScene()
	{
		Remove(Menu);
		AddChild(DeckBuilder);
	}

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadLobbyScene()
	{
		Remove(Menu);
		Lobby ??= LobbyScene.Instantiate();
		AddChild(Lobby);
	}

	public void LoadGameScene(GameModeSettings gameSettings)
	{
		UnloadLobby();
		if (Game is GameManager manager)
		{
			manager.SetStats(gameSettings);
		}
		AddChild(Game);
		Rpc(MethodName.loadGameOnClient);
	}

	[Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void loadGameOnClient()
	{
		UnloadLobby();
		AddChild(Game);
	}

	private void UnloadLobby()
	{
		Remove(Lobby);
		Lobby?.QueueFree();
		Lobby = null;
	}

	private void Remove(Node node)
	{
		if (node?.GetParent() != null)
		{
			RemoveChild(node);
		}
	}
}
