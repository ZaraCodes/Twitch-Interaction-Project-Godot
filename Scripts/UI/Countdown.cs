using Godot;
using System;

/// <summary>
/// This countdown is used before the marble race starts
/// </summary>
public partial class Countdown : Label
{
	/// <summary>The timer used between each step</summary>
	[Export] private Timer timer;

	/// <summary>The number at which the timer starts</summary>
	[Export] private int startNumber;

	[Signal]
	public delegate void OnCountdownFinishedEventHandler();
	/// <summary>The number the countdown is currently at</summary>
	private int currentNumber;

	[Signal]
	public delegate void OnCountdownStartedEventHandler();

	/// <summary>
	/// Starts the countdown
	/// </summary>
	public void StartCountdown()
	{
		currentNumber = startNumber;
		SetLabel(currentNumber.ToString());
		Visible = true;
        timer.OneShot = false;
        timer.Timeout += SetState;
		timer.Start(1d);
		EmitSignal(SignalName.OnCountdownStarted);
	}

	/// <summary>
	/// Sets the label text to the current countdown number
	/// </summary>
	/// <param name="text"></param>
	private void SetLabel(string text)
	{
		Text = text;
	}

	/// <summary>
	/// Sets the current number and the label state
	/// </summary>
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

	/// <summary>Hides the label</summary>
	private void HideLabel()
	{
		timer.Timeout -= HideLabel;
		Visible = false;
	}
}
