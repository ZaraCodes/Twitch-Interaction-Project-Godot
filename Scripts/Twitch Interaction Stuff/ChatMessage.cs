using Godot;
using System.Collections;
using System.Collections.Generic;

public class ChatMessage
{
    public string BadgeInfo { get; private set; }
    public string Badges { get; private set; }
    public string ClientNonce { get; private set; }
    /// <summary>The Color of the user's name</summary>
    public string Color { get; private set; }
    /// <summary>The user's display name is the same as the AccountName but with custom capitalization</summary>
    public string DisplayName { get; private set; }
    public string Emotes { get; private set; }
    /// <summary>tracks if this chat message is the user's first message in this chat</summary>
    public string FirstMsg { get; private set; }
    public string Flags { get; private set; }
    /// <summary>The ID of this chat message</summary>
    public string Id { get; private set; }
    public string Mod { get; private set; }
    public string ReturningChatter { get; private set; }
    /// <summary>The streamer's twitch ID</summary>
    public string RoomId { get; private set; }
    public string Subscriber { get; private set; }
    public string TmiSentTs { get; private set; }
    public string Turbo { get; private set; }
    /// <summary>The ID of the user who sent this message</summary>
    public string UserId { get; private set; }
    public string UserType { get; private set; }
    /// <summary>The text of this chat message</summary>
    public string Text { get; private set; }
    /// <summary>The account name of the user who sent this message</summary>
    public string AccountName { get; private set; }

    public ChatMessage(string rawMetadata, string message, string chatter)
    {
        Text = message;
        AccountName = chatter;

        string[] metadata = rawMetadata.Replace("@", "").Split(';');

        foreach (string s in metadata)
        {
            string[] pair = s.Split('=');
            string key = pair[0];
            string value = pair[1];

            switch (key)
            {
                case "badge-info":
                    BadgeInfo = value; break;
                case "badges":
                    Badges = value; break;
                case "client-nonce":
                    ClientNonce = value; break;
                case "color":
                    Color = value; break;
                case "display-name":
                    DisplayName = value; break;
                case "emotes":
                    Emotes = value; break;
                case "first-msg":
                    FirstMsg = value; break;
                case "flags":
                    Flags = value; break;
                case "id":
                    Id = value; break;
                case "mod":
                    Mod = value; break;
                case "returning-chatter":
                    ReturningChatter = value; break;
                case "room-id":
                    RoomId = value; break;
                case "subscriber":
                    Subscriber = value; break;
                case "tmi-sent-ts":
                    TmiSentTs = value; break;
                case "turbo":
                    Turbo = value; break;
                case "user-id":
                    UserId = value; break;
                case "user-type":
                    UserType = value; break;
                default:
                    break;
            }
        }
    }
}
