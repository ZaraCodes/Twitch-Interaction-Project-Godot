using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
public partial class TwitchConnection : Node
{
    [Signal]
    public delegate void UserTokenReceivedEventHandler(string tokenData);

    [Signal]
    public delegate void UserTokenCheckedEventHandler(string login);

    [Signal]
    public delegate void AuthenticatedEventHandler();

    [Signal]
    public delegate void AuthenticationFailedEventHandler();

    [Signal]
    public delegate void OnChatMessageEventHandler(Dictionary chatMessage);

    [Signal]
    public delegate void EventEventHandler(Dictionary data);

    [Signal]
    public delegate void EventsIdEventHandler(string sessionId);

    [Signal]
    public delegate void EventsReconnectEventHandler();

    [Signal]
    public delegate void EventsRevokedEventHandler(string type, string status);

    [Signal]
    public delegate void EventsubConnectedEventHandler();

    [Signal]
    public delegate void BadgeDataEventHandler(string data);

    [Signal]
    public delegate void BadgeCategoryReceivedEventHandler();

    [Signal]
    public delegate void UserTokenRefreshedEventHandler(Dictionary data);

    /// <summary>Tcp Client responsible for sending and receiving chat messages</summary>
    public TcpClient TwitchClient { get; private set; }

    /// <summary>Tcp Server that's used for getting the authorization code</summary>
    private TcpServer server;
    /// <summary>This is used to connect to the server</summary>
    private StreamPeerTcp peer;

    /// <summary>Websocket for subscribing to twitch events</summary>
    private WebSocketPeer eventsub;
    /// <summary>Used to save eventsub messages to avoid responding to the same message twice</summary>
    private Dictionary eventsubMessages;
    /// <summary>Tracks if the eventsub is currently connected</summary>
    private bool eventsubConnected;
    /// <summary>The url to connect to in case the eventsub loses connection</summary>
    private string eventsubReconnectURL;
    /// <summary>The session Id for the current eventsub connection</summary>
    private string sessionID;
    /// <summary>Time keep alive timeout for the current eventsub connection</summary>
    private int keepaliveTimeout;
    /// <summary>The timestamp of the last keepalive message</summary>
    private ulong lastKeepalive;

    /// <summary>This is used to create a read stream from the twitch client</summary>
    private StreamReader reader;
    /// <summary>This is used to create a write stream from the twitch client</summary>
    private StreamWriter writer;

    /// <summary>The url for connecting to the twitch irc chat</summary>
    private const string URL = "irc.chat.twitch.tv";
    /// <summary>The port required to connect to the twitch irc chat</summary>
    private const int PORT = 6667;

    /// <summary>The channel name to connect to</summary>
    public string Channel = string.Empty;

    /// <summary>Dictionary that contains the token with all of its associated data</summary>
    private Dictionary token;
    /// <summary>The username of the connected twitch account</summary>
    private string username;
    /// <summary>The user id of the connected twitch account</summary>
    private string userId;
    /// <summary>The user id of the connected twitch account</summary>
    public string UserId { get { return userId; } }

    /// <summary>The client id of the application registered at twitch</summary>
    private string clientId = string.Empty;
    /// <summary>The client secret of the application registered at twitch</summary>
    private string clientSecret = string.Empty;
    /// <summary>The scopes this application will request from twitch</summary>
    private string[] scopes = new string[] { "channel:read:redemptions", "chat:edit", "chat:read" };

    /// <summary>The user agent of this application</summary>
    private const string USER_AGENT = "User-Agent: ZTI 0.1 (Godot Engine)";

    /// <summary>List of chat responses to work through</summary>
    private List<ChatResponse> chatResponses = new();

    /// <summary>Timer that triggers a reconnection</summary>
    private Timer reconnectionTimer;

    /// <summary>Tracks if currently the client is trying to reconnect with twitch</summary>
    private bool attemptingReconnection;
    /// <summary>The interval for the next reconnection attempt</summary>
    private double reconnectionInterval;
    /// <summary>The max amount of reconnection attempts</summary>
    private int maxReconnectionAttempts;
    /// <summary>The current count of reconnection attempts</summary>
    private int reconnectionAttemptCounter;

