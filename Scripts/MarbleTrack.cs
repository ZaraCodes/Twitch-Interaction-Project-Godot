using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MarbleTrack : Node3D
{
	#region Fields
	public TwitchGlobals TwitchGlobals { get; private set; }
	/// <summary>Array that contains all points on the track where marbles can spawn</summary>
	private List<Vector3> spawnPoints;

	[Export] public string MapName { get; private set; }
	/// <summary>Node that is the parent of the spawnpoints</summary>
	[Export] private Node3D spawnPointParent;

	[Export] private Node3D marblesParent;

	[Export] public float DeathHeight { get; private set; }

	private bool allowMarblesSpawning;

	private int maxPlayerCount;

	public int CurrentPlayerCount { get; private set; }

	private PackedScene packedMarbleScene;

	public TrackManager TrackManager;

	private RandomNumberGenerator rng;

	public int FinishedPlayers { get; private set; }

	public int DeadPlayers { get; private set; }

	public int MaxJoinedPlayers { get; private set; }

	[Signal]
	public delegate void OnResetEventHandler();
	#endregion

	#region Methods
	private void InitSpawnpoints()
	{
		spawnPoints = new();
		foreach (Node3D spawnPoint in spawnPointParent.GetChildren().Cast<Node3D>())
		{
			spawnPoints.Add(spawnPoint.GlobalPosition);
		}
		maxPlayerCount = spawnPoints.Count;
	}

	private void InitTitleBar()
	{
		TrackManager.TitleBar.PlayerNumbers.Text = $"{CurrentPlayerCount}/{maxPlayerCount}";
		TrackManager.TitleBar.MapName.Text = MapName;
		TrackManager.InitLevel(this);
	}

	public void HandleJoinMessage(Dictionary message)
	{
		if (!allowMarblesSpawning) return;

		if (message.TryGetValue("Text", out var value))
		{
			string text = (string)value;
			if (!text.StartsWith("!play")) return;
			if (spawnPoints.Count < 1) return;

			TwitchGlobals.AddPlayerData(message);
			SpawnMarble(message);
		}
	}

	public void SpawnMarble(Dictionary message)
	{
		PlayerMarble newMarble = (PlayerMarble)packedMarbleScene.Instantiate();
		marblesParent.AddChild(newMarble);

		int index = GD.RandRange(0, spawnPoints.Count - 1);
		Vector3 spawnPoint = spawnPoints[index];
		spawnPoints.Remove(spawnPoint);
		TrackManager.TitleBar.PlayerNumbers.Text = $"{++CurrentPlayerCount}/{maxPlayerCount}";

		newMarble.Position = spawnPoint;
		newMarble.InitMarble((string)message["UserId"], (string)message["Badges"], this);

		TrackManager.CreateSpawnMessage((string)message["DisplayName"], (string)message["Color"]);
	}

	public void SpawnMarble(string id, string DisplayName, string color)
	{
		TwitchGlobals.AddPlayerData(id, DisplayName, color);
		PlayerMarble newMarble = (PlayerMarble)packedMarbleScene.Instantiate();
		marblesParent.AddChild(newMarble);

		int index = rng.RandiRange(0, spawnPoints.Count - 1);
		Vector3 spawnPoint = spawnPoints[index];
		spawnPoints.Remove(spawnPoint);
		TrackManager.TitleBar.PlayerNumbers.Text = $"{++CurrentPlayerCount}/{maxPlayerCount}";
		newMarble.Position = spawnPoint;
		newMarble.InitMarble(id, "", this);
	}

	public void TestTrack()
	{
		if (!allowMarblesSpawning) return;
		var maxMarbles = spawnPoints.Count;
		for (int i = 0; i < maxMarbles; i++)
		{
			var color = new Color(rng.Randf(), rng.Randf(), rng.Randf());
			var name = $"Test Marble {i}";
			SpawnMarble(i.ToString(), name, color.ToHtml());
		}
	}

	public void StartGame()
	{
		DeadPlayers = 0;
		MaxJoinedPlayers = CurrentPlayerCount;

		allowMarblesSpawning = false;
		TrackManager.TitleBar.SetPromptToRace();
		foreach (PlayerMarble marble in marblesParent.GetChildren().Cast<PlayerMarble>())
		{
			marble.SetFreeze(false);
		}
	}

	public void RemoveOnePlayer(string id)
	{
        var player = new Dictionary
        {
            { "id", id },
            { "time", -1d }
        };
		if (!TrackManager.FinishedPlayers.ContainsKey(MaxJoinedPlayers - DeadPlayers))
			TrackManager.FinishedPlayers.Add(MaxJoinedPlayers - DeadPlayers, player);
		else GD.Print($"YEK {MaxJoinedPlayers - DeadPlayers}");
		DeadPlayers++;

		HasGameEnded();
    }

	private void HasGameEnded()
	{
        CurrentPlayerCount--;
		GD.Print(CurrentPlayerCount);
        if (CurrentPlayerCount == 0)
        {
            GD.Print(TrackManager.FinishedPlayers);
            TrackManager.RaceTime.Stop();
            TrackManager.ShowWinningPage();
        }
    }

    public void FinishPlayer(string id)
	{
		FinishedPlayers++;
		var time = TrackManager.RaceTime.Time;
        var player = new Dictionary
        {
            { "id", id },
            { "time", time }
        };

        if (!TrackManager.FinishedPlayers.ContainsKey(FinishedPlayers))
            TrackManager.FinishedPlayers.Add(FinishedPlayers, player);

		HasGameEnded();
    }

	public void Reset()
	{
		TrackManager.FinishedPlayers.Clear();
		CurrentPlayerCount = 0;
		FinishedPlayers = 0;
		DeadPlayers = 0;
		MaxJoinedPlayers = 0;
        TrackManager.TitleBar.PlayerNumbers.Text = $"{CurrentPlayerCount}/{maxPlayerCount}";
        allowMarblesSpawning = true;
		InitSpawnpoints();
		DeleteMarbles();
		TrackManager.WinningPage.Hide();
		TrackManager.TitleBar.Reset();
		TrackManager.RaceTime.Reset();
		TrackManager.Camera.ResetPosition();
		EmitSignal(SignalName.OnReset);
	}

	private void DeleteMarbles()
	{
		foreach (var node in marblesParent.GetChildren())
		{
			node.Free();
		}
	}
	#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TrackManager = GetParent<TrackManager>();
		TwitchGlobals = GetNode<TwitchGlobals>("/root/TwitchGlobals");
		var twitchConnection = GetNode<TwitchConnection>("/root/TwitchConnection");

		twitchConnection.OnChatMessage += HandleJoinMessage;
		InitSpawnpoints();
		InitTitleBar();
		packedMarbleScene = GD.Load<PackedScene>("res://Scenes/PlayerMarble.tscn");
		allowMarblesSpawning = true;
		rng = new();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
