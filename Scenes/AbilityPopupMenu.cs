using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts;
using Godot;

public partial class AbilityPopupMenu : PanelContainer
{
	[Export] private PackedScene abilityFrameScene;
	[Export] private HBoxContainer frameContainer;
	List<AbilityFrame> abilityFrames = new();
	
	[Signal]
	public delegate void ClickedEventHandler(int index);
	
	public void ShowAbilities(IList<NetAbility> abilities)
	{
		abilityFrames.Clear();
		foreach (var child in frameContainer.GetChildren())
		{
			child.QueueFree();
		}
		
		var abilityList = abilities.OrderBy(x => x.Index).ToList();
		foreach (var ability in abilityList)
		{
			var af = abilityFrameScene.Instantiate<AbilityFrame>();
			abilityFrames.Add(af);
			frameContainer.AddChild(af);
			af.SlotIndex = ability.Index;
			af.UpdateUi(ability);
			af.Clicked += af_clicked;
			
		}
	}

	private void af_clicked(int slot)
	{
		EmitSignal(SignalName.Clicked, slot);
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
}
