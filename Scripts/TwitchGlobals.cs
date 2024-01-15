using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class TwitchGlobals : Node
{
	private Dictionary playerDatas = new Dictionary();
	private System.Collections.Generic.Dictionary<string, string> alejoPronouns;

	public void AddPlayerData(Dictionary message)
	{ 
		var userId = (string)message["UserId"];

		var playerData = FindUserById(userId);
		if (playerData == null)
		{
			// chatMessage.UserId, chatMessage.DisplayName, chatMessage.Color
			playerData = new Dictionary()
			{
				["UserId"] = (string)message["UserId"],
				["DisplayName"] = (string)message["DisplayName"],
				["Color"] = (string)message["Color"],
			};
			playerDatas[userId] = playerData;
			GD.Print(playerDatas);
        }
    }

	public Dictionary FindUserById(string userId)
	{
		if (playerDatas.TryGetValue(userId, out var user)) return (Dictionary) user;
		return null;
	}

	public void SetUser(Dictionary user)
	{
		playerDatas[(string)user["UserId"]] = user;
	}

	public async void GetPronouns()
	{
        // first, get them from pronoundb.org
		var pronoundbUsers = new List<string>();
		var alejoUsers = new List<string>();
		var batches = new List<List<string>>();

		foreach (var key in playerDatas.Keys)
		{
			var value = (Dictionary) playerDatas[(string)key];
			// skip users who already have pronouns
			if (value.ContainsKey("Pronouns")) continue;
			pronoundbUsers.Add((string)key);
			alejoUsers.Add((string)key);
			
			if (pronoundbUsers.Count != 50) continue;
			batches.Add(pronoundbUsers);
			pronoundbUsers = new List<string>();
		}
		if (pronoundbUsers.Count > 0) batches.Add(pronoundbUsers);

		var request = new HttpRequest();
		AddChild(request);

		foreach (var batch in batches)
		{
			var requestURL = "https://pronoundb.org/api/v2/lookup?platform=twitch&ids=";
			foreach (var userId in batch)
			{
				requestURL += $"{userId},";
			}
			if (requestURL.EndsWith(',')) requestURL = requestURL[..^1];

			request.Request(requestURL);
			var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
            var response = (Dictionary)Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());

			foreach (var key in response.Keys)
			{
				var user = FindUserById((string)key);
				var pronouns = string.Empty;
				if (((Dictionary)response[(string)key]).TryGetValue("sets", out var sets))
				{
					if (((Dictionary)sets).TryGetValue("en", out var set))
					{
						foreach (var pronoun in (string[])set)
						{
							pronouns += $"{pronoun.Capitalize()}/";
						}
                        if (pronouns.EndsWith('/')) pronouns = pronouns[..^1];
                    }
				}
				user["Pronouns"] = pronouns;
				alejoUsers.Remove((string)key);
				SetUser(user);
			}
        }

		// if not registered in pronoundb, look for pronouns from https://pronouns.alejo.io/api/users/[user]
		if (alejoPronouns is null)
		{
			alejoPronouns = new();
			request.Request("https://pronouns.alejo.io/api/pronouns");
			var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
			var response = (Array<Dictionary>)Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());
			foreach (var set in response)
			{
				alejoPronouns.Add((string)set["name"], (string)set["display"]);
			}
		}

        foreach (var userId in alejoUsers)
		{
			var user = FindUserById(userId);
			var requestURL = $"https://pronouns.alejo.io/api/users/{((string)user["DisplayName"]).ToLower()}";
			request.Request(requestURL);
			var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
            var response = (Array<Dictionary>) Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());
			if (response.Count > 0)
				user["Pronouns"] = alejoPronouns[(string)response[0]["pronoun_id"]];
			else
				user["Pronouns"] = "-";
			SetUser(user);
        }
        GD.Print(playerDatas);
    }

    public async void AddEventsubSubscriptions()
    {
		await ToSignal(GetTree().CreateTimer(1), Timer.SignalName.Timeout);
        var twitchConnector = GetNode<TwitchConnection>("/root/TwitchConnection");

		var conditions = new Dictionary()
		{
			["broadcaster_user_id"] = twitchConnector.UserId,
		};
		twitchConnector.SubscribeToEvent("channel.channel_points_custom_reward_redemption.add",
										 1, conditions);
    }



    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        var twitchConnector = GetNode<TwitchConnection>("/root/TwitchConnection");
		twitchConnector.OnChatMessage += AddPlayerData;
		twitchConnector.EventsubConnected += AddEventsubSubscriptions;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
