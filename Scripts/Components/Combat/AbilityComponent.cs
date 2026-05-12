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
    
    public int Index { get; set; }

    public static NetAbility CreateFromAbility(Ability ability, int index)
    {
        return new NetAbility()
        {
            GUID = ability.GUID,
            IconPath = ability.IconPath,
            Index = index,
            Cooldowns = new Vector2((float)ability.CurrentCooldown, (float)ability.BaseCooldown),
            Stacks = new Vector2(ability.CurrentStack, ability.MaxStack)
        };
    }

    public Godot.Collections.Dictionary<string, Variant> ToDict()
    {
        return new Godot.Collections.Dictionary<string, Variant>()
        {
            { nameof(IconPath), IconPath },
            { nameof(GUID), GUID },
            { nameof(Index), Index }
        };
    }

    public static NetAbility CreateFromDict(Godot.Collections.Dictionary<string, Variant> dict)
    {
        return new NetAbility()
        {
            GUID = (string)dict[nameof(GUID)],
            IconPath = (string)dict[nameof(IconPath)],
            Index = (int)dict[nameof(Index)],
        };
    }
}

public partial class AbilityComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    public void SetParent(IEntityComponent component)
    {
        Parent = component;
    }

    public RingContainer RingContainer { get; private set; }

    public Dictionary<string, NetAbility> networkAbilities = new();
    public Dictionary<int, Ability> Abilities = new();
    private bool active = false;

    public event EventHandler<AbilityEventArgs> AbilityCasted;

    public override void _EnterTree()
    {
        Name = "AbilityComponent";
        base._EnterTree();
        RingContainer = new RingContainer
        {
            Name = "RingContainer"
        };
        AddChild(RingContainer);
        if (Parent.TryGetComponent(out HealthComponent hc))
        {
            hc.Death += OnPlayerDeath;
        }

        active = true;
    }

    private void OnPlayerDeath(object sender, EventArgs args)
    {
        InterruptAbilities();
        active = false;
    }

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
        foreach(var (key, ability) in Abilities) {
            if (active)
            {
                ability.ProcessAbility((float)delta);
                ability.HandleInput(keyStates[key], delta);
            }
            
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
        foreach (var ability in Abilities.Values)
        {
            ability.CancelAbility();
        }
    }

    public void RoundReset()
    {
        foreach (var ability in Abilities.Values)
        {
            ability.RoundReset();
        }
    }

    public bool AddUpdateAbility(string abilityGuid)
    {
        if (Abilities.Values.FirstOrDefault(a => a.GUID == abilityGuid) is { } ability)
        {
            ability.ApplyUpdate();
            return true;
        }

        var newAbility = (Ability)AbilityManager.Create(abilityGuid, (PlayerCharacter)Parent);
        var index = 0;
        
        if (Abilities.Count > 0)
        {
            var indexList = Abilities.Keys.ToList();
            indexList.Sort();

            var maxIndex = Mathf.Max(indexList.Last(), 4);
            for (var i = 0; i < maxIndex; i++)
            {
                if (indexList.Contains(i))
                {
                    continue;
                }

                index = i;
                break;
            }
        }

        Abilities.Add(index, newAbility);
        Rpc(MethodName.addNetworkAbility, newAbility.GUID, newAbility.IconPath, index);
        return true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void addNetworkAbility(string guid, string iconPath,  int index)
    {
        var newNetAbility = new NetAbility()
        {
            GUID = guid,
            IconPath = iconPath,
            Index = index,
        };
        networkAbilities.Add(guid, newNetAbility);
    }

    public void Enable()
    {
        active = true;
    }

    public void Disable()
    {
        InterruptAbilities();
        active = false;
    }

    public IList<NetAbility> GetNetAbilities()
    {
        return networkAbilities.Values.OrderBy(v => v.Index).ToList();
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
            networkAbilities[entry.Key].Cooldowns = new Vector2((float)abilityEntry["cdx"], (float)abilityEntry["cdy"]);
            networkAbilities[entry.Key].Stacks = new Vector2((int)abilityEntry["sx"], (int)abilityEntry["sy"]);
        }
    }

    public void Cleanup()
    {
        InterruptAbilities();
        foreach (var ability in Abilities.Values)
        {
            ability.ClearAbility();
        }
    }
    
    public void SwapAbilities(int targetIndex, int sourceIndex)
    {
        RpcId(1, MethodName.swapAbilitiesServer,  targetIndex, sourceIndex);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode =  MultiplayerPeer.TransferModeEnum.Reliable)]
    private void swapAbilitiesServer(int targetIndex, int sourceIndex)
    {
        Abilities.TryGetValue(sourceIndex, out var abilitySource);
        Abilities.TryGetValue(targetIndex, out var abilityTarget);

        if (abilityTarget is null)
        {
            Abilities.Remove(sourceIndex);
            Abilities.Add(targetIndex, abilitySource);
        }
        else
        {
            if (abilitySource is not null)
            {
                
                Abilities[targetIndex] = abilitySource;
                Abilities[sourceIndex] = abilityTarget; 
            }
        }
        
        networkAbilities.Clear();
        var dict = new Godot.Collections.Dictionary<string, Variant>();
        foreach (var kvp in Abilities)
        {
            networkAbilities.Add(kvp.Value.GUID, NetAbility.CreateFromAbility(kvp.Value, kvp.Key));
            dict.Add(kvp.Value.GUID, networkAbilities[kvp.Value.GUID].ToDict());
        }

        var senderId = Multiplayer.GetRemoteSenderId();

        if (senderId != 1)
        {
            RpcId(senderId, MethodName.syncNetworkAbilities, dict);
        }
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false,  TransferMode =  MultiplayerPeer.TransferModeEnum.Reliable)]
    private void syncNetworkAbilities(Godot.Collections.Dictionary<string, Variant> dict)
    {
        networkAbilities.Clear();
        foreach (var kvp in dict)
        {
            networkAbilities.Add(kvp.Key, NetAbility.CreateFromDict(kvp.Value.AsGodotDictionary<string, Variant>()));
        }
    }
}
