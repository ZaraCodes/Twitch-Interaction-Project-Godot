using Godot;
using System;

public partial class TrackManager : Node3D
{
	#region
	public MarbleTrack MarbleTrack { get; private set; }

	[Export] public TitleBar TitleBar { get; private set; }

	[Export] public SpectatorCam Camera { get; private set; }
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
		TitleBar.DisableStartButton();
		MarbleTrack.StartGame();
	}

	public void CreateSpawnMessage(string name, string color)
	{
		TitleBar.CreateSpawnMessage(name, color);
	}

	#endregion
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
