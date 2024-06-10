using Godot;
using System;

/// <summary>
/// Timer in the top left corner of the screen that shows the current race time
/// </summary>
public partial class RaceTime : RichTextLabel
{
	/// <summary>
	/// The bbcode to format the label
	/// </summary>
	[Export]
	private string bbcode;

	/// <summary>The time that will count up</summary>
	public double Time;
	/// <summary>Sets if the timer should count or not</summary>
	private bool count;

	/// <summary>Starts the count</summary>
	public void Start() { count = true; }

	/// <summary>Stops the count</summary>
	public void Stop() { count = false; }

	/// <summary>
	/// Sets the timer display
	/// </summary>
	public void SetDisplay()
	{
		Text = GetFormattedTime(Time, bbcode);
	}

	/// <summary>
	/// Resets the timer
	/// </summary>
	public void Reset()
	{
		Time = 0d;
		SetDisplay();
	}

	/// <summary>
	/// Gets a formatted time string
	/// </summary>
	/// <param name="time">The time that will be converted</param>
	/// <param name="bbcode">The bbcode to format the string</param>
	/// <returns></returns>
	public static string GetFormattedTime(double time, string bbcode)
	{
		if (time == -1d) return "--:--.---";

        var thousands = (int)(time * 1000 % 1000d);
        var seconds = Mathf.FloorToInt(time % 60);
        var minutes = Mathf.FloorToInt(time / 60);
        return $"{bbcode}{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}.{thousands.ToString().PadLeft(3, '0')}";
    }
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (count)
		{
			Time += delta;
			SetDisplay();
		}
	}
}
