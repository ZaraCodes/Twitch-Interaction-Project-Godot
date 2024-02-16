using Godot;
using System;

/// <summary>
/// Script for controlling the camera
/// </summary>
public partial class SpectatorCam : Camera3D
{
	/// <summary>The speed at which the camera will fly</summary>
	[Export]
	private float speed = 5f;
	/// <summary>The max speed the spectator can set</summary>
	[Export]
	private float maxSpeed = 50f;
	/// <summary>The smallest distance to a marble if the camera is following a marble</summary>
	[Export]
	private float minDistanceToMarble = 1f;
	/// <summary>Reference to the node that the camera will rotate instead if it's following a marble</summary>
	[Export]
	private Node3D RotationPoint;

	/// <summary>/// The mouse movement on the screen</summary>
	private Vector2 mouseMovement;

	/// <summary>Tracks the rotation around the x axis</summary>
	private float rotationX;

	/// <summary>Speed change during a frame if the player uses the mouse wheel</summary>
	private float speedDiff;

	private Vector2 mouseCoords;
	private Vector2 mouseCoordsCache;

	/// <summary>Reference to the marble that the camera rotates around and follows</summary>
	private Node3D referencedMarble;

	/// <summary>The position the camera was at at the start of the game</summary>
	private Vector3 startPosition;

	/// <summary>The rotation the camera had at the start of the game</summary>
	private Vector3 startRotation;
	
	/// <summary>
	/// The ID of the player that the camera follows
	/// </summary>
	public string RefId { get; set; }

	/// <summary>
	/// Moves and rotates the camera
	/// </summary>
	/// <param name="delta">time since the last frame</param>
	private void Move(double delta)
	{
		HandleRotation(delta);
		HandleMovement(delta);

		if (speedDiff != 0f)
		{
			speed += speedDiff * (float)delta;
			if (speed > maxSpeed) speed = maxSpeed;
			else if (speed < 0) speed = 0;
			speedDiff = 0f;
		}
	}

    /// <summary>
    /// Handles the movement
    /// </summary>
    /// <param name="delta">time since the last frame</param>
    private void HandleMovement(double delta)
	{
		var direction = GetMovementVector();
		if (direction.Length() <= 0) return;

		if (referencedMarble == null)
			Position += (direction.X * Basis.X + direction.Z * Basis.Z + new Vector3(0, direction.Y, 0)) * speed * (float)delta;
		else
		{
			Position += direction.Z * Basis.Z * speed * (float)delta;
			RotationPoint.Rotate(Vector3.Up, direction.X * (float)delta);
		}
	}

    /// <summary>
    /// Handles the rotation
    /// </summary>
    /// <param name="delta">time since the last frame</param>
    private void HandleRotation(double delta)
	{
		if (Input.IsActionPressed("rotate_camera"))
		{
			rotationX -= mouseMovement.Y * (float)delta;

			if (rotationX < -Mathf.Pi / 2) rotationX = -Mathf.Pi / 2;
			else if (rotationX > Mathf.Pi / 2) rotationX = Mathf.Pi / 2;
			if (referencedMarble == null)
			{
				Rotate(Vector3.Up, -mouseMovement.X * (float)delta);
				Rotation = new Vector3(rotationX, Rotation.Y, Rotation.Z);
			}
			else
			{
				RotationPoint.Rotate(Vector3.Up, -mouseMovement.X * (float)delta);
				RotationPoint.Rotation = new Vector3(rotationX, RotationPoint.Rotation.Y, Rotation.Z);
			}
			mouseMovement = new Vector2();
		}
		if (Input.IsActionJustPressed("rotate_camera"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (Input.IsActionJustReleased("rotate_camera"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetViewport().WarpMouse(mouseCoords);
		}
	}

	/// <summary>Sets a reference marble</summary>
	/// <param name="marble"></param>
	/// <param name="id"></param>
	public void SetReferenceMarble(Node3D marble, string id)
	{
		referencedMarble = marble;
		Position = new Vector3(0f, 0f, (marble.GlobalPosition - GlobalPosition).Length());
		Rotation = new Vector3();
		RefId = id;
	}

	/// <summary>Returns a normalized movement vector</summary>
	/// <returns></returns>
	private Vector3 GetMovementVector()
	{
		Vector3 direction = new();
		if (Input.IsActionPressed("move_left")) direction.X = -1;
		if (Input.IsActionPressed("move_right")) direction.X = 1;
		if (Input.IsActionPressed("move_up")) direction.Y = 1;
		if (Input.IsActionPressed("move_down")) direction.Y = -1;
		if (Input.IsActionPressed("move_forward")) direction.Z = -1;
		if (Input.IsActionPressed("move_back")) direction.Z = 1;
		return direction.Normalized();
	}

	/// <summary>
	/// Resets the marble to follow to null so the camera will move normally again
	/// </summary>
	public void UnfollowCam()
	{
        referencedMarble = null;
        var globalPos = GlobalPosition;
        var globalRot = GlobalRotation;
        RotationPoint.Rotation = Vector3.Zero;
        RotationPoint.Position = Vector3.Zero;
        Rotation = globalRot;
        Position = globalPos;
		RefId = string.Empty;
    }

	/// <summary>
	/// Resets the camera
	/// </summary>
	public void Reset()
	{
		if (referencedMarble != null) UnfollowCam();
		Position = startPosition;
		Rotation = startRotation;
	}

	/// <summary>
	/// Processes mouse input
	/// </summary>
	/// <param name="event"></param>
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motionEvent)
		{
			if (Input.MouseMode == Input.MouseModeEnum.Visible) mouseCoords = motionEvent.Position;
			mouseMovement = motionEvent.Relative * 0.33f;
		}
		if (@event is InputEventMouseButton buttonEvent)
		{
			if (buttonEvent.ButtonIndex == MouseButton.WheelUp)
			{
				speedDiff = buttonEvent.Factor * 75f;
			}
			else if (buttonEvent.ButtonIndex == MouseButton.WheelDown)
			{
				speedDiff = -buttonEvent.Factor * 75f;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Move(delta);
		if (Input.IsActionJustPressed("unfollow_cam"))
		{
			UnfollowCam();
		}
		if (referencedMarble != null)
		{
			if (Position.Z < minDistanceToMarble)
			{
				Position = new(0, 0, minDistanceToMarble);
			}
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		rotationX = Rotation.X;
		startPosition = Position;
		startRotation = Rotation;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (referencedMarble != null)
		{
			RotationPoint.GlobalPosition = referencedMarble.GlobalPosition;
		}
	}
}
