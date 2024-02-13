using Godot;
using System;

public partial class Countdown : Label
{
	[Export] private Timer timer;

	[Export] private int startNumber;

	[Signal]
	public delegate void OnCountdownFinishedEventHandler();
	private int currentNumber;

	public void StartCountdown()
	{
		currentNumber = startNumber;
		SetLabel(currentNumber.ToString());
		Visible = true;
        timer.OneShot = false;
        timer.Timeout += SetState;
		timer.Start(1d);
	}
	private void SetLabel(string text)
	{
		Text = text;
	}

	private void SetState()
	{
		if (currentNumber > 1)
		{
			currentNumber--;
			SetLabel(currentNumber.ToString());
		}
		else
		{
			EmitSignal(SignalName.OnCountdownFinished);
			SetLabel("Go!");
			timer.Timeout -= SetState;
			timer.Timeout += HideLabel;
			timer.OneShot = true;
			timer.Start(2d);
		}
	}

	private void HideLabel()
	{
		timer.Timeout -= HideLabel;
		Visible = false;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
