using System;
using CardBase.Scripts.Cards;
using Godot;

namespace CardBase.Scripts;

public class BaseCardableObject : IBaseProperty
{
    public string DisplayName { get; protected set; }
    public string Description { get; protected set; }
    public string IconPath { get; protected set; }
    public string GUID { get; init; }

    public BaseCardableObject(string guid)
    {
        GUID = guid ?? throw new ArgumentNullException(nameof(guid), "GUID cannot be null");
    }
}
