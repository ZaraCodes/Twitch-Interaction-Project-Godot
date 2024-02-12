using Godot;
using System;
public partial class PlayerMarble : Node3D
{
	#region Fields
	public string ID;
	private MarbleTrack marbleTrack;
	[Export] private BoxContainer boxContainer;
	[Export] private RichTextLabel label;
	[Export] private RigidBody3D rigidBody;
	[Export] private TextureRect[] badges;
	[Export] private Vector2 offset;
	private string playerName;
	private string color;
	#endregion

	#region Methods
	public async void InitMarble(string id, string badges, MarbleTrack marbleTrack)
	{
		this.marbleTrack = marbleTrack;
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
			//if (badge.Contains("subscriber")) continue;
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

	public void SetViewTarget(InputEvent @event)
	{
		if (@event is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.Left && button.IsPressed())
			{
				var cam = marbleTrack.TrackManager.Camera;
				cam.SetReferenceMarble(rigidBody, ID);
                // marbleTrack.TrackManager.ShowCard(ID);
            }
		}
	}

	private bool IsBelowDeathLevel()
	{
		return marbleTrack.DeathHeight > rigidBody.GlobalPosition.Y;
	}

	private void CheckDeathPlane()
	{
		if (IsBelowDeathLevel())
		{
			marbleTrack.TrackManager.TitleBar.CreateFallMessage(playerName, color);
			if (marbleTrack.TrackManager.Camera.RefId == ID) marbleTrack.TrackManager.Camera.UnfollowCam();

            Free();
		}
	}

	public void SetFreeze(bool freeze)
	{
		rigidBody.Freeze = freeze;
	}

	private void UpdateLabelPosition()
	{
		boxContainer.GlobalPosition = GetViewport().GetCamera3D().UnprojectPosition(rigidBody.GlobalPosition) - new Vector2(boxContainer.Size.X, 2 * boxContainer.Size.Y) / 2f;
		boxContainer.Visible = !GetViewport().GetCamera3D().IsPositionBehind(rigidBody.GlobalPosition);
	}
	#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateLabelPosition();
		CheckDeathPlane();
	}
}
