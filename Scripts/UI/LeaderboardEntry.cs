using Godot;
using System;

/// <summary>
/// Represents an entry on the ending leaderboard
/// </summary>
public partial class LeaderboardEntry : HBoxContainer
{
	/// <summary>The label that displays the player's position</summary>
	[Export] private RichTextLabel positionLabel;
	/// <summary>The label that displays the player's name</summary>
	[Export] private RichTextLabel nameLabel;
	/// <summary>The label that displays the player's time</summary>
	[Export] private RichTextLabel timeLabel;

	/// <summary>
	/// Initializes the leaderboard entry
	/// </summary>
	/// <param name="position">The position of the player</param>
	/// <param name="name">The player's name</param>
	/// <param name="pronouns">The player's pronouns will be displayed as a tooltip if the streamer hovers over the name</param>
	/// <param name="color">The player's color</param>
	/// <param name="time">The player's time</param>
	public void Init(int position, string name, string pronouns, string color, double time)
	{
        positionLabel.Text = $"{position})";
        nameLabel.Text = $"[color={color}][hint={pronouns}]{name}";
        timeLabel.Text = $"{RaceTime.GetFormattedTime(time, string.Empty)}";
	}
}