    /// <summary>
	/// Tries to authenticate the application with the twitch api.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L146
    /// </summary>
    public async void Authenticate()
    {
        GD.Print("[Authenticate] Loading Config...");
        if (!LoadConfig())
        {
            GD.Print("[Authenticate] Loading Config failed!");
            EmitSignal(SignalName.AuthenticationFailed);
            return;
        }
        GD.Print("[Authenticate] Config loaded successfully!");

        token = new();
        string path = "user://auth/user_token";
        GD.Print("[Authenticate] Checking Token...");
        if (Godot.FileAccess.FileExists(path))
        {
            var file = Godot.FileAccess.OpenEncryptedWithPass(path, Godot.FileAccess.ModeFlags.Read, clientSecret);
            if (file == null)
            {
                GD.Print(Godot.FileAccess.GetOpenError().ToString());
            }

            token = (Dictionary)Json.ParseString(file.GetAsText());

            if (token.ContainsKey("scope") && scopes.Length != 0)
            {
                if (scopes.Length != ((string[])token["scope"]).Length)
                {
                    GD.Print("[Authenticate] The token's scope length does not align with the amount of required scopes.");
                    GetToken();
                    await ToSignal(this, SignalName.UserTokenReceived);
                }
                else
                {
                    foreach (var scope in scopes)
                    {
                        if (!((string[])token["scope"]).Contains(scope))
                        {
                            GD.Print($"[Authenticate] Scope {scope} was not included in the token's scope");
                            GetToken();
                            await ToSignal(this, SignalName.UserTokenReceived);
                            break;
                        }
                    }
                }
            }
            else
            {
                GD.Print("[Authenticate] Token does not contain \"scope\" field.");
                GetToken();
                await ToSignal(this, SignalName.UserTokenReceived);
            }
        }
        else
        {
            GD.Print("[Authenticate] User token file does not exist.");
            GetToken();
            await ToSignal(this, SignalName.UserTokenReceived);
        }

        if (token.TryGetValue("access_token", out var access_token))
        {
            IsTokenValid((string)access_token);
            var result = await ToSignal(this, SignalName.UserTokenChecked);

            username = (string)result[0];

            GD.Print($"[Authenticate] User ID: {userId}");
            GD.Print($"[Authenticate] Username: {username}");

            while (username == string.Empty)
            {
                GD.Print("[Authenticate] Token invalid");
                if (token.TryGetValue("refresh_token", out var refresh_token))
                {
                    if ((string)refresh_token == string.Empty)
                    {
                        GetToken();
                        await ToSignal(this, SignalName.UserTokenReceived);
                    }
                    else
                    {
                        RefreshAccessToken((string)refresh_token);
                        await ToSignal(this, SignalName.UserTokenRefreshed);
                    }

                    if (token.TryGetValue("access_token", out access_token))
                    {
                        IsTokenValid((string)access_token);
                        result = await ToSignal(this, SignalName.UserTokenChecked);
                    }
                    else
                    {
                        GetToken();
                        await ToSignal(this, SignalName.UserTokenReceived);
                        IsTokenValid((string)access_token);
                        result = await ToSignal(this, SignalName.UserTokenChecked);
                    }
                    username = (string)result[0];
                }
                else
                {
                    GD.Print("[Authenticate] No refresh token in dictionary");
                    EmitSignal(SignalName.AuthenticationFailed);
                    return;
                }
            }
            RefreshToken();
            EmitSignal(SignalName.Authenticated);
        }
        else
        {
            GD.Print("[Authenticate] No access token in dictionary");
            EmitSignal(SignalName.AuthenticationFailed);
        }
    }

    /// <summary>
	/// Checks if the token is still valid every hour.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L271
    /// </summary>
    private async void RefreshToken()
    {
        await ToSignal(GetTree().CreateTimer(3600), Timer.SignalName.Timeout);

        GD.Print("[RefreshToken] Refreshing token...");
        IsTokenValid((string)token["access_token"]);
        var result = await ToSignal(this, SignalName.UserTokenChecked);

        if ((string)result[0] == string.Empty)
        {
            GD.Print("[RefreshToken] User token invalid");
            RefreshAccessToken();
            return;
        }
        else RefreshToken();

        var toRemove = new List<string>();
        foreach (var entry in eventsubMessages.Keys)
        {
            if (Time.GetTicksMsec() - (ulong)eventsubMessages[(string)entry] > 600000)
            {
                toRemove.Add((string)entry);
            }
        }
        foreach (var message in toRemove)
        {
            eventsubMessages.Remove(message);
        }
    }

