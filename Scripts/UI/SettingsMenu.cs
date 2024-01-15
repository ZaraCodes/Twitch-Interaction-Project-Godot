using Godot;
using System;

public partial class SettingsMenu : Control
{
	private string channelName;
	[Export]
	private Control mainMenu;
	[Export]
	private Control settingsMenu;
	[Export]
	private Label responseMessage;

	public void SaveConfig()
	{
		var config = new ConfigFile();

		// store some values
		config.SetValue("ConnectionSettings", "Channel", channelName);

		// save file (overwrite)
		config.Save("user://config.cfg");
	}

	public void SetChannelName(string channelName)
	{
		this.channelName = channelName;
	}

	public void ConnectToTwitch()
	{
        var twitchConnector = GetNode<TwitchConnection>("/root/TwitchConnection");

		twitchConnector.Authenticated += OnAuthenticated;
		twitchConnector.AuthenticationFailed += () => responseMessage.Text = "Authentication failed";
        twitchConnector.Authenticate();
    }

	public void OnAuthenticated()
	{
		responseMessage.Text = "Authentication successful";

    }



    public void GoBackToMainMenu()
	{
		RequestReady();

		SaveConfig();
		mainMenu.Show();
		settingsMenu.Hide();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		responseMessage.Text = " ";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
