using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

public partial class TwitchConnection : Node
{
	[Signal]
	public delegate void UserTokenReceivedEventHandler(string tokenData);

	public TcpClient TwitchClient { get; private set; }

	private TcpServer server;
	private StreamPeerTcp peer;

	private StreamReader reader;
	private StreamWriter writer;

	private const string URL = "irc.chat.twitch.tv";
	private const int PORT = 6667;

	private const string USER = "TearsOfTheZara";
	private const string OAuth = "oauth:57o8qcq8cwkd53v2ycdl1idvqjxbnq";
	public string Channel = string.Empty;

	private Dictionary token;
	private string clientId = "pfoxn89i1xs0sjucxqjji5fl9uqdcv";
	private string clientSecret = "xc93bw00adn9gvb6f7sm9svl2l5zk8";
	private string[] scopes = new string[] { "channel:read:redemptions", "chat:edit", "chat:read" };

	private const string USER_AGENT = "User-Agent: ZTI 0.1 (Godot Engine)";

	public List<PlayerData> PlayerDatas { get; private set; }

	/// <summary>Timer that triggers a reconnection</summary>
	private Timer reconnectionTimer;

	private bool attemptingReconnection;
	private double reconnectionInterval;
	private int maxReconnectionAttempts;
	private int reconnectionAttemptCounter;

	/// <summary>Event that gets invoked when a chat message comes in</summary>
	/// <param name="message">The chat message</param>
	public delegate void ChatMessageEventHandler(ChatMessage message);

	public ChatMessageEventHandler OnChatMessage;

	//private async void Authenticate(string clientId, string clientSecret)
	private void Authenticate()
	{
		string path = "user://auth/user_token";
		GD.Print("Checking Token...");
		if (Godot.FileAccess.FileExists(URL))
		{
			var file = Godot.FileAccess.OpenEncryptedWithPass(path, Godot.FileAccess.ModeFlags.Read, clientSecret);
			token = (Dictionary)Json.ParseString(file.GetAsText());

			if (token.ContainsKey("scope") && scopes.Length != 0)
			{
				if (scopes.Length != ((string[])token["scope"]).Length)
					GetToken();
				else
				{
					foreach (var scope in scopes)
					{
						if (!((Dictionary)token["scope"]).ContainsKey(scope))
							GetToken();
					}
				}
			}
			else GetToken();
		}
		else GetToken();
	}

	private void SetToken(string token)
	{
		this.token = (Dictionary)Json.ParseString(token);
	}

	private async void GetToken()
	{
		GD.Print("Fetching new token");
		var scope = string.Empty;

		for (int i = 0; i < scopes.Length - 1; i++)
			scope += $"{scopes[i]} ";

		if (scopes.Length > 0)
			scope += $"{scopes[^1]}";

		scope.URIEncode();
		OS.ShellOpen($"https://id.twitch.tv/oauth2/authorize?respose_type=code&client_id={clientId}&redirect_uri=http://localhost:39504&scope={scope}");
		server.Listen(39504);
		GD.Print("Waiting for user to login.");

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
					GD.Print("Empty response. Check if your redirect URL is set to http://localhost:39504.");
					return;
				}
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
					break;
				}
				else
				{
					GD.Print("Success!");
					SendResponse(peer, "200 OK", "Success!".ToUtf8Buffer());
					peer.DisconnectFromHost();
					HttpRequest request = new();
					AddChild(request);
					string[] headers = new[] { USER_AGENT, "Content-Type: application/x-www-form-urlencoded" };
					//request.RequestCompleted += SaveToken;
					request.Request("https://id.twitch.tv/oauth2/token", headers, HttpClient.Method.Post, $"client_id={clientId}&client_secret={clientSecret}&code={data["code"]}&grant_type=authorization_code&redirect_uri=http://localhost:39504");
					
					var answer = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);
					if (!DirAccess.DirExistsAbsolute("user://auth"))
					{
						DirAccess.MakeDirRecursiveAbsolute("user://auth");
					}
					var file = Godot.FileAccess.OpenEncryptedWithPass("user://auth/user_token", Godot.FileAccess.ModeFlags.Write, clientSecret);
					var token_data = answer[3].AsByteArray().GetStringFromUtf8();
					file.StoreString(token_data);
					//EmitSignal(SignalName.UserTokenReceived, token_data);
					SetToken(token_data);
				}
			}
			OS.DelayMsec(100);
		}
	}

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
		TwitchClient = new TcpClient(URL, PORT);
		reader = new StreamReader(TwitchClient.GetStream());
		writer = new StreamWriter(TwitchClient.GetStream());

		writer.WriteLine($"PASS {OAuth}");
		writer.WriteLine($"NICK {USER.ToLower()}");
		writer.WriteLine($"JOIN #{Channel.ToLower()}");

		writer.WriteLine("CAP REQ :twitch.tv/commands twitch.tv/tags");
		writer.Flush();
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
		writer.WriteLine($"PRIVMSG #{Channel} :{message}");
		writer.Flush();
	}

	/// <summary>
	/// Replies to a message using its id
	/// </summary>
	/// <param name="id">The id of the chat message</param>
	/// <param name="message">The message that will be sent and appear in chat</param>
	public void ReplyToChatMessage(string id, string message)
	{
		writer.WriteLine($"@reply-parent-msg-id={id} PRIVMSG #{Channel} :{message}");
		writer.Flush();
	}

	private void AttemptReconnect()
	{
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
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		UserTokenReceived += SetToken;

		reconnectionTimer = new Timer();
		reconnectionTimer.Name = "Reconnection Timer";
		reconnectionTimer.OneShot = true;
		reconnectionTimer.Timeout += AttemptReconnect;
		AddChild(reconnectionTimer);

		maxReconnectionAttempts = 6;

		//ConnectToTwitch();
		PlayerDatas = new();
	}
	
	private void LoadConfig()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Channel == string.Empty)
		{
			return;
		}

		if (!TwitchClient.Connected && !attemptingReconnection)
		{
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
				ChatMessage chatMessage = new(metadataString, message[(splitPoint + 1)..], chatter);

				OnChatMessage?.Invoke(chatMessage);
				

				var playerData = PlayerDatas.Find(user => user.Id == chatMessage.UserId);
				if (playerData == null)
				{
					playerData = new(chatMessage.UserId, chatMessage.DisplayName, chatMessage.Color);
					PlayerDatas.Add(playerData);
				}
				GD.Print($"{metadataString}\n{message}");
			}
			else if (message.StartsWith("PING"))
			{
				GD.Print(message);
				message.Replace("PING", "PONG");
				writer.WriteLine(message);
				writer.Flush();
			}
			else
			{
				GD.Print(message);
			}
		}
	}
}
