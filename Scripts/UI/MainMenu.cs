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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
