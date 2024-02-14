using Godot;
using Godot.Collections;
using System;

public partial class WinningPage : Panel
{
    [Export] private VBoxContainer leaderboard;
    [Export] private RichTextLabel winnerNameLabel;
    [Export] private RichTextLabel winningTimeLabel;

    public void SetWinnerData(string name, string pronouns, string color, double time)
    {
        winnerNameLabel.Text = $"[center][font_size=30][b][color={color}][hint={pronouns}]{name}[/hint][/color]\nwins!";
        winningTimeLabel.Text = RaceTime.GetFormattedTime(time, "[center][font_size=20][i]");
    }

    public void AddLeaderboardEntry(int position, string name, string pronouns, string color, double time)
    {
        var packedLeaderboardEntry = GD.Load<PackedScene>("res://Scenes/leaderboard_entry.tscn");
        var entry = (LeaderboardEntry)packedLeaderboardEntry.Instantiate();
        entry.Init(position, name, pronouns, color, time);
        leaderboard.AddChild(entry);
    }

    private void SetWinner(Dictionary winner, double winningTime)
    {
        var name = (string)winner["DisplayName"];
        var color = (string)winner["Color"];
        var pronouns = (string)winner["Pronouns"];
        SetWinnerData(name, pronouns, color, winningTime);
    }

    private void ResetLeaderboard()
    {
        foreach (var entry in leaderboard.GetChildren())
        {
            entry.Free();
        }
    }

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

    public void QuitGame()
    {
        GetTree().Quit();
    }
}
