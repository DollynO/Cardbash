using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Abilities;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts;

public class NetAbility
{
    public Vector2 Stacks { get; set; }
    public Vector2 Cooldowns { get; set; }
    public Texture2D Icon { get; private set; }

    public string IconPath
    {
        get => iconPath;
        set
        {
            iconPath = value;
            Icon = IconLoader.Instance.LoadImage(value);
        }
    }
    private string iconPath;
    public string GUID { get; set; }
}

public partial class AbilityComponent : Node, IComponent
{
    public IEntityComponent Parent { get; private set;  }
    public void SetParent(IEntityComponent component)
    {
        Parent = component;
    }
    
    public Dictionary<string, NetAbility> networkAbilities = new();
    public List<Ability> Abilities = new();
    private Ability? activeAbility;
    
    public event EventHandler<AbilityEventArgs>? AbilityCasted;

    public void NotifyAbilityCasted(Ability ability)
    {
        AbilityCasted?.Invoke(this, new AbilityEventArgs(ability));
    }

    public void ProcessAbilities(double delta, AbilityKeyState[] keyStates)
    {
        if (keyStates.Length < Abilities.Count)
        {
            throw new ArgumentOutOfRangeException();
        }

        var dict = new Godot.Collections.Dictionary<string, Variant>();
        for (var i = 0; i < Abilities.Count; i++)
        {
            var ability = Abilities[i];
            ability.ProcessAbility((float)delta);
            ability.HandleInput(keyStates[i], delta);
            ability.UpdateCooldown(delta);
            var netAbilityDict = new Godot.Collections.Dictionary<string, Variant>();
            dict.Add(ability.GUID, netAbilityDict);
            netAbilityDict["cdx"] = ability.CurrentCooldown;
            netAbilityDict["cdy"] = ability.BaseCooldown;
            netAbilityDict["sx"] = ability.CurrentStack;
            netAbilityDict["sy"] = ability.MaxStack;
        }

        Rpc(MethodName.updateAbilities, dict);
    }

    public void InterruptAbilities()
    {
        foreach (var ability in Abilities)
        {
            ability.CancelAbility();
        }
    }
    
    public bool AddUpdateAbility(string abilityGuid)
    {
        if (Abilities.FirstOrDefault(a => a.GUID == abilityGuid) is { } ability)
        {
            ability.ApplyUpdate();
            return true;
        }
        
        if (Abilities.Count >= 4)
        {
            return false;
        }
        
        var newAbility = (Ability)AbilityManager.Create(abilityGuid, (PlayerCharacter)Parent);
        Abilities.Add(newAbility);
        Rpc(MethodName.addNetworkAbility, newAbility.GUID, newAbility.IconPath);
        return true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void addNetworkAbility(string guid, string iconPath)
    {
        var newNetAbility = new NetAbility()
        {
            GUID = guid,
            IconPath = iconPath,
        };
        networkAbilities.Add(guid, newNetAbility);
    }
    
    public Ability? GetActiveAbility()
    {
        return activeAbility;
    }

    public IList<NetAbility> GetNetAbilities()
    {
        return networkAbilities.Values.ToList();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void updateAbilities(Variant data)
    {
        var id = ((Node)this.Parent).Name;
        if (Multiplayer.GetUniqueId() != long.Parse(id))
        {
            return;
        }
        
        var dict = data.AsGodotDictionary<string, Variant>();

        foreach (var entry in dict)
        {
            var abilityEntry = entry.Value.AsGodotDictionary<string, Variant>();
            networkAbilities[entry.Key].Cooldowns = new Vector2((float)abilityEntry["cdx"],  (float)abilityEntry["cdy"]);
            networkAbilities[entry.Key].Stacks = new Vector2((int)abilityEntry["sx"],  (int)abilityEntry["sy"]);
        }
    }
}