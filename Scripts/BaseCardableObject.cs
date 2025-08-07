using System;
using CardBase.Scripts.Cards;
using Godot;

namespace CardBase.Scripts;

public class BaseCardableObject : IBaseProperty
{
    public string DisplayName { get; protected init; }
    public string Description { get; protected init; }
    public string IconPath { get; protected init; }
    public string GUID { get; init; }
    
    public BaseCardableObject(string guid)
    {
        GUID = guid ?? throw new ArgumentNullException(nameof(guid), "GUID cannot be null");
    }
}