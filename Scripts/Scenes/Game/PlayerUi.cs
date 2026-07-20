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
    
    private System.Collections.Generic.Dictionary<StatType, StatOverviewElement> _statOverviewElements = new();
    private PlayerCharacter _statOverviewPlayer;

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
        EventBus.Instance.CardSystemEventBus.CardLockedEventHandler += card_locked;
    }

    private void card_locked(object sender, CardEventArgs args)
    {
        
    }

    private void createStatOverview()
    {
        _statOverviewElements.Clear();
        foreach (var child in statOverviewContainer.GetChildren())
        {
            child.QueueFree();
        }

        _statOverviewPlayer = currentPlayer;
        
        if (currentPlayer.TryGetComponent(out StatblockComponent component))
        {
            AddStatOverviewElement(
                StatType.CritChance,
                "res://Sprites/StatOverview/crit.png",
                "Crit",
                "Crit change doubles damage",
                component.GetStat(StatType.CritChance).ToString("0.00"));

            AddStatOverviewElement(
                StatType.EnergyShield,
                "res://Sprites/StatOverview/energy_shield_icon.png",
                "Energy Shield",
                "Shields for magic damage",
                component.GetStat(StatType.EnergyShield).ToString("0.00"));

            AddStatOverviewElement(
                StatType.Armor,
                "res://Sprites/StatOverview/amor_icon.png",
                "Armor",
                "Defends the player from physical damage",
                component.GetStat(StatType.Armor).ToString("0.00"));

            AddStatOverviewElement(
                StatType.MovementSpeed,
                "res://Sprites/StatOverview/movement.png",
                "Movement speed",
                "Movement speed of the player",
                component.GetStat(StatType.MovementSpeed).ToString("0"));

            AddStatOverviewElement(
                StatType.DmgFireBonus,
                "res://Sprites/Items/fire_orb.png",
                "Fire damage",
                "Increased fire damage",
                FormatPercentBonus(component.GetStat(StatType.DmgFireBonus)));

            AddStatOverviewElement(
                StatType.DmgIceBonus,
                "res://Sprites/Items/ice_orb.png",
                "Ice damage",
                "Increased ice damage",
                FormatPercentBonus(component.GetStat(StatType.DmgIceBonus)));

            AddStatOverviewElement(
                StatType.DmgLightningBonus,
                "res://Sprites/Items/lightning_orb.png",
                "Lightning damage",
                "Increased lightning damage",
                FormatPercentBonus(component.GetStat(StatType.DmgLightningBonus)));

            AddStatOverviewElement(
                StatType.DmgPoisonBonus,
                "res://Sprites/Items/poison_orb.png",
                "Poison damage",
                "Increased poison damage",
                FormatPercentBonus(component.GetStat(StatType.DmgPoisonBonus)));

            AddStatOverviewElement(
                StatType.DmgPhysicalBonus,
                "res://Sprites/Items/physical_orb.png",
                "Physical damage",
                "Increased physical damage",
                FormatPercentBonus(component.GetStat(StatType.DmgPhysicalBonus)));

            AddStatOverviewElement(
                StatType.DmgHolyBonus,
                "res://Sprites/Items/holy_orb.png",
                "Holy damage",
                "Increased holy damage",
                FormatPercentBonus(component.GetStat(StatType.DmgHolyBonus)));

            AddStatOverviewElement(
                StatType.DmgDarknessBonus,
                "res://Sprites/Items/darkness_orb.png",
                "Darkness damage",
                "Increased darkness damage",
                FormatPercentBonus(component.GetStat(StatType.DmgDarknessBonus)));
        }
    }

    private void AddStatOverviewElement(
        StatType statType,
        string iconPath,
        string name,
        string description,
        string value)
    {
        var element = statOverviewScene.Instantiate<StatOverviewElement>();
        var data = new StatOverviewElementData(iconPath, name, description, value);
        statOverviewContainer.AddChild(element);
        element.Init(data);
        _statOverviewElements[statType] = element;
    }

    private void updateStatOverview()
    {
        if (currentPlayer == null)
        {
            return;
        }

        if (_statOverviewPlayer != currentPlayer || _statOverviewElements.Count == 0)
        {
            createStatOverview();
            return;
        }

        if (!currentPlayer.TryGetComponent(out StatblockComponent component))
        {
            return;
        }

        updateStatValue(StatType.CritChance, component.GetStat(StatType.CritChance).ToString("0.00"));
        updateStatValue(StatType.EnergyShield, component.GetStat(StatType.EnergyShield).ToString("0.00"));
        updateStatValue(StatType.Armor, component.GetStat(StatType.Armor).ToString("0.00"));
        updateStatValue(StatType.MovementSpeed, component.GetStat(StatType.MovementSpeed).ToString("0"));
        updateStatValue(StatType.DmgFireBonus, FormatPercentBonus(component.GetStat(StatType.DmgFireBonus)));
        updateStatValue(StatType.DmgIceBonus, FormatPercentBonus(component.GetStat(StatType.DmgIceBonus)));
        updateStatValue(StatType.DmgLightningBonus, FormatPercentBonus(component.GetStat(StatType.DmgLightningBonus)));
        updateStatValue(StatType.DmgPoisonBonus, FormatPercentBonus(component.GetStat(StatType.DmgPoisonBonus)));
        updateStatValue(StatType.DmgPhysicalBonus, FormatPercentBonus(component.GetStat(StatType.DmgPhysicalBonus)));
        updateStatValue(StatType.DmgHolyBonus, FormatPercentBonus(component.GetStat(StatType.DmgHolyBonus)));
        updateStatValue(StatType.DmgDarknessBonus, FormatPercentBonus(component.GetStat(StatType.DmgDarknessBonus)));
    }

    private void updateStatValue(StatType statType, string value)
    {
        if (_statOverviewElements.TryGetValue(statType, out var element))
        {
            element.UpdateValue(value);
        }
    }

    private static string FormatPercentBonus(float value)
    {
        var percent = value * 100f;
        return percent > 0 ? $"+{percent:0}%" : $"{percent:0}%";
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

        updateStatOverview();

        var networkAbilities = currentPlayer.AbilityComponent.GetNetAbilities();
        for (var i = 0; i < _abilityFrames.Count; i++)
        {
            var netAbility = networkAbilities.FirstOrDefault(a => a.Index == i);
            _abilityFrames[i].UpdateUi(netAbility);
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
