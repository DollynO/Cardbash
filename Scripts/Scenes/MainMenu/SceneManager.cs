using Godot;

public partial class SceneManager : Node
{
    public static SceneManager Instance;

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
        Instance = this;
        Menu = MenuScene.Instantiate();
        DeckBuilder = DeckBuilderScene.Instantiate();
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
        UnloadGame();

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
        Game ??= GameScene.Instantiate();

        if (Game is GameManager manager)
        {
            manager.SetStats(gameSettings);
        }
        AddChild(Game);
        Rpc(MethodName.loadGameOnClient);
    }

    public void LeaveGame()
    {
        UnloadGame();
        AddChild(Menu);
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void loadGameOnClient()
    {
        UnloadLobby();

        Game ??= GameScene.Instantiate();
        AddChild(Game);
    }

    private void UnloadLobby()
    {
        Remove(Lobby);
        Lobby?.QueueFree();
        Lobby = null;
    }

    private void UnloadGame()
    {
        Remove(Game);
        Game?.QueueFree();
        Game = null;
    }

    private void Remove(Node node)
    {
        if (node?.GetParent() != null)
        {
            RemoveChild(node);
        }
    }
}
