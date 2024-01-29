using Godot;
using System;

public partial class SpectatorCam : Camera3D
{
	[Export]
	private float speed = 5f;
	[Export]
	private float maxSpeed = 50f;
	[Export]
	private float minDistanceToMarble = 1f;
	[Export]
	private Node3D RotationPoint;

	private Vector2 mouseMovement;

	private float rotationX;

	private float speedDiff;

	private Vector2 mouseCoords;
	private Vector2 mouseCoordsCache;

	private Node3D referencedMarble;

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

	public void SetReferenceMarble(Node3D marble)
	{
		referencedMarble = marble;
		Position = new Vector3(0f, 0f, (marble.GlobalPosition - GlobalPosition).Length());
		Rotation = new Vector3();
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
			referencedMarble = null;
			var globalPos = GlobalPosition;
			var globalRot = GlobalRotation;
			RotationPoint.Rotation = Vector3.Zero;
			RotationPoint.Position = Vector3.Zero;
			Rotation = globalRot;
			Position = globalPos;
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
