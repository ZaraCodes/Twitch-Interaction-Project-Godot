using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// Node that holds global variables about the game and twitch
/// </summary>
public partial class TwitchGlobals : Node
{
    [Signal]
    public delegate void BadgeDownloadedEventHandler();

    [Signal]
    public delegate void ProfilePictureDownloadedEventHandler();

    [Signal]
    public delegate void PronounsReceivedEventHandler();

    [Signal]
    public delegate ImageTexture TextureReadyEventHandler();

    /// <summary>Dictionary that contains data about players</summary>
    private Dictionary playerDatas = new Dictionary();
    /// <summary>Dictionary that contains data about badges</summary>
    private Dictionary badgeData = new Dictionary();
    /// <summary>Dictionary that contains the downloaded badges</summary>
    private Dictionary downloadedBadges = new Dictionary();
    /// <summary>
    /// Dictionary that contains the pronouns from users of the alejo pronouns plugin
    /// </summary>
    private System.Collections.Generic.Dictionary<string, string> alejoPronouns;

    /// <summary>
    /// Adds player data
    /// </summary>
    /// <param name="message">message that the player sent</param>
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
                ["Badges"] = (string)message["Badges"]
            };
            playerDatas[userId] = playerData;
        }
    }

    /// <summary>Adds test player data</summary>
    /// <param name="id">a fake id</param>
    /// <param name="displayName">a fake name</param>
    /// <param name="color">a fake color</param>
    public void AddPlayerData(string id, string displayName, string color)
    {
        var playerData = new Dictionary()
        {
            ["UserId"] = id,
            ["DisplayName"] = displayName,
            ["Color"] = color,
            ["Pronouns"] = "-"
        };
        playerDatas[id] = playerData;
    }

    /// <summary>
    /// Adds Badge data from a badge to the badge data dictionary
    /// </summary>
    /// <param name="badgeInfo"></param>
    public void AddBadgeData(string badgeInfo)
    {
        badgeInfo = badgeInfo.Replace("\\'", "");
        var badge = new Dictionary();
        Dictionary badgeData;

        badgeData = (Dictionary)Json.ParseString(badgeInfo);

        if (!badgeData.TryGetValue("versions", out _))
        {
            GD.PrintErr($"[AddBadgeData] Couldn't read data: {badgeInfo}");
            return;
        }
        var versions = (string[])badgeData["versions"];
        foreach (var version in versions)
        {
            var badgeVersion = (Dictionary)Json.ParseString(version);
            badge[(string)badgeVersion["id"]] = badgeVersion;
        }
        this.badgeData[badgeData["set_id"]] = badge;
    }

    /// <summary>
    /// Tries to get a badge from the downloaded badges dictionary
    /// </summary>
    /// <param name="badge">The badge we're looking for</param>
    /// <param name="imageTexture">The image texture we assign the badge to if we find it</param>
    /// <returns></returns>
    public bool TryGetBadge(string badge, out ImageTexture imageTexture)
    {
        if (downloadedBadges.ContainsKey(badge))
        {
            imageTexture = (ImageTexture)downloadedBadges[badge];
            return true;
        }
        imageTexture = null;
        return false;
    }

    /// <summary>
    /// Downloads a badge based on the link from the badge data
    /// </summary>
    /// <param name="badge"></param>
    public async void DownloadBadge(string badge)
    {
        var downloadURL = GetBadgeDownloadURL(badge);
        if (downloadURL == string.Empty) return;

        var request = new HttpRequest();
        AddChild(request);
        GD.Print($"[DownloadBadge] Downloading badge {badge} from {downloadURL}");
        request.Timeout = 5;
        request.Request(downloadURL);
        var result = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
        request.QueueFree();

        var image = new Image();
        var error = image.LoadPngFromBuffer(result[3].AsByteArray());
        if (error != Error.Ok)
        {
            GD.PushError($"Couldn't load image from {downloadURL}.");
            EmitSignal(SignalName.BadgeDownloaded);
            return;
        }

        var texture = new ImageTexture();
        texture.SetImage(image);
        GD.Print($"[DownloadBadge] Badge {badge} downloaded");

        downloadedBadges[badge] = texture;
        EmitSignal(SignalName.BadgeDownloaded);
    }

    /// <summary>
    /// Returns a download url from badge data
    /// </summary>
    /// <param name="badge"></param>
    /// <returns></returns>
    private string GetBadgeDownloadURL(string badge)
    {
        var setId = badge.Split('/')[0];
        var version = badge.Split('/')[1];
        Variant variant;

        if (!badgeData.TryGetValue(setId, out variant)) return string.Empty;

        var badgeInfo = (Dictionary)variant;
        if (!badgeInfo.TryGetValue(version, out variant)) return string.Empty;

        var badgeVersion = (Dictionary)variant;
        if (badgeVersion.TryGetValue("image_url_1x", out variant)) return (string)variant;

        return string.Empty;
    }

    /// <summary>
    /// Finds a user by id in the player data dictionary. returns null if no player is found
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Dictionary FindUserById(string userId)
    {
        if (playerDatas.TryGetValue(userId, out var user)) return (Dictionary)user;
        return null;
    }

    /// <summary>
    /// Sets a user based on it's dictionary and id
    /// </summary>
    /// <param name="user"></param>
    public void SetUser(Dictionary user)
    {
        playerDatas[(string)user["UserId"]] = user;
    }

    /// <summary>
    /// Tries to get the pronouns of users from two different plugins for this purpose
    /// </summary>
    public async void GetPronouns()
    {
        var pronoundbUsers = new List<string>();
        var alejoUsers = new List<string>();
        var batches = new List<List<string>>();

        // first, get them from pronoundb.org
        foreach (var key in playerDatas.Keys)
        {
            var value = (Dictionary)playerDatas[(string)key];
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
            if (user == null) continue;
            var requestURL = $"https://pronouns.alejo.io/api/users/{((string)user["DisplayName"]).ToLower()}";
            request.Request(requestURL);
            var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
            var response = (Array<Dictionary>)Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());
            if (response.Count > 0)
                user["Pronouns"] = alejoPronouns[(string)response[0]["pronoun_id"]];
            else
                user["Pronouns"] = "-";
            SetUser(user);
        }
    }

    /// <summary>
    /// Adds an eventsub subscription for channelpoint redeems
    /// </summary>
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
        //twitchConnector.OnChatMessage += AddPlayerData;
        twitchConnector.EventsubConnected += AddEventsubSubscriptions;
        twitchConnector.BadgeData += AddBadgeData;
    }
}
