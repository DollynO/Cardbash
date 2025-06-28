using CardBase.Scripts.Cards;
using CardBase.Scripts.PlayerScripts;
using Godot;

namespace CardBase.Scripts.Items;

public partial class Item : BaseCardableObject
{
    protected Item(string guid) : base(guid)
    {
    }
    
    public virtual void ApplyItem(PlayerCharacter player)
    {
        
    }
}