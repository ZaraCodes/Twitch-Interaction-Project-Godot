using Godot;
using System;

public class PlayerData
{
    #region Fields
    /// <summary>The twitch id of this player</summary>
    public string Id { get; private set; }
    /// <summary>The player's name in twitch chat</summary>
    public string DisplayName { get; private set; }
    /// <summary>The color of the player in chat</summary>
    public Color Color { get; private set; }
    /// <summary>If the player has registered their pronouns with one of the two pronoun plugins on twitch, they will be saved here</summary>
    public string Pronouns { get; set; }
    #endregion

    #region Methods
    public PlayerData(string id, string name, string color)
    {
        Id = id;
        DisplayName = name;
        var r = color.Substring(1, 2);
        var g = color.Substring(3, 2);
        var b = color.Substring(5, 2);
        Color = new Color(Convert.ToInt32(r, 16) / 255f, Convert.ToInt32(g, 16) / 255f, Convert.ToInt32(b, 16) / 255f);
    }
    #endregion
}