using Godot;
using System;

/// <summary>
/// Methods for the main menu
/// </summary>
public partial class MainMenu : Control
{
	#region Fields
	/// <summary>Reference to the control node that contains the main menu</summary>
	[Export] private Control mainMenu;
	/// <summary>Reference to the control node that contains the settings menu</summary>
	[Export] private Control settingsMenu;
	#endregion
	/// <summary>
	/// Quits the game
	/// </summary>
	public void QuitGame()
	{
		GetTree().Quit();
	}
	
	/// <summary>
	/// Opens the settings menu
	/// </summary>
	public void OpenSettingsMenu()
	{
		settingsMenu.Visible = true;
		mainMenu.Visible = false;
	}

	/// <summary>
	/// Switches to the game scene
	/// </summary>
	public void ChangeScene()
	{
		GetTree().Root.AddChild(GD.Load<PackedScene>("res://Scenes/Game Scene.tscn").Instantiate());
		GetParent().GetParent().QueueFree();
	}
}
