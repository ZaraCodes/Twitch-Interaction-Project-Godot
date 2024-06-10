using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Node that contains everything needed for a track
/// </summary>
public partial class MarbleTrack : Node3D
{
    #region Fields
    /// <summary>Reference to the saved data related to twitch</summary>
    public TwitchGlobals TwitchGlobals { get; private set; }
    /// <summary>Array that contains all points on the track where marbles can spawn</summary>
    private List<Vector3> spawnPoints;
    /// <summary>The name of the map</summary>
    [Export] public string MapName { get; private set; }
    /// <summary>Node that is the parent of the spawnpoints</summary>
    [Export] private Node3D spawnPointParent;
    /// <summary>Parent object that will hold all marbles/// </summary>
    [Export] private Node3D marblesParent;
    /// <summary>The height where marbles below will delete themselves</summary>
    [Export] public float DeathHeight { get; private set; }
    /// <summary>Sets if marbles can spawn or not</summary>
    private bool allowMarblesSpawning;
    /// <summary>Sets how many players can join the map, depends on the number of spawnpoint objects</summary>
    private int maxPlayerCount;
    /// <summary>The current amount of active players in the map</summary>
    public int CurrentPlayerCount { get; private set; }
    /// <summary>Cache of the packed scene of a player marble</summary>
    private PackedScene packedMarbleScene;
    /// <summary>Reference to the TrackManager</summary>
    public TrackManager TrackManager;
    /// <summary>A Random Number Generator that is used to randomly select a spawn point for a new player</summary>
    private RandomNumberGenerator rng;
    /// <summary>The amount of players that have finished</summary>
    public int FinishedPlayers { get; private set; }
    /// <summary>The amount of players that didn't make it</summary>
    public int DeadPlayers { get; private set; }
    /// <summary>The amount of players that joined the race</summary>
    public int MaxJoinedPlayers { get; private set; }
    /// <summary>List of players that joined the race</summary>
    public List<string> JoinedPlayers { get; private set; }

    /// <summary>Signal that gets emitted when the track gets reset</summary>
    [Signal]
    public delegate void OnResetEventHandler();
    #endregion

    #region Methods
    /// <summary>
    /// Initializes the spawnpoints
    /// </summary>
    private void InitSpawnpoints()
    {
        spawnPoints = new();
        foreach (Node3D spawnPoint in spawnPointParent.GetChildren().Cast<Node3D>())
        {
            spawnPoints.Add(spawnPoint.GlobalPosition);
        }
        maxPlayerCount = spawnPoints.Count;
    }

    /// <summary>
    /// Initializes the title bar
    /// </summary>
    private void InitTitleBar()
    {
        TrackManager.TitleBar.PlayerNumbers.Text = $"{CurrentPlayerCount}/{maxPlayerCount}";
        TrackManager.TitleBar.MapName.Text = MapName;
        TrackManager.InitLevel(this);
    }

    /// <summary>
    /// Receives a message and joins the player who sent it the race
    /// </summary>
    /// <param name="message"></param>
    public void HandleJoinMessage(Dictionary message)
    {
        if (!allowMarblesSpawning) return;

        if (!message.TryGetValue("UserId", out var id)) return;
        if (JoinedPlayers.Contains((string)id)) return;

        if (message.TryGetValue("Text", out var value))
        {
            string text = (string)value;
            if (!text.StartsWith("!play")) return;
            if (spawnPoints.Count < 1) return;

            JoinedPlayers.Add((string)id);
            TwitchGlobals.AddPlayerData(message);
            SpawnMarble(message);
        }
    }

    /// <summary>
    /// Spawns a marble using data from the player's message
    /// </summary>
    /// <param name="message"></param>
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

    /// <summary>
    /// Spawns a marble with test data
    /// </summary>
    /// <param name="id"></param>
    /// <param name="DisplayName"></param>
    /// <param name="color"></param>
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

    /// <summary>
    /// Fills the marble track with test marbles
    /// </summary>
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

    /// <summary>
    /// Starts the race by unfreezing the marbles
    /// </summary>
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

    /// <summary>
    /// Removes a player that fell off the track
    /// </summary>
    /// <param name="id"></param>
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

    /// <summary>
    /// Checks if the game has ended yet
    /// </summary>
    private void HasGameEnded()
    {
        CurrentPlayerCount--;
        if (CurrentPlayerCount == 0)
        {
            //GD.Print(TrackManager.FinishedPlayers);
            TrackManager.RaceTime.Stop();
            TrackManager.ShowWinningPage();
        }
    }

    /// <summary>
    /// Lets a player finish
    /// </summary>
    /// <param name="id"></param>
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

    /// <summary>
    /// Resets the track
    /// </summary>
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
        JoinedPlayers.Clear();
        TrackManager.WinningPage.Hide();
        TrackManager.TitleBar.Reset();
        TrackManager.RaceTime.Reset();
        TrackManager.Camera.Reset();
        EmitSignal(SignalName.OnReset);
    }

    /// <summary>
    /// Deletes all marbles on the track
    /// </summary>
    private void DeleteMarbles()
    {
        foreach (var node in marblesParent.GetChildren())
        {
            node.Free();
        }
    }

    /// <summary>
    /// Colors a marble
    /// </summary>
    /// <param name="id"></param>
    /// <param name="color"></param>
    public void ColorMarble(string id, Color color)
    {
        PlayerMarble marble = null;
        foreach (var player in marblesParent.GetChildren())
        {
            if (player.Name == id)
            {
                marble = (PlayerMarble)player;
                break;
            }
        }
        if (marble == null) return;
        marble.SetCustomColor(color);
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
        JoinedPlayers = new();
    }
}
