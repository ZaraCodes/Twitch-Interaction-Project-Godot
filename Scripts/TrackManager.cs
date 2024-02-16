using Godot;
using Godot.Collections;
using System;

public partial class TrackManager : Node3D
{
	#region Fields
	public MarbleTrack MarbleTrack { get; private set; }

	[Export] public TitleBar TitleBar { get; private set; }

	[Export] public SpectatorCam Camera { get; private set; }

	[Export] private PlayerCard playerCard;

	[Export] public RaceTime RaceTime { get; private set; }

	[Export] public WinningPage WinningPage { get; private set; }

	public Dictionary FinishedPlayers { get; private set; }
	#endregion

	#region Methods
	public void InitLevel(MarbleTrack marbleTrack)
	{
		MarbleTrack = marbleTrack;
	}

	public void TestTrack()
	{
		MarbleTrack.TestTrack();
	}

	/// <summary>
	/// Stops the ability for players to join and starts physics simulations for all the marbles
	/// </summary>
	public void StartGame()
	{
		MarbleTrack.StartGame();
	}

	public void CreateSpawnMessage(string name, string color)
	{
		TitleBar.CreateSpawnMessage(name, color);
	}

	public void ShowCard(string userId)
	{
		playerCard.ShowCard(userId);
	}

	public void ShowWinningPage()
	{
		WinningPage.FillLeaderboard(FinishedPlayers, MarbleTrack.TwitchGlobals);
		WinningPage.Visible = true;
	}

	public void Reset()
	{
		MarbleTrack.Reset();
	}

	public void OnCountdownStarted()
	{
		MarbleTrack.TwitchGlobals.GetPronouns();
	}

	public void OnTwitchEvent(Dictionary eventData)
	{
		if (eventData.TryGetValue("reward", out var reward))
		{
			var rewardDict = (Dictionary)Json.ParseString((string)reward);
			if (rewardDict.TryGetValue("title", out var title))
			{
				if ((string)title == "Color your marble!")
				{
					var userId = (string)eventData["user_id"];
					var color = new Color((string)MarbleTrack.TwitchGlobals.FindUserById(userId)["Color"]);
					MarbleTrack.ColorMarble(userId, color);
				}
			}
        }
	}

	#endregion
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		FinishedPlayers = new Dictionary();
		var twitchConnector = GetNode<TwitchConnection>("/root/TwitchConnection");
		twitchConnector.Event += OnTwitchEvent;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
