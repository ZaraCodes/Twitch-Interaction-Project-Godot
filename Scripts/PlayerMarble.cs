using Godot;
using System;

/// <summary>
/// Node for use as a player marble
/// </summary>
public partial class PlayerMarble : Node3D
{
	#region Fields
	/// <summary>The player's user id</summary>
	public string ID;
	/// <summary>Reference to the marble track</summary>
	private MarbleTrack marbleTrack;
	/// <summary>Reference to the box container that contains the name and the badges</summary>
	[Export] private BoxContainer boxContainer;
	/// <summary>Reference to the mesh instance of the marble</summary>
	[Export] private MeshInstance3D meshInstance;
	/// <summary>Reference to the label that displays the name</summary>
	[Export] private RichTextLabel label;
	/// <summary>Reference to the player's rigidbody</summary>
	[Export] private RigidBody3D rigidBody;
	/// <summary>List of texture recs for displaying the marbles</summary>
	[Export] private TextureRect[] badges;
	/// <summary>Offset for name display</summary>
	[Export] private Vector2 offset;

	/// <summary>The player's name</summary>
	private string playerName;
	/// <summary>The player's color</summary>
	private string color;

	/// <summary>Signal that gets emitted when a player marble gets destructed/// </summary>
	/// <param name="id"></param>
	[Signal]
	public delegate void OnMarbleDestructionEventHandler(string id);
	#endregion

	#region Methods
	/// <summary>
	/// Initializes the marble
	/// </summary>
	/// <param name="id">The player's id</param>
	/// <param name="badges">The player's badges</param>
	/// <param name="marbleTrack">Reference to the marble track</param>
	public async void InitMarble(string id, string badges, MarbleTrack marbleTrack)
	{
		this.marbleTrack = marbleTrack;
        OnMarbleDestruction += marbleTrack.RemoveOnePlayer;

        ID = id;
		Name = id;
		label.Text = string.Empty;
		var twitchGlobals = marbleTrack.TwitchGlobals;
		var user = twitchGlobals.FindUserById(id);
		if (user == null) return;

		if (user.TryGetValue("Color", out var Color))
		{
			label.AppendText($"[color={(string)Color}]");
			color = (string)Color;
		}

		if (user.TryGetValue("DisplayName", out var DisplayName))
		{
			label.AppendText((string)DisplayName);
			playerName = (string)DisplayName;
		}
		else
			label.AppendText("<ERROR>");

		var numberOfBadges = 0;
		foreach (string badge in badges.Split(','))
		{
			if (badge == string.Empty) continue;
			GD.Print($"[InitMarble] Marble {Name}: Getting badge {badge}");

			if (!twitchGlobals.TryGetBadge(badge, out ImageTexture imageTexture))
			{
				twitchGlobals.DownloadBadge(badge);
				await ToSignal(twitchGlobals, TwitchGlobals.SignalName.BadgeDownloaded);
				if (!twitchGlobals.TryGetBadge(badge, out imageTexture))
				{
					GD.Print($"[InitMarble] Marble {Name}: Badge {badge} not available");
					continue;
				}
			}
			if (imageTexture != null && numberOfBadges < badges.Length)
			{
				GD.Print($"[InitMarble] Marble {Name}: Applying {badge}");
				this.badges[numberOfBadges].Texture = imageTexture;
				this.badges[numberOfBadges].Visible = true;
				numberOfBadges++;
			}
		}
	}

	/// <summary>
	/// Sets a custom marble color by applying a new material
	/// </summary>
	/// <param name="color">The color of the new material</param>
	public void SetCustomColor(Color color)
	{
		StandardMaterial3D material = new();
		material.AlbedoColor = color;
		meshInstance.MaterialOverride = material;
	}

	/// <summary>Makes the camera rotate around this marble</summary>
	/// <param name="event"></param>
	public void SetViewTarget(InputEvent @event)
	{
		if (@event is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.Left && button.IsPressed())
			{
				var cam = marbleTrack.TrackManager.Camera;
				cam.SetReferenceMarble(rigidBody, ID);
			}
		}
	}

	/// <summary>
	/// Checks if the marble is below the death level
	/// </summary>
	/// <returns></returns>
	private bool IsBelowDeathLevel()
	{
		return marbleTrack.DeathHeight > rigidBody.GlobalPosition.Y;
	}

	/// <summary>
	/// Removes the marble if it's below the death plane
	/// </summary>
	private void CheckDeathPlane()
	{
		if (IsBelowDeathLevel())
		{
			marbleTrack.TrackManager.TitleBar.CreateFallMessage(playerName, color);
			if (marbleTrack.TrackManager.Camera.RefId == ID) marbleTrack.TrackManager.Camera.UnfollowCam();

			EmitSignal(SignalName.OnMarbleDestruction, ID);
			Free();
		}
	}

	/// <summary>
	/// Sets the freeze property of the rigidbody
	/// </summary>
	/// <param name="freeze"></param>
	public void SetFreeze(bool freeze)
	{
		rigidBody.Freeze = freeze;
	}

	/// <summary>
	/// Updates the marble's name display position
	/// </summary>
	private void UpdateLabelPosition()
	{
		boxContainer.GlobalPosition = GetViewport().GetCamera3D().UnprojectPosition(rigidBody.GlobalPosition) - new Vector2(boxContainer.Size.X, 2 * boxContainer.Size.Y) / 2f;
		boxContainer.Visible = !GetViewport().GetCamera3D().IsPositionBehind(rigidBody.GlobalPosition);
	}
	#endregion

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateLabelPosition();
		CheckDeathPlane();
	}
}
