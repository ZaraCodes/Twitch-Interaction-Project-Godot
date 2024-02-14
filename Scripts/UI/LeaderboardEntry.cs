using Godot;
using System;

public partial class LeaderboardEntry : HBoxContainer
{
	[Export] private RichTextLabel positionLabel;
	[Export] private RichTextLabel nameLabel;
	[Export] private RichTextLabel timeLabel;

	public void Init(int position, string name, string pronouns, string color, double time)
	{
        positionLabel.Text = $"{position})";
        nameLabel.Text = $"[color={color}][hint={pronouns}]{name}";
        timeLabel.Text = $"{RaceTime.GetFormattedTime(time, string.Empty)}";
	}
}
