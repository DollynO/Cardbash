using System.Text;
using CardBase.Scripts.PlayerScripts;
using Godot;
using Godot.Collections;

namespace CardBase.Scripts;

public partial class VisualComponent : Node2D, IComponent
{
    public IEntityComponent Parent { get; private set; }
    public void SetParent(IEntityComponent component)
    {
        Parent = component;
    }

    public Vector2 TextureSize
    {
        get; 
        private set;
    }
    
    public Vector2 ScaledTextureSize => TextureSize * ((Node2D)Parent).Scale;
    private string preLoadedAnimation = string.Empty;
    private Vector2 preLoadedOffset = Vector2.Zero;
    private ShaderMaterial preLoadedShader = null;
    private string currentAnimationString = string.Empty;
    public void SetAnimation(string path, Vector2 offset)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        
        if (sprite != null)
        {
            assignAnimation(sprite, path, offset);
        }
        else
        {
            preLoadedAnimation = path;
            preLoadedOffset = offset;
        }
    }

    public void SetShader(string path, Dictionary<string, float> shaderParams)
    {
        
    }

    public void SetShader(ShaderMaterial shader)
    {
        if (shader == null)
        {
            return;
        }
        
        if (sprite != null)
        {
            sprite.Material = (ShaderMaterial)shader.Duplicate();
        }
        else
        {
            preLoadedShader = shader;
        }
    }
    
    private AnimatedSprite2D sprite;
    
    public override void _Ready()
    {
        sprite = new  AnimatedSprite2D();
        sprite.SetTextureFilter(TextureFilterEnum.Nearest);
        if (preLoadedAnimation != string.Empty)
        {
            assignAnimation(sprite, preLoadedAnimation, preLoadedOffset);
        }

        if (preLoadedShader != null)
        {
            sprite.Material = (ShaderMaterial)preLoadedShader.Duplicate();
        }
        this.AddChild(sprite);
        sprite.Play();
    }

    private void assignAnimation(AnimatedSprite2D sp, string path, Vector2 offset)
    {
        sp.SpriteFrames = IconLoader.Instance.LoadAnimation(path);
        var spriteFrames = sp.SpriteFrames;
        var texture = spriteFrames.GetFrameTexture(sp.Animation, sp.Frame);
        sp.Offset = offset;
        TextureSize = texture.GetSize();
    }
    
    public void UpdateAnimation(PlayerInput _input)
    {
        if (sprite == null)
        {
            return;
        }
        
        if (_input.XDirection == 0
            && _input.YDirection == 0)
        {
            sprite.Play(currentAnimationString.Replace("move", "idle"));
            return;
        }

        // Set the correct walking animation
        if (_input.XDirection > 0)
        {
            currentAnimationString = "move_side";
            sprite.FlipH = false;
        }
        else if (_input.XDirection < 0)
        {
            currentAnimationString = "move_side";
            sprite.FlipH = true;
        }
        else if (_input.YDirection > 0)
        {
            currentAnimationString = "move_down";
        }
        else if (_input.YDirection < 0)
        {
            currentAnimationString = "move_up";
        }

        sprite.Play(currentAnimationString);
    }
}