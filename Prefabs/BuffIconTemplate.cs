using Godot;

public partial class BuffIconTemplate : Node2D
{
	[Export] public float RemainingDurationPercentage;
	private MultiplayerSynchronizer cooldownSync;
	private ShaderMaterial timerShaderMat;
	private Shader timerShader = GD.Load<Shader>("res://Shaders/CircleTimerShader.gdshader");

	private Sprite2D Icon;

	public override void _EnterTree()
	{
		initIcon();
		cooldownSync = new MultiplayerSynchronizer();
		cooldownSync.Name = "cooldownSync";
		cooldownSync.SetMultiplayerAuthority(1);
		cooldownSync.RootPath = new NodePath(".");
		var cfg = new SceneReplicationConfig();
		cfg.AddProperty(new NodePath($"{GetPath()}:{nameof(RemainingDurationPercentage)}"));
		cooldownSync.ReplicationConfig = cfg;
		AddChild(cooldownSync, true);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timerShaderMat.SetShaderParameter("fill_amount", RemainingDurationPercentage);
	}

	public void UpdateTimer(double current, double total)
	{
		RemainingDurationPercentage = (float)(current / total);
	}
	
	public void SetBuff(Texture2D texture)
	{
		initIcon();
		Icon.Texture = texture;
		var size = Icon.Texture.GetSize();
		Icon.Scale = new Vector2(20 / size.X, 20 / size.Y);
	}

	private void initIcon()
	{
		if (Icon == null)
		{
			timerShaderMat = new ShaderMaterial();
			timerShaderMat.Shader = timerShader;
			Icon = new Sprite2D();
			Icon.Material = timerShaderMat;
			AddChild(Icon);
		}
	}
}
