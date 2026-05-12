using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

public partial class PlayerUi : Control
{
    [Export] private Panel PlayerOverviewPanel;
    [Export] private VBoxContainer PlayerOverview;
    [Export] private PackedScene PlayerOverviewScene;
    private GameManager gameManager;
    private PlayerCharacter currentPlayer;
    [Export] Array<AbilityFrame> _abilityFrames;
    [Export] private AbilityPopupMenu _abilityPopupMenu;

    private int slotCallerIndex;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayerOverviewPanel.Visible = false;
        gameManager = GetNode<GameManager>("/root/Main/Game");
        currentPlayer = gameManager.GetPlayers().FirstOrDefault(x => x.Name == Multiplayer.GetUniqueId().ToString());
        
        foreach (var abilityFrame in _abilityFrames)
        {
            abilityFrame.SlotIndex = abilityFrame.GetIndex();
            abilityFrame.Clicked += openAbilityMenu;
        }

        _abilityPopupMenu.Clicked += newAbilityIndexClicked;
    }

    private void newAbilityIndexClicked(int index)
    {
        _abilityPopupMenu.Visible = false;
        currentPlayer.AbilityComponent.SwapAbilities(slotCallerIndex, index);
    }
    
    private void openAbilityMenu(int index)
    {
        _abilityPopupMenu.Visible = true;
        _abilityPopupMenu.ShowAbilities(currentPlayer.AbilityComponent.GetNetAbilities());
        slotCallerIndex = index;
        _abilityPopupMenu.GlobalPosition = _abilityFrames[index].GlobalPosition - _abilityPopupMenu.Size;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("HudPlayerOverview"))
        {
            CreatePlayerOverview();
        }

        PlayerOverviewPanel.Visible = Input.IsActionPressed("HudPlayerOverview");

        currentPlayer ??= gameManager.GetPlayers().FirstOrDefault(x => x.Name == Multiplayer.GetUniqueId().ToString());
        if (currentPlayer == null)
        {
            return;
        }
        
        var networkAbilities = currentPlayer.AbilityComponent.GetNetAbilities();
        for (var i = 0; i < _abilityFrames.Count; i++)
        {
            var netAbility = networkAbilities.FirstOrDefault(a => a.Index == i);
            _abilityFrames[i].UpdateUi(netAbility);
        }
        foreach (var netAbility in networkAbilities)
        {
            if (_abilityFrames.Count < netAbility.Index)
            {
                GD.Print($"Net index error{netAbility.Index}");
                return;
            }
            
            _abilityFrames[netAbility.Index].UpdateUi(netAbility);    
        }
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
