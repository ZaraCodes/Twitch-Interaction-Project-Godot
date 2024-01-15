using Godot;
using System;

public partial class MainMenu : Control
{
	#region Fields
	[Export] private Control mainMenu;
	[Export] private Control settingsMenu;
	#endregion
	/// <summary>Quits the game</summary>
	public void QuitGame()
	{
		GetTree().Quit();
	}

	public void OpenSettingsMenu()
	{
		settingsMenu.Visible = true;
		mainMenu.Visible = false;
	}

	public void ChangeScene()
	{
		GetTree().Root.AddChild(GD.Load<PackedScene>("res://Scenes/Game Scene.tscn").Instantiate());
		GetParent().GetParent().QueueFree();
	}

	public void GetPronouns()
	{
        var twitchGlobals = GetNode<TwitchGlobals>("/root/TwitchGlobals");
		twitchGlobals.GetPronouns();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{

	}
}
