using Godot;
using System;

public partial class RaceTime : RichTextLabel
{
	[Export]
	private string bbcode;

	public double Time;
	private bool count;

	public void Start() { count = true; }

	public void Stop() { count = false; }

	public void SetDisplay()
	{
		Text = GetFormattedTime(Time, bbcode);
	}

	public static string GetFormattedTime(double time, string bbcode)
	{
		if (time == -1d) return "--:--.---";

        var thousands = (int)(time * 1000 % 1000d);
        var seconds = Mathf.FloorToInt(time % 60);
        var minutes = Mathf.FloorToInt(time / 60);
        return $"{bbcode}{minutes.ToString().PadLeft(2, '0')}:{seconds.ToString().PadLeft(2, '0')}.{thousands.ToString().PadLeft(3, '0')}";
    }
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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
