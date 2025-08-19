using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts;

public class IconLoader
{
    public static IconLoader Instance => instance ??= new IconLoader();
    private static IconLoader instance;
    private Dictionary<string, Texture2D> loadedImages = new Dictionary<string, Texture2D>();
    

    public Texture2D LoadImage(string path)
    {
        if (!loadedImages.ContainsKey(path))
        {
            loadedImages.Add(path, GD.Load<Texture2D>(path));
        }
        
        return (Texture2D)loadedImages[path].Duplicate();
    }
}