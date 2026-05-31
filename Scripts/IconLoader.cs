using System.Collections.Generic;
using Godot;

namespace CardBase.Scripts;

public class IconLoader
{
    public static IconLoader Instance => instance ??= new IconLoader();
    private static IconLoader instance;
    private Dictionary<string, Texture2D> loadedImages = new Dictionary<string, Texture2D>();
    private Dictionary<string, SpriteFrames> loadedAnimations = new Dictionary<string, SpriteFrames>();
    private Dictionary<string, List<Texture2D>> loadedSingleAnimations = new Dictionary<string, List<Texture2D>>();


    public Texture2D LoadImage(string path)
    {
        if (!loadedImages.ContainsKey(path))
        {
            loadedImages.Add(path, GD.Load<Texture2D>(path));
        }

        return loadedImages[path];
    }

    public SpriteFrames LoadAnimation(string path)
    {
        if (!loadedAnimations.ContainsKey(path))
        {
            loadedAnimations.Add(path, ResourceLoader.Load<SpriteFrames>(path));
        }

        return (SpriteFrames)loadedAnimations[path].Duplicate();
    }

    public List<Texture2D> LoadSingleAnimation(string path, string filename, int count)
    {
        var id = $"{path}_{filename}_{count}";
        if (!loadedSingleAnimations.ContainsKey(id))
        {
            var list = new List<Texture2D>();
            for (var i = 0; i < count; i++)
            {
                list.Add(LoadImage($"{path}/{filename}_{i}.png"));
            }
            loadedSingleAnimations.Add(id, list);
        }

        return loadedSingleAnimations[id];
    }
}
