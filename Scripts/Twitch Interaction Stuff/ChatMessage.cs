using Godot;
using Godot.Collections;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class contains a static method to parse an incoming chat message
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Parses the metadata of a message into a dictionary for easier access
    /// </summary>
    /// <param name="rawMetadata">The metadata of the message</param>
    /// <param name="message">The text of the message</param>
    /// <param name="chatter">The chatter's name</param>
    /// <returns></returns>
    public static Dictionary CreateMessage(string rawMetadata, string message, string chatter)
    {
        var newMsg = new Dictionary
        {
            ["Text"] = message,
            ["AccountName"] = chatter
        };

        string[] metadata = rawMetadata.Replace("@", "").Split(';');

        foreach (string s in metadata)
        {
            string[] pair = s.Split('=');
            string key = pair[0];
            string value = pair[1];

            switch (key)
            {
                case "badge-info":
                    newMsg["BadgeInfo"] = value; break;
                case "badges":
                    newMsg["Badges"] = value; break;
                case "client-nonce":
                    newMsg["ClientNonce"] = value; break;
                case "color":
                    newMsg["Color"] = value; break;
                case "display-name":
                    newMsg["DisplayName"] = value; break;
                case "emotes":
                    newMsg["Emotes"] = value; break;
                case "first-msg":
                    newMsg["FirstMsg"] = value; break;
                case "flags":
                    newMsg["Flags"] = value; break;
                case "id":
                    newMsg["Id"] = value; break;
                case "mod":
                    newMsg["Mod"] = value; break;
                case "returning-chatter":
                    newMsg["ReturningChatter"] = value; break;
                case "room-id":
                    newMsg["RoomId"] = value; break;
                case "subscriber":
                    newMsg["Subscriber"] = value; break;
                case "tmi-sent-ts":
                    newMsg["TmiSentTs"] = value; break;
                case "turbo":
                    newMsg["Turbo"] = value; break;
                case "user-id":
                    newMsg["UserId"] = value; break;
                case "user-type":
                    newMsg["UserType"] = value; break;
                default:
                    break;
            }
        }
        return newMsg;
    }
}
