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

	private bool allowMarblesSpawning;

	private int maxPlayerCount;

	private int playerCount;

	private PackedScene packedMarbleScene;

	public TrackManager TrackManager;

	private RandomNumberGenerator rng;
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
		TrackManager.TitleBar.PlayerNumbers.Text = $"{playerCount}/{maxPlayerCount}";
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
		TrackManager.TitleBar.PlayerNumbers.Text = $"{++playerCount}/{maxPlayerCount}";

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
		TrackManager.TitleBar.PlayerNumbers.Text = $"{++playerCount}/{maxPlayerCount}";
		newMarble.Position = spawnPoint;
		newMarble.InitMarble(id, "", this);
	}

	public void TestTrack()
	{
		for (int i = 0; i < maxPlayerCount; i++)
		{
			var color = new Color(rng.Randf(), rng.Randf(), rng.Randf());
			var name = $"Test Marble {i}";
			SpawnMarble(i.ToString(), name, color.ToHtml());
		}
	}

	public void StartGame()
	{
		allowMarblesSpawning = false;

		foreach (PlayerMarble marble in marblesParent.GetChildren())
		{
			marble.SetFreeze(false);
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
