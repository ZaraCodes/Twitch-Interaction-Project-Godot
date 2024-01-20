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

    public Camera3D camera { get; private set; }
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
        TrackManager trackManager = GetParent<TrackManager>();
        trackManager.TitleBar.PlayerNumbers.Text = $"{playerCount}/{maxPlayerCount}";
        trackManager.TitleBar.MapName.Text = MapName;
        trackManager.InitLevel(this);
    }

    public void HandleJoinMessage(Dictionary message)
    {
        if (!allowMarblesSpawning) return;

        if (message.TryGetValue("Text", out var value))
        {
            string text = (string)value;
            if (!text.StartsWith("!play")) return;

            TwitchGlobals.AddPlayerData(message);
            SpawnMarble((string)message["UserId"], (string)message["Badges"]);
        }
    }

    public void SpawnMarble(string id, string badges)
    {
        PlayerMarble newMarble = (PlayerMarble)packedMarbleScene.Instantiate();
        marblesParent.AddChild(newMarble);

        int index = GD.RandRange(0, spawnPoints.Count - 1);
        Vector3 spawnPoint = spawnPoints[index];
        spawnPoints.Remove(spawnPoint);

        newMarble.Position = spawnPoint;
        newMarble.InitMarble(id, badges, this);
    }

    public void SpawnMarble(string id, string DisplayName, string color)
    {
        TwitchGlobals.AddPlayerData(id, DisplayName, color);
    }
    #endregion

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        TwitchGlobals = GetNode<TwitchGlobals>("/root/TwitchGlobals");
        camera = GetParent().GetParent().GetChild<Camera3D>(0);
        var twitchConnection = GetNode<TwitchConnection>("/root/TwitchConnection");

        twitchConnection.OnChatMessage += HandleJoinMessage;
        InitSpawnpoints();
        InitTitleBar();
        packedMarbleScene = GD.Load<PackedScene>("res://Scenes/PlayerMarble.tscn");
        allowMarblesSpawning = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
