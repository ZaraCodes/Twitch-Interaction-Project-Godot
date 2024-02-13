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

	public void SetSpawnMsg(string name, Color color, string action)
	{
		SetLabelText(name, color, action);
		SetBorderStyle(color);
	}

	public void SetDeathMsg(string name, Color color, string action)
	{
		SetLabelText(name, color, action);
		SetBorderStyle(new Color("000000"));
	}
	private void SetBorderStyle(Color color)
	{
        var newStyle = (StyleBoxFlat)style.Duplicate();
        newStyle.BorderColor = color;
        panel.AddThemeStyleboxOverride("panel", newStyle);
	}

	private void SetLabelText(string name, Color color, string action)
	{
        label.Text = label.Text.Replace("REPLACE", color.ToHtml());
        label.Text = label.Text.Replace("PlayerName", name);
        label.Text = label.Text.Replace("ACTION", action);
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
