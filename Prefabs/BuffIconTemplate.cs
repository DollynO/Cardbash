using Godot;

public partial class BuffIconTemplate : Node2D
{
	[Export] public float RemainingDurationPercentage;
	[Export] public int CurrentStack;
	private int oldStacks = 0;
	private MultiplayerSynchronizer cooldownSync;
	private ShaderMaterial timerShaderMat;
	private Shader timerShader = GD.Load<Shader>("res://Shaders/CircleTimerShader.gdshader");

	private Sprite2D Icon;
	private Label StackCountLabel;

	public override void _EnterTree()
	{
		initIcon();
		initStackLabel();
		cooldownSync = new MultiplayerSynchronizer();
		cooldownSync.Name = "cooldownSync";
		cooldownSync.SetMultiplayerAuthority(1);
		cooldownSync.RootPath = new NodePath(".");
		var cfg = new SceneReplicationConfig();
		cfg.AddProperty(new NodePath($"{GetPath()}:{nameof(RemainingDurationPercentage)}"));
		cfg.AddProperty(new NodePath($"{GetPath()}:{nameof(CurrentStack)}"));
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
		
		if (oldStacks != this.CurrentStack)
		{
			oldStacks = this.CurrentStack;
			StackCountLabel.Text = this.CurrentStack.ToString();
		}
	}

	public void UpdateTimer(double current, double total)
	{
		RemainingDurationPercentage = (float)(current / total);
	}

	public void UpdateStacks(int stacks)
	{
		this.CurrentStack = stacks;
	}
	
	public void SetBuff(Texture2D texture)
	{
		initIcon();
		Icon.Texture = texture;
		var size = Icon.Texture.GetSize();
		Icon.Scale = new Vector2(15 / size.X, 15 / size.Y);
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

	private void initStackLabel()
	{
		if (StackCountLabel == null)
		{
			StackCountLabel = new Label();
			AddChild(StackCountLabel);
		}
	}
}
