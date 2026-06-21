using System;
using CardBase.Scripts.Cards;
using CardBase.Scripts.GameSettings;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts.Items;

public abstract partial class Item : BaseCardableObject
{
    public bool IsDisabled { get; protected set; }
    public string InstanceGuid { get; init; }

    protected Item(string guid) : base(guid)
    {
        this.InstanceGuid = Guid.NewGuid().ToString("N");
    }

    public abstract void ApplyItem(IEntityComponent targetEntity);
    public abstract void RemoveItem(IEntityComponent targetEntity);

    public virtual void ApplyGameplayConfig(GameplayConfigEntry config)
    {
        if (config == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(config.DisplayName)) DisplayName = config.DisplayName;
        if (!string.IsNullOrWhiteSpace(config.Description)) Description = config.Description;
        if (!string.IsNullOrWhiteSpace(config.IconPath)) IconPath = config.IconPath;
    }

    protected float ConfigParam(string paramName, float fallback)
    {
        return GameplayConfigManager.GetItemParam(GUID, paramName, fallback);
    }

    protected int ConfigParam(string paramName, int fallback)
    {
        return GameplayConfigManager.GetItemParam(GUID, paramName, fallback);
    }

    public void DisableItem(IEntityComponent targetEntity)
    {
        IsDisabled = true;
        RemoveItem(targetEntity);
    }

    public void EnableItem(IEntityComponent targetEntity)
    {
        IsDisabled = false;
        ApplyItem(targetEntity);
    }
}

public sealed class NetItem
{
    public string DisplayName { get; init; }
    public string IconPath { get; init; }

    public bool IsDisabled { get; set; }

    public Texture2D Icon { get; init; }

    public NetItem(string displayName, string iconPath, bool isDisabled)
    {
        this.DisplayName = displayName;
        this.IconPath = iconPath;
        this.IsDisabled = isDisabled;
        this.Icon = IconLoader.Instance.LoadImage(iconPath);
    }

    public NetItem(Item item)
        : this(item.DisplayName, item.IconPath, item.IsDisabled)
    {
    }

    public Dictionary<string, Variant> ToDict()
    {
        return new Dictionary<string, Variant>()
        {
            { nameof(DisplayName), DisplayName },
            { nameof(IconPath), IconPath },
            { nameof(IsDisabled), IsDisabled },
        };
    }

    public static NetItem FromDict(Dictionary<string, Variant> dict)
    {
        return new NetItem((string)dict[nameof(DisplayName)],
            (string)dict[nameof(IconPath)],
            (bool)dict[nameof(IsDisabled)]);
    }
}
