using System;
using System.Collections.Generic;
using System.Linq;
using CardBase.Scripts.Items;
using Godot;

namespace CardBase.Scripts;

public partial class ItemManagerComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; set; }
    
    public readonly List<Item> Items = new();
    public readonly Dictionary<string, NetItem> NetItems= new(); 
    
    public event EventHandler<ItemEventArgs>? ItemAdded;
    public event EventHandler<ItemEventArgs>? ItemRemoved;
    public event EventHandler<ItemEventArgs>? ItemEnabled;
    public event EventHandler<ItemEventArgs>? ItemDisabled;

    public void AddNewItem(Item item)
    {
        Items.Add(item);
        item.ApplyItem(Parent);
        Rpc(MethodName.addNetItem,  item.InstanceGuid, new NetItem(item).ToDict());
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal =  true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void addNetItem(string guid, Godot.Collections.Dictionary<string, Variant> dict)
    {
        NetItems.Add(guid, NetItem.FromDict(dict));
        ItemAdded?.Invoke(this, new ItemEventArgs{InstanceGuid = guid});
    }

    public void RemoveItem(Item item)
    {
        Items.Remove(item);
        item.RemoveItem(Parent);
        Rpc(MethodName.removeNetItem,  item.InstanceGuid);
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal =  true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void removeNetItem(string guid)
    {
        NetItems.Remove(guid);
        ItemRemoved?.Invoke(this, new ItemEventArgs{InstanceGuid = guid});
    }
    
    public void TemporaryDisableItem(string itemInstanceGuid)
    {
        var item = Items.FirstOrDefault(i => i.InstanceGuid == itemInstanceGuid && !i.IsDisabled);
        item?.DisableItem(Parent);
        Rpc(MethodName.disableItem, itemInstanceGuid);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal =  true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void disableItem(string itemInstanceGuid)
    {
        var netItem = NetItems.FirstOrDefault(kvp => kvp.Key == itemInstanceGuid).Value;
        if (netItem != null)
        {
            netItem.IsDisabled = true;
            ItemDisabled?.Invoke(this, new ItemEventArgs{InstanceGuid = itemInstanceGuid});
        }
    }

    public void TemporaryEnableItem(string itemInstanceGuid)
    {
        var item = Items.FirstOrDefault(i => i.InstanceGuid == itemInstanceGuid && i.IsDisabled);
        item?.EnableItem(Parent);
        Rpc(MethodName.enableItem, itemInstanceGuid);
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal =  true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void enableItem(string itemInstanceGuid)
    {
        var netItem = NetItems.FirstOrDefault(kvp => kvp.Key == itemInstanceGuid).Value;
        if (netItem != null)
        {
            netItem.IsDisabled = false;
            ItemEnabled?.Invoke(this, new ItemEventArgs{InstanceGuid = itemInstanceGuid});
        }
    }
}

public class ItemEventArgs : EventArgs
{
    public string InstanceGuid { get; set; }
}