using Godot;
using Godot.Collections;
using System;

public partial class MainMenuChatDisplay : Control
{
	RandomNumberGenerator rng;

	public void AddMessageHandler()
	{
		var twitchConnection = GetNode<TwitchConnection>("/root/TwitchConnection");
		twitchConnection.OnChatMessage += ShowMessageOnScreen;
	}

	public void RemoveMessageHandler()
	{
		var twitchConnection = GetNode<TwitchConnection>("/root/TwitchConnection");
		twitchConnection.OnChatMessage -= ShowMessageOnScreen;
	}

	private void ShowMessageOnScreen(Dictionary chatMessage)
	{
		var node = GD.Load<PackedScene>("res://Scenes/MainMenuChatMessage.tscn").Instantiate();
		AddChild(node);
		MenuChatMessage newMessage = node as MenuChatMessage;
		newMessage.SetContent(chatMessage);
		newMessage.Position = new(rng.Randf() * Size.X, rng.Randf() * Size.Y);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		rng = new RandomNumberGenerator();
		AddMessageHandler();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _ExitTree()
	{
		RemoveMessageHandler();
		base._ExitTree();
	}
}
