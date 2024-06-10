using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Node that displays the leaderboard with the top 10 positions after the race has ended
/// </summary>
public partial class WinningPage : Panel
{
    /// <summary>Reference to the container that holds all entries other than the first</summary>
    [Export] private VBoxContainer leaderboard;
    /// <summary>Reference to the label that displays the winner's name</summary>
    [Export] private RichTextLabel winnerNameLabel;
    /// <summary>Reference to the label that displays the winning time</summary>
    [Export] private RichTextLabel winningTimeLabel;

    /// <summary>
    /// Sets the data from the winner
    /// </summary>
    /// <param name="name">The winner's name</param>
    /// <param name="pronouns">The winner's pronouns (displayed as a tooltip when hovered)</param>
    /// <param name="color">The color of the winner's name</param>
    /// <param name="time">The winning time</param>
    public void SetWinnerData(string name, string pronouns, string color, double time)
    {
        winnerNameLabel.Text = $"[center][font_size=30][b][color={color}][hint={pronouns}]{name}[/hint][/color]\nwins!";
        winningTimeLabel.Text = RaceTime.GetFormattedTime(time, "[center][font_size=20][i]");
    }

    /// <summary>
    /// Adds a leaderboard entry
    /// </summary>
    /// <param name="position">The player's position</param>
    /// <param name="name">The player's name</param>
    /// <param name="pronouns">The player's pronouns</param>
    /// <param name="color">The player's display color</param>
    /// <param name="time">The player's time</param>
    public void AddLeaderboardEntry(int position, string name, string pronouns, string color, double time)
    {
        var packedLeaderboardEntry = GD.Load<PackedScene>("res://Scenes/leaderboard_entry.tscn");
        var entry = (LeaderboardEntry)packedLeaderboardEntry.Instantiate();
        entry.Init(position, name, pronouns, color, time);
        leaderboard.AddChild(entry);
    }

    /// <summary>
    /// Parses the winner dictionary to usable strings
    /// </summary>
    /// <param name="winner">A dictionary that contains data related to the winner</param>
    /// <param name="winningTime">The winning time</param>
    private void SetWinner(Dictionary winner, double winningTime)
    {
        var name = (string)winner["DisplayName"];
        var color = (string)winner["Color"];
        var pronouns = (string)winner["Pronouns"];
        SetWinnerData(name, pronouns, color, winningTime);
    }

    /// <summary>
    /// Resets the leaderboard
    /// </summary>
    private void ResetLeaderboard()
    {
        foreach (var entry in leaderboard.GetChildren())
        {
            entry.Free();
        }
    }

    /// <summary>
    /// Fills the leaderboard with the data from the race
    /// </summary>
    /// <param name="positions">Dictionary containing race times, positions, and player ids</param>
    /// <param name="twitchGlobals">Reference to the twitch globals node</param>
    public void FillLeaderboard(Dictionary positions, TwitchGlobals twitchGlobals)
    {
        ResetLeaderboard();
        if (!positions.ContainsKey(1))
        {
            SetWinnerData("Noone", "-", "white", -1d);
        }
        var winner = (Dictionary)positions[1];
        SetWinner(twitchGlobals.FindUserById((string)winner["id"]), (double)winner["time"]);

        for (int i = 2; i < 11; i++)
        {
            if (!positions.ContainsKey(i))
                break;
            var player = (Dictionary)positions[i];
            var playerData = twitchGlobals.FindUserById((string)player["id"]);
            var pronouns = "none";
            if (playerData.ContainsKey("Pronouns")) pronouns = (string)playerData["Pronouns"];
            AddLeaderboardEntry(i, (string)playerData["DisplayName"], pronouns, (string)playerData["Color"], (double)player["time"]);
        }
    }

    /// <summary>
    /// Quits the game
    /// </summary>
    public void QuitGame()
    {
        GetTree().Quit();
    }
}
