using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Abilities.Buffs;

[GlobalClass]
public partial class BuffManager : Node
{
    private List<Buff> activeBuffs = new();

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
        }
        
        buff.OnActivate();
        //RpcId(buff.Target.PlayerId, nameof(rpcUpdateBuff), buff.ToDict());
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void rpcUpdateBuff(Dictionary buffDict)
    {
        
    }
}