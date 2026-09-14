using Godot;
using System;
using CardBase.Scripts;
using CardBase.Scripts.PlayerScripts;

public partial class OverviewPlayer : Control
{
    [Export] private Label playerName;
    [Export] private Label points;
    [Export] private ColorRect teamColor;
    [Export] private TextureRect[] abilityIcons;
    [Export] private HBoxContainer itemContainer;

    public void Update(PlayerCharacter player, int score)
    {
        playerName.Text = player.PlayerName;
        points.Text = $"Score {score}   K/D {player.Kills}/{player.Deaths}";
        teamColor.Color = ColorPlate.GetColor(player.TeamId);
        foreach (var ability in player.AbilityComponent.GetNetAbilities())
        {
            if (ability.Index >= abilityIcons.Length)
            {
                break;
            }

            abilityIcons[ability.Index ].Texture = IconLoader.Instance.LoadImage(ability.IconPath);
        }

        foreach (var child in itemContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var item in player.ItemManagerComponent.NetItems)
        {
            var tr = new TextureRect();
            tr.Texture = item.Value.Icon;
            tr.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
            itemContainer.AddChild(tr);
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
