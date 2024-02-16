using Godot;
using System;

/// <summary>
/// This type of message gets displayed if a player joins the race or falls of the track
/// </summary>
public partial class JoinMessage : Panel
{
	/// <summary>The time this message will be displayed before it gets removed again</summary>
	[Export]
	private float maxAge;

	/// <summary>The background of the message</summary>
	[Export]
	private Panel panel;

	/// <summary>The defined style of the panel</summary>
	[Export]
	private StyleBoxFlat style;

	/// <summary>Reference to the label that displays the message</summary>
	[Export]
	private RichTextLabel label;

	/// <summary>
	/// Sets up the message to display a join message
	/// </summary>
	/// <param name="name">The name of the player</param>
	/// <param name="color">The player's color</param>
	/// <param name="action">The action that is displayed after the player's name</param>
	public void SetSpawnMsg(string name, Color color, string action)
	{
		SetLabelText(name, color, action);
		SetBorderStyle(color);
	}

    /// <summary>
    /// Sets up the message to display a death message
    /// </summary>
    /// <param name="name">The name of the player</param>
    /// <param name="color">The player's color</param>
    /// <param name="action">The action that is displayed after the player's name</param>
    public void SetDeathMsg(string name, Color color, string action)
	{
		SetLabelText(name, color, action);
		SetBorderStyle(new Color("000000"));
	}

	/// <summary>
	/// Sets the border color of the background panel
	/// </summary>
	/// <param name="color">The new color of the background panel</param>
	private void SetBorderStyle(Color color)
	{
        var newStyle = (StyleBoxFlat)style.Duplicate();
        newStyle.BorderColor = color;
        panel.AddThemeStyleboxOverride("panel", newStyle);
	}

    /// <summary>
    /// Sets the label text
    /// </summary>
    /// <param name="name">The player's name</param>
    /// <param name="color">The player's color</param>
    /// <param name="action">The action that is displayed after the player's name </param>
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
}
