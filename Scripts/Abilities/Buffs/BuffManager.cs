using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities.Buffs;

[GlobalClass]
public partial class BuffManagerComponent : Node
{
    private List<Buff> activeBuffs = new();
    [Export] private HBoxContainer buffContainer;

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        var expiredBuffs = new List<Buff>();
        foreach (var buff in activeBuffs)
        {
            if (buff.OnTick((float)delta))
            {
                expiredBuffs.Add(buff);
            }
        }

        foreach (var buff in expiredBuffs)
        {
            activeBuffs.Remove(buff);
        }
    }

    public void ApplyBuff(Buff buff)
    {
        var existing = activeBuffs
            .FirstOrDefault(b => b.GetType() == buff.GetType() && b.Caller == buff.Caller);
        if (existing == null)
        {
            activeBuffs.Add(buff);
            var buffIcon = new BuffIcon();
            buffIcon.SetBuff(ref buff);
            buffContainer.AddChild(buffIcon);
        }
        
        buff.OnActivate();

    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void rpcUpdateBuff(Dictionary buffDict)
    {
           
    }
}

public static class BuffManager
{
    public static System.Collections.Generic.Dictionary<string, Func<PlayerCharacter, PlayerCharacter, Buff>> Abilities = new()
    {
        {"088DF86D-7101-4BBF-A331-17B625D11379", (creator, target)=>new BurnDebuff(creator, target)},
    };
    
    public static Buff Create(string GUID, PlayerCharacter creator, PlayerCharacter target)
    {
        return Abilities.TryGetValue(GUID, out var constructor) ? constructor(creator, target) : null;
    }
}