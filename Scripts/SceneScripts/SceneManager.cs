using Godot;

public partial class SceneManager : Node2D
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
	
	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadMenuScene()
	{
		RemoveChild(DeckBuilder);
		UnloadLobby();
		AddChild(Menu);
	}
	
	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadDeckBuilderScene()
	{
		RemoveChild(Menu);
		AddChild(DeckBuilder);
	}

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadLobbyScene()
	{
		RemoveChild(Menu);
		Lobby ??= LobbyScene.Instantiate();
		AddChild(Lobby);
	}

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadGameScene()
	{
		UnloadLobby();
		AddChild(Game);
	}

	private void UnloadLobby()
	{
		RemoveChild(Lobby);
		Lobby?.QueueFree();
		Lobby = null;
	}
}
