using Godot;
using Godot.Collections;
using System;

public partial class MenuChatMessage : Control
{
	[Export]
	private Label messageLabel;
	[Export]
	private RichTextLabel richMessageLabel;

	public void SetContent(Dictionary chatMessage)
	{
		messageLabel.Text = $"{chatMessage["DisplayName"]}: {chatMessage["Text"]}";
		//richMessageLabel.Text = $"[color={chatMessage.Color}]{chatMessage.DisplayName}[/color]: {chatMessage.Text}";
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Name = "Chat Message";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
