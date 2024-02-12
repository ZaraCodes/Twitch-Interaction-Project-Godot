using Godot;
using System;

public partial class PlayerCard : Panel
{
	TwitchGlobals twitchGlobals;

	[Export]
	private RichTextLabel PlayerName;

	[Export]
	private RichTextLabel PlayerPronouns;

	[Export]
	private TextureRect[] badges;

	[Export]
	private RichTextLabel Wins;

	public async void ShowCard(string userId)
	{
        GD.Print("bla");

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

	public void SetWins(int wins)
	{
		Wins.Text = $"Wins: [b] {wins}";
	}

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        twitchGlobals = GetNode<TwitchGlobals>("/root/TwitchGlobals");

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
