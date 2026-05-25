using System.Linq;
using CardBase.Scripts;
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
    
    [Export] private GridContainer statOverviewContainer;
    [Export] private PackedScene statOverviewScene;
    
    private Dictionary<StatType, StatOverviewElement> _statOverviewElements = new();

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

    private void createStatOverview()
    {
        _statOverviewElements.Clear();
        foreach (var child in statOverviewContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        if (currentPlayer.TryGetComponent(out StatblockComponent component))
        {
            var element = statOverviewScene.Instantiate<StatOverviewElement>();
            var data = new StatOverviewElementData(
                "res://Sprites/StatOverview/crit.png",
                "Crit",
                "Crit change doubles damage",
                component.GetStat(StatType.CritChance).ToString("0.00"));
            statOverviewContainer.AddChild(element);
            element.Init(data);   
            _statOverviewElements.Add(StatType.CritChance, element);
            
            element =  statOverviewScene.Instantiate<StatOverviewElement>();
            data = new StatOverviewElementData(
                "res://Sprites/StatOverview/energy_shield_icon.png",
                "Energy Shield",
                "Shields for magic damage",
                component.GetStat(StatType.EnergyShield).ToString("0.00"));
            statOverviewContainer.AddChild(element);
            element.Init(data);
            _statOverviewElements.Add(StatType.EnergyShield, element);
            
            element =  statOverviewScene.Instantiate<StatOverviewElement>();
            data = new StatOverviewElementData(
                "res://Sprites/StatOverview/amor_icon.png",
                "Armor",
                "Defends the player from physical damage",
                component.GetStat(StatType.Armor).ToString("0.00"));
            statOverviewContainer.AddChild(element);
            element.Init(data);
            _statOverviewElements.Add(StatType.Armor, element);
            
            element =  statOverviewScene.Instantiate<StatOverviewElement>();
            data = new StatOverviewElementData(
                "res://Sprites/StatOverview/movement.png",
                "Movement speed",
                "Movement speed of the player",
                component.GetStat(StatType.MovementSpeed).ToString("0"));
            statOverviewContainer.AddChild(element);
            element.Init(data);
            _statOverviewElements.Add(StatType.MovementSpeed, element);
        }
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

        createStatOverview();

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
