using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public enum ColorPlateName
{
    Yellow,
    Purple,
    Orange,
    LightBlue,
    Red,
    Beige,
    Green,
    Pink,
    Blue,
    Salmon,
    DarkBlue,
    DarkPink,
    Brown,
    Lime,
    DarkGreen
}

public static class ColorPlate
{
    public static readonly Array<Godot.Color> Colors = new Array<Godot.Color>
    {
        new("f3c300"),
        new("875692"), 
        new("f38400"),
        new("a1caf1"),
        new("be0032"),
        new("c2b280"),
        new("008856"),
        new("e68fac"),
        new("0067a5"),
        new("f99379"),
        new("604e97"),
        new("b3446c"),
        new("882d17"),
        new("8db600"),
        new("2d3d26")
    };
    public static int MaxTeams => Colors.Count;
    
    public static Godot.Color GetColor(int teamNumber)
    {
        return Colors[teamNumber % Colors.Count];
    }
}