using Godot;
using System;

public partial class JoinMessage : Panel
{
	[Export]
	private float maxAge;

	[Export]
	private Panel panel;

	[Export]
	private StyleBoxFlat style;

	[Export]
	private RichTextLabel label;

	public void Init(string name, Color color)
	{
		var newStyle = (StyleBoxFlat)style.Duplicate();
		newStyle.BorderColor = color;
		label.Text = label.Text.Replace("REPLACE", color.ToHtml());
		label.Text = label.Text.Replace("PlayerName", name);

		panel.AddThemeStyleboxOverride("panel", newStyle);
		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var timer = new Timer();
		AddChild(timer);

		timer.Start(maxAge);
		timer.Timeout += QueueFree;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
