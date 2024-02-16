using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Manages the connection between the track and other parts of the ui
/// </summary>
public partial class TrackManager : Node3D
{
	#region Fields
	/// <summary>Reference to the marble track</summary>
	public MarbleTrack MarbleTrack { get; private set; }
	/// <summary>Reference to the title bar</summary>
	[Export] public TitleBar TitleBar { get; private set; }
	/// <summary>Reference to the camera</summary>
	[Export] public SpectatorCam Camera { get; private set; }
	/// <summary>Reference to the unused player card</summary>
	[Export] private PlayerCard playerCard;
	/// <summary>Reference to the race time</summary>
	[Export] public RaceTime RaceTime { get; private set; }
	/// <summary>Reference to the winning page</summary>
	[Export] public WinningPage WinningPage { get; private set; }
	/// <summary>Dictionary that contains finished and fallen players</summary>
	public Dictionary FinishedPlayers { get; private set; }
	#endregion

	#region Methods
	/// <summary>
	/// Initializes the level by assigning the marble track
	/// </summary>
	/// <param name="marbleTrack"></param>
	public void InitLevel(MarbleTrack marbleTrack)
	{
		MarbleTrack = marbleTrack;
	}

	/// <summary>
	/// Tests the marble track
	/// </summary>
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

	/// <summary>
	/// Creates a spawn message
	/// </summary>
	/// <param name="name">The player's name</param>
	/// <param name="color">The player's color</param>
	public void CreateSpawnMessage(string name, string color)
	{
		TitleBar.CreateSpawnMessage(name, color);
	}

	/// <summary>
	/// Shows the player card (is never called)
	/// </summary>
	/// <param name="userId"></param>
	public void ShowCard(string userId)
	{
		playerCard.ShowCard(userId);
	}

	/// <summary>
	/// Shows the winning page
	/// </summary>
	public void ShowWinningPage()
	{
		WinningPage.FillLeaderboard(FinishedPlayers, MarbleTrack.TwitchGlobals);
		WinningPage.Visible = true;
	}

	/// <summary>
	/// Resets the track
	/// </summary>
	public void Reset()
	{
		MarbleTrack.Reset();
	}

	/// <summary>
	/// Tries to get the pronouns once the countdown has started
	/// </summary>
	public void OnCountdownStarted()
	{
		MarbleTrack.TwitchGlobals.GetPronouns();
	}

    /// <summary>
    /// Reacts to a received twitch event and to a specific channel point redemption called "Color your marble!" (sorry it's hardcoded)
    /// </summary>
    /// <param name="eventData"></param>
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
}
