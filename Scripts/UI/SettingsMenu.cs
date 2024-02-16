using Godot;
using System;

/// <summary>
/// Node that contains the settings menu
/// </summary>
public partial class SettingsMenu : Control
{
	/// <summary>Reference to the main menu node</summary>
	[Export]
	private Control mainMenu;
	/// <summary>Reference to the settings menu node</summary>
	[Export]
	private Control settingsMenu;
	/// <summary>Reference to the label that displays if the authentication was successful or not</summary>
	[Export]
	private Label responseMessage;

	/// <summary>
	/// Sets up the signal responses and connects the application to twitch
	/// </summary>
	public void ConnectToTwitch()
	{
        var twitchConnector = GetNode<TwitchConnection>("/root/TwitchConnection");

		twitchConnector.Authenticated += OnAuthenticated;
		twitchConnector.AuthenticationFailed += OnAuthenticationFailed;
        twitchConnector.Authenticate();
    }

	/// <summary>
	/// Displays a success message in the label
	/// </summary>
    private void OnAuthenticated()
	{
		responseMessage.Text = "Authentication successful";
    }

	/// <summary>
	/// Displays a failure message in the label
	/// </summary>
	private void OnAuthenticationFailed()
	{
        responseMessage.Text = "Authentication failed";
    }

	/// <summary>
	/// Returns back to the main menu
	/// </summary>
    public void GoBackToMainMenu()
	{
		RequestReady();

		mainMenu.Show();
		settingsMenu.Hide();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		responseMessage.Text = " ";
	}
}