    /// <summary>
	/// Checks if the token is valid
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L259
    /// </summary>
    /// <param name="token"></param>
    private async void IsTokenValid(string token)
    {
        GD.Print("[IsTokenValid] Begin");
        var request = new HttpRequest();
        AddChild(request);
        request.Request("https://id.twitch.tv/oauth2/validate", new string[] { USER_AGENT, $"Authorization: OAuth {token}" });
        var data = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
        request.QueueFree();
        if ((int)data[1] == 200)
        {
            var payload = (Dictionary)Json.ParseString(data[3].AsByteArray().GetStringFromUtf8());

            userId = (string)payload["user_id"];
            GD.Print("[IsTokenValid] Validation successful.");
            EmitSignal(SignalName.UserTokenChecked, (string)payload["login"]);
        }
        else
        {
            var dataDict = (Dictionary)Json.ParseString(data[3].AsByteArray().GetStringFromUtf8());
            if (dataDict.ContainsKey("message"))
                GD.Print($"[IsTokenValid] Validation failed with code {(int)data[1]} and reason {dataDict["message"]}.");
            else
                GD.Print($"[IsTokenValid] Validation failed: {data[3]}.");
            EmitSignal(SignalName.UserTokenChecked, string.Empty);
        }
    }

    /// <summary>
	/// Requests a new access token based on the saved refresh token.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L179
    /// </summary>
    /// <param name="refresh"></param>
    private async void RefreshAccessToken(string refresh)
    {
        GD.Print("[RefreshAccessToken] Begin");
        var request = new HttpRequest();
        AddChild(request);
        request.Request("https://id.twitch.tv/oauth2/token",
                        new string[] { USER_AGENT, "ContentType: application/x-www-form-urlencoded" },
                        HttpClient.Method.Post,
                        $"grant_type=refresh_token&refresh_token={refresh}&client_id={clientId}&client_secret={clientSecret}");

        var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
        request.QueueFree();
        var response = (Dictionary)Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());

