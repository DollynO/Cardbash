using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public interface IDictAble<T> where T:new()
{
    public Dictionary<string, Variant> ToDict();

    public static T FromDict(Dictionary<string, Variant> dict)
    {
        return new T();
    }
}