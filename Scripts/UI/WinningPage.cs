using Godot;
using Godot.Collections;
using System;

public partial class WinningPage : Panel
{
    [Export] private VBoxContainer leaderboard;
    [Export] private RichTextLabel winnerNameLabel;
    [Export] private RichTextLabel winningTimeLabel;

    public void SetWinnerData(string winnerName, string winnerColor, double winningTime)
    {
        winnerNameLabel.Text = $"[center][font_size=30][b][color={winnerColor}]{winnerName}[/color]\nwins!";
        winningTimeLabel.Text = RaceTime.GetFormattedTime(winningTime, "[center][font_size=20][i]");
    }

    public void AddLeaderboardEntry(int position, string name, string color, double time)
    {
        var packedLeaderboardEntry = GD.Load<PackedScene>("res://Scenes/leaderboard_entry.tscn");
        var entry = (LeaderboardEntry)packedLeaderboardEntry.Instantiate();
        entry.Init(position, name, color, time);
        leaderboard.AddChild(entry);
    }

    private void SetWinner(Dictionary winner, double winningTime)
    {
        var name = (string)winner["DisplayName"];
        var color = (string)winner["Color"];
        SetWinnerData(name, color, winningTime);
    }

    public void FillLeaderboard(Dictionary positions, TwitchGlobals twitchGlobals)
    {
        if (!positions.ContainsKey(1))
        {
            SetWinnerData("Noone", "white", -1d);
        }
        var winner = (Dictionary)positions[1];
        SetWinner(twitchGlobals.FindUserById((string)winner["id"]), (double)winner["time"]);

        for (int i = 2; i < 11; i++)
        {
            if (!positions.ContainsKey(i))
                break;
            var player = (Dictionary)positions[i];
            var playerData = twitchGlobals.FindUserById((string)player["id"]);
            AddLeaderboardEntry(i, (string)playerData["DisplayName"], (string)playerData["Color"], (double)player["time"]);
        }
    }
}