        if ((int)reply[1] == 400)
        {
            GD.Print("[RefreshAccessToken] Refreshing token failed, requesting new token");
            GetToken();
            await ToSignal(this, SignalName.UserTokenReceived);
        }
        else if ((int)reply[1] == 401)
        {
            GD.Print("[RefreshAccessToken] Refreshing token failed because it has already been used, requesting new token");
            GetToken();
            await ToSignal(this, SignalName.UserTokenReceived);
        }
        else
        {
            GD.Print("[RefreshAccessToken] Success");
            token = response;
            var file = Godot.FileAccess.OpenEncryptedWithPass("user://auth/user_token", Godot.FileAccess.ModeFlags.Write, clientSecret);
            file.StoreString(reply[3].AsByteArray().GetStringFromUtf8());
            file.Close();
        }
        EmitSignal(SignalName.UserTokenRefreshed, response);
    }

    /// <summary>
    /// Calls the actual refresh access token if there is an access token in the current token dictionary
    /// </summary>
    private async void RefreshAccessToken()
    {
        if (token.TryGetValue("refresh_token", out var refresh_token))
        {
            RefreshAccessToken((string)refresh_token);
        }
        else
        {
            GD.Print("[RefreshAccessToken] No refresh token in dictionary, getting new token...");
            GetToken();
            await ToSignal(this, SignalName.UserTokenReceived);
        }
    }

    /// <summary>
    /// Sets the token dictionary 
    /// </summary>
    /// <param name="token">The new token as a string</param>
    private void SetToken(string token)
    {
        this.token = (Dictionary)Json.ParseString(token);
    }

    /// <summary>
	/// Requests a new token from twitch.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L197
    /// </summary>
    private async void GetToken()
    {
        GD.Print("[GetToken] Fetching new token");
        var scope = string.Empty;

        for (int i = 0; i < scopes.Length - 1; i++)
            scope += $"{scopes[i]} ";

        if (scopes.Length > 0)
            scope += $"{scopes[^1]}";

        scope.URIEncode();
        GD.Print("[GetToken] Waiting for user to login.");
        OS.ShellOpen($"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={clientId}&redirect_uri=http://localhost:39504&scope={scope}");
        server = new();
        server.Listen(39504);

        while (peer == null)
        {
            peer = server.TakeConnection();
            OS.DelayMsec(100);
        }
        while (peer.GetStatus() == StreamPeerTcp.Status.Connected)
        {
            peer.Poll();
            if (peer.GetAvailableBytes() > 0)
            {
                string response = peer.GetUtf8String(peer.GetAvailableBytes());
                if (response == string.Empty)
                {
                    GD.Print("[GetToken] Empty response. Check if your redirect URL is set to http://localhost:39504.");
                    return;
                }
                peer.PutUtf8String("success!");
                int start = response.Find("?");

                response = response.Substring(start + 1, response.Find(" ", start) - start);
                Dictionary data = new();
                foreach (string entry in response.Split("&"))
                {
                    string[] pair = entry.Split('=');
                    data[pair[0]] = pair.Length > 0 ? pair[1] : string.Empty;
                }

                if (data.ContainsKey("error"))
                {
                    string msg = $"Error {data["error"]}: {data["error_description"]}";
                    GD.Print(msg);
                    SendResponse(peer, "400 BAD REQUEST", msg.ToUtf8Buffer());

                    peer.DisconnectFromHost();

                    EmitSignal(SignalName.UserTokenReceived, string.Empty);
                    return;
                }
                else
                {
                    GD.Print("[GetToken] Success!");
                    SendResponse(peer, "200 OK", "Success!".ToUtf8Buffer());

                    peer.DisconnectFromHost();

                    HttpRequest request = new();
                    AddChild(request);
                    string[] headers = new[] { USER_AGENT, "Content-Type: application/x-www-form-urlencoded" };

                    request.Request("https://id.twitch.tv/oauth2/token",
                                    headers,
                                    HttpClient.Method.Post,
                                    $"client_id={clientId}&client_secret={clientSecret}&code={data["code"]}&grant_type=authorization_code&redirect_uri=http://localhost:39504");

                    var answer = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
                    request.QueueFree();

                    if (!DirAccess.DirExistsAbsolute("user://auth"))
                    {
                        DirAccess.MakeDirRecursiveAbsolute("user://auth");
                    }
                    var file = Godot.FileAccess.OpenEncryptedWithPass("user://auth/user_token", Godot.FileAccess.ModeFlags.Write, clientSecret);
                    var token_data = answer[3].AsByteArray().GetStringFromUtf8();
                    file.StoreString(token_data);
                    file.Close();

                    SetToken(token_data);
                    EmitSignal(SignalName.UserTokenReceived, token_data);
                }
            }
            OS.DelayMsec(100);
        }
    }

    /// <summary>
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L249
    /// </summary>
    /// <param name="peer"></param>
    /// <param name="response"></param>
    /// <param name="body"></param>
    private void SendResponse(StreamPeer peer, string response, byte[] body)
    {
        peer.PutData($"HTTP/1.1 {response}\r\n".ToUtf8Buffer());
        peer.PutData("Server: ZTI (Godot Engine)\r\n".ToUtf8Buffer());
        peer.PutData($"Content-Length: {body.Length}\r\n".ToUtf8Buffer());
        peer.PutData("Connection: close\r\n".ToUtf8Buffer());
        peer.PutData("Content-Type: text/plain; charset=UTF-8\r\n".ToUtf8Buffer());
        peer.PutData("\r\n".ToUtf8Buffer());
        peer.PutData(body);
    }

    /// <summary>Connects the client to twitch</summary>
    public void ConnectToTwitch()
    {
        try
        {
            TwitchClient = new TcpClient(URL, PORT);
        }
        catch (SocketException e)
        {
            GD.PrintErr(e);
            return;
        }
        if (username == null)
        {
            GD.PrintErr("[ConnectToTwitch] Username is null");
            return;
        }

        reader = new StreamReader(TwitchClient.GetStream());
        writer = new StreamWriter(TwitchClient.GetStream());

        writer.WriteLine($"PASS oauth:{(string)token["access_token"]}");
        OS.DelayMsec(100);

        writer.WriteLine($"NICK {username.ToLower()}");
        OS.DelayMsec(100);

        writer.WriteLine($"JOIN #{username.ToLower()}");

        writer.WriteLine("CAP REQ :twitch.tv/commands twitch.tv/tags");
        writer.Flush();

        Channel = username.ToLower();
    }

    /// <summary>Pings twitch to keep the connection alive</summary>
    public void PingTwitch()
    {
        writer.WriteLine($"PING {URL}");
        writer.Flush();
    }

    /// <summary>
    /// Sends a chat message in the twitch chat of the current channel
    /// </summary>
    /// <param name="message">The message that will be sent and appear in chat</param>
    public void SendChatMessage(string message)
    {
        chatResponses.Add(new ChatResponse(message, Channel));
    }

    /// <summary>
    /// Replies to a message using its id
    /// </summary>
    /// <param name="id">The id of the chat message</param>
    /// <param name="message">The message that will be sent and appear in chat</param>
    public void ReplyToChatMessage(string id, string message)
    {
        chatResponses.Add(new ChatResponse(message, Channel, id));
    }

    /// <summary>
    /// Attempts to reconnect to twitch
    /// </summary>
    private void AttemptReconnect()
    {
        GD.Print("[AttemptReconnect] Attempting to reconnect...");
        ConnectToTwitch();
        if (!TwitchClient.Connected)
        {
            if (reconnectionAttemptCounter < maxReconnectionAttempts)
            {
                reconnectionAttemptCounter++;
                reconnectionTimer.Start(reconnectionInterval);
                reconnectionInterval *= 2d;
            }
        }
        else
        {
            attemptingReconnection = false;
        }
    }

    /// <summary>
    /// Sends a chat message from the chat responses list
    /// </summary>
    private void SendChatMessage()
    {
        if (chatResponses.Count > 0)
        {
            var message = chatResponses[0];
            chatResponses.Remove(message);
            writer.WriteLine(message.Message);
        }
    }

    /// <summary>
    /// Gets the chat badge data from twitch for global and channel specific badges
    /// </summary>
    private async void GetChatBadges()
    {
        // global chat badges
        GetBadgesFromURL("https://api.twitch.tv/helix/chat/badges/global");
        await ToSignal(this, SignalName.BadgeCategoryReceived);
        // channel chat badges
        GetBadgesFromURL($"https://api.twitch.tv/helix/chat/badges?broadcaster_id={UserId}");
        await ToSignal(this, SignalName.BadgeCategoryReceived);
    }

    /// <summary>
    /// Gets the chat badge data from a given URL
    /// </summary>
    /// <param name="url"></param>
    private async void GetBadgesFromURL(string url)
    {
        var request = new HttpRequest();
        AddChild(request);

        GD.Print($"[GetChatBadges] Sending Request to {url}");
        var headers = new string[] { USER_AGENT, $"Authorization: Bearer {token["access_token"]}", $"Client-Id: {clientId}" };

        request.Request(url, headers, HttpClient.Method.Get);
        var answer = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
        request.QueueFree();


        if ((int)answer[1] == 200)
        {
            GD.Print($"[GetChatBadges] Success for {url}");
            var badgesData = (Dictionary)Json.ParseString(((string)Json.ParseString(answer[3].AsByteArray().GetStringFromUtf8())).Replace("<null>", "\"\"").Replace("\\", ""));
            var badgesInfo = (string[])badgesData["data"];
            foreach (var badgeInfo in badgesInfo)
            {
                //GD.Print($"[GetChatBadges] {badgeInfo}");
                EmitSignal(SignalName.BadgeData, badgeInfo);
            }
            EmitSignal(SignalName.BadgeCategoryReceived);
            return;
        }
        GD.Print($"[GetChatBadges] Request for {url} failed");
        EmitSignal(SignalName.BadgeCategoryReceived);
    }

    /// <summary>
    /// Loads the client id and client secret from the config file
    /// </summary>
    /// <returns></returns>
    private bool LoadConfig()
    {
        var config = new ConfigFile();

        Error error = config.Load("user://auth/secret");

        if (error != Error.Ok)
        {
            GD.PrintErr("[LoadConfig] Loading auth config failed");
            return false;
        }

        clientId = (string)config.GetValue("ClientInfo", "ClientId");
        clientSecret = (string)config.GetValue("ClientInfo", "ClientSecret");
        return true;
    }

    /// <summary>
	/// Connects the application to the twitch eventsub.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L387
    /// </summary>
    private void ConnectWebSocket()
    {
        eventsub = new WebSocketPeer();
        var error = eventsub.ConnectToUrl("wss://eventsub.wss.twitch.tv/ws");
        if (error == Error.Ok)
        {
            GD.Print("[ConnectWebSocket] Eventsub connected to websocket!");
            eventsubConnected = true;
            EmitSignal(SignalName.EventsubConnected);
        }
    }

    /// <summary>
	/// Subscribes to a given event.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L396
    /// </summary>
    /// <param name="eventName">The name of the event</param>
    /// <param name="version">the given version</param>
    /// <param name="conditions">the conditions required by twitch</param>
    public async void SubscribeToEvent(string eventName, int version, Dictionary conditions)
    {
        var data = new Dictionary()
        {
            ["type"] = eventName,
            ["version"] = $"{version}",
            ["condition"] = conditions,
            ["transport"] = new Dictionary()
            {
                ["method"] = "websocket",
                ["session_id"] = sessionID,
            }
        };
        var request = new HttpRequest();
        AddChild(request);
        GD.Print($"[SubscribeToEvent] Subscribing to event {eventName}");
        var headers = new string[] { USER_AGENT, $"Authorization: Bearer {token["access_token"]}", $"Client-Id:{clientId}", $"Content-Type: application/json" };
        request.Request("https://api.twitch.tv/helix/eventsub/subscriptions", headers, HttpClient.Method.Post, Json.Stringify(data));

        var reply = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
        request.QueueFree();
        var response = (Dictionary)Json.ParseString(reply[3].AsByteArray().GetStringFromUtf8());
        GD.Print($"[SubscribeToEvent] Code: {(int)reply[1]} Response: {response}");
        if (response.ContainsKey("error"))
        {
            GD.Print($"[SubscribeToEvent] Subscribing to event {eventName} failed: Error {response["status"]} ({response["error"]}): {response["message"]}");
            return;
        }
        GD.Print($"[SubscribeToEvent] Now listening to {eventName}");
    }

    /// <summary>
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L321
    /// </summary>
    private void ProcessWebsocket()
    {
        eventsub.Poll();
        var state = eventsub.GetReadyState();
        switch (state)
        {
            case WebSocketPeer.State.Open:
                {
                    while (eventsub.GetAvailablePacketCount() > 0)
                    {
                        ProcessEvent(eventsub.GetPacket());
                    }
                    break;
                }
            case WebSocketPeer.State.Closed:
                {
                    if (eventsubConnected)
                    {
                        eventsubConnected = false;
                        var code = eventsub.GetCloseCode();
                        var reason = eventsub.GetCloseReason();
                        GD.Print($"[ProcessWebsocket] Websocket closed with code {code}: {reason}. Clean: {code != -1}");

                    }
                    break;
                }
        }
    }

    /// <summary>
	/// Processes an event.
    /// Based on https://github.com/issork/gift/blob/0545456faa8537a86bb266fe1df8fd3d06505358/addons/gift/gift_node.gd#L348
    /// </summary>
    /// <param name="data"></param>
    private void ProcessEvent(byte[] data)
    {
        var message = (Dictionary)Json.ParseString(data.GetStringFromUtf8());
        var messageId = (string)((Dictionary)message["metadata"])["message_id"];
        if (eventsubMessages.ContainsKey(messageId))
            return;

        eventsubMessages[messageId] = Time.GetTicksMsec();
        var payload = (Dictionary)message["payload"];
        lastKeepalive = Time.GetTicksMsec();
        switch ((string)((Dictionary)message["metadata"])["message_type"])
        {
            case "session_welcome":
                {
                    sessionID = (string)((Dictionary)payload["session"])["id"];
                    keepaliveTimeout = (int)((Dictionary)payload["session"])["keepalive_timeout_seconds"];
                    EmitSignal(SignalName.EventsId, sessionID);
                    break;
                }
            case "session_keepalive":
                {
                    if (payload.ContainsKey("session"))
                    {
                        keepaliveTimeout = (int)((Dictionary)payload["session"])["keepalive_timeout_seconds"];
                    }
                    break;
                }
            case "session_reconnect":
                {
                    eventsubReconnectURL = (string)((Dictionary)payload["session"])["reconnect_url"];
                    GD.Print($"[ProcessEvent] Session reconnect {message}");
                    EmitSignal(SignalName.EventsReconnect);
                    break;
                }
            case "revocation":
                {
                    var type = (string)((Dictionary)payload["subscription"])["type"];
                    var status = (string)((Dictionary)payload["subscription"])["status"];
                    GD.Print($"[ProcessEvent] Revocation: {message}");
                    EmitSignal(SignalName.EventsRevoked, type, status);
                    break;
                }
            case "notification":
                {
                    var eventData = (Dictionary)payload["event"];
                    GD.Print($"[ProcessEvent] Event Data: {eventData}");
                    EmitSignal(SignalName.Event, eventData);
                    break;
                }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        eventsubMessages = new();
        UserTokenReceived += SetToken;

        var reconnectionTimer = new Timer
        {
            Name = "Reconnection Timer",
            OneShot = true,
            Autostart = false
        };
        reconnectionTimer.Timeout += AttemptReconnect;
        maxReconnectionAttempts = 6;
        AddChild(reconnectionTimer);

        var chatMessageResponseTimer = new Timer
        {
            Name = "Chat Message Response Timer",
            OneShot = false,
            WaitTime = 1.6d,
        };
        chatMessageResponseTimer.Timeout += SendChatMessage;
        AddChild(chatMessageResponseTimer);

        chatResponses ??= new List<ChatResponse>();

        Authenticated += ConnectToTwitch;
        Authenticated += GetChatBadges;
        Authenticated += ConnectWebSocket;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Channel == string.Empty)
        {
            return;
        }

        if (TwitchClient is null && !attemptingReconnection || !TwitchClient.Connected && !attemptingReconnection)
        {
            GD.Print("Client is not connected! Starting reconnection attempts.");
            reconnectionAttemptCounter = 0;
            attemptingReconnection = true;
            reconnectionInterval = 1d;
            AttemptReconnect();
        }

        if (TwitchClient.Available > 0)
        {
            string message = reader.ReadLine();
            if (message.Contains("PRIVMSG"))
            {
                int splitPoint = message.IndexOf(' ');
                string metadataString = message[..splitPoint];
                message = message[(splitPoint + 1)..];

                splitPoint = message.IndexOf("!");
                string chatter = message[1..splitPoint];

                splitPoint = message.IndexOf(":", 1);
                var chatMessage = ChatMessage.CreateMessage(metadataString, message[(splitPoint + 1)..], chatter);

                EmitSignal(SignalName.OnChatMessage, chatMessage);
                GD.Print($"{metadataString}\n{message}");
            }
            else if (message.StartsWith("PING"))
            {
                GD.Print(message);
                message = message.Replace("PING", "PONG");
                writer.WriteLine(message);
                writer.Flush();
            }
            else
            {
                GD.Print(message);
            }
        }
        if (eventsub != null)
        {
            ProcessWebsocket();
        }
    }

    public override void _ExitTree()
    {
        if (TwitchClient.Connected)
        {
            writer.WriteLine($"PART #{Channel}");
        }
        base._ExitTree();
    }

    private class ChatResponse
    {
        private string MessageText { get; set; }
        private string Channel { get; set; }
        public string Message
        {
            get
            {
                if (ReplyTo == string.Empty)
                {
                    return $"PRIVMSG #{Channel} :{MessageText}";
                }
                else
                {
                    return $"@reply-parent-msg-id={ReplyTo} PRIVMSG #{Channel} :{MessageText}";
                }
            }
        }
        public string ReplyTo { get; private set; }

        public ChatResponse(string message, string channel, string replyTo = "")
        {
            MessageText = message;
            Channel = channel;
            ReplyTo = replyTo;
        }
    }
}
