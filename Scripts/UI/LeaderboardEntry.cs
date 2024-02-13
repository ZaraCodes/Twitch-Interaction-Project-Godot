using Godot;
using System;

public partial class LeaderboardEntry : HBoxContainer
{
	[Export] private RichTextLabel positionLabel;
	[Export] private RichTextLabel nameLabel;
	[Export] private RichTextLabel timeLabel;

	public void Init(int position, string name, string color, double time)
	{
		positionLabel.Text = $"{position})";
		nameLabel.Text = $"[color={color}]{name}";
		timeLabel.Text = RaceTime.GetFormattedTime(time, string.Empty);
	}
}
