using Godot;
using System;

/// <summary>
/// Unused Node that was supposed to display information about the player during the race
/// </summary>
public partial class PlayerCard : Panel
{
    /// <summary>Reference to the twitch globals node</summary>
    TwitchGlobals twitchGlobals;

    /// <summary>Reference to the label that displays the player's name</summary>
    [Export]
    private RichTextLabel PlayerName;

    /// <summary>Reference to the label that displays the player's pronouns</summary>
    [Export]
    private RichTextLabel PlayerPronouns;

    /// <summary>An array of texture rects that contains three items for the badges</summary>
    [Export]
    private TextureRect[] badges;

    /// <summary>Reference to the label that displays how often that player has won</summary>
    [Export]
    private RichTextLabel Wins;

    /// <summary>
    /// Shows info about the player
    /// </summary>
    /// <param name="userId">The player to display info about</param>
    public async void ShowCard(string userId)
    {
        foreach (var badge in this.badges)
        {
            badge.Texture = null;
        }
        if (!twitchGlobals.FindUserById(userId).ContainsKey("Pronouns"))
        {
            twitchGlobals.GetPronouns();
            await ToSignal(twitchGlobals, TwitchGlobals.SignalName.PronounsReceived);
        }

        var user = twitchGlobals.FindUserById(userId);
        if (user.TryGetValue("Pronouns", out var pronouns))
            PlayerPronouns.Text = (string)pronouns;
        else
            PlayerPronouns.Text = "--";
        if (user.TryGetValue("DisplayName", out var name))
            PlayerName.Text = (string)name;

        else { PlayerName.Text = "--"; }
        if (user.TryGetValue("Badges", out var badges))
        {
            int badgeIdx = 0;
            foreach (string badge in GD.VarToStr(badges).Split(','))
            {
                if (twitchGlobals.TryGetBadge(badge, out var texture))
                {
                    this.badges[badgeIdx].Texture = texture;
                }
            }
        }
        Visible = true;
    }

    /// <summary>
    /// Sets the wins label
    /// </summary>
    /// <param name="wins"></param>
    public void SetWins(int wins)
    {
        Wins.Text = $"Wins: [b] {wins}";
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        twitchGlobals = GetNode<TwitchGlobals>("/root/TwitchGlobals");
    }
}
