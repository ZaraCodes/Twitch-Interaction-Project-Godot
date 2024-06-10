using Godot;
using System;

/// <summary>
/// The bar at the top of the screen displays some information about the game
/// </summary>
public partial class TitleBar : HBoxContainer
{
    #region Fields
    /// <summary>Reference to the map name label</summary>
    [Export] public RichTextLabel MapName { get; private set; }

    /// <summary>Reference to the label that displays the player numbers</summary>
    [Export] public RichTextLabel PlayerNumbers { get; private set; }

    /// <summary>Reference to the button that starts the game</summary>
    [Export] public Button StartGameButton { get; private set; }

    /// <summary>Reference to the button that fills the track with test marbles</summary>
    [Export] public Button JoinButton { get; private set; }

    /// <summary>The box container that displays the join and death messages</summary>
    [Export] public BoxContainer JoinMessageContainer { get; private set; }
    /// <summary>Reference to the label that displays what players can do</summary>
    [Export] private RichTextLabel prompt;
    /// <summary>String that gets displayed when players can join</summary>
    [Export] private string JoinPrompt;
    /// <summary>String that gets displayed during the race</summary>
    [Export] private string RacePrompt;

    /// <summary>Cache of the packed scene that is used to instantiate join messages</summary>
    private PackedScene packedJoinMessageScene;
    #endregion

    #region Methods
    /// <summary>
    /// Disables the start button
    /// </summary>
    public void DisableStartButton()
    {
        StartGameButton.Disabled = true;
    }

    /// <summary>
    /// Disables the test button
    /// </summary>
    public void DisableTestButton()
    {
        JoinButton.Disabled = true;
    }

    /// <summary>
    /// Sets the promt text to the race text
    /// </summary>
    public void SetPromptToRace()
    {
        prompt.Text = RacePrompt;
    }

    /// <summary>
    /// Sets the prompt text to the join text
    /// </summary>
    public void SetPromptToJoin()
    {
        prompt.Text = JoinPrompt;
    }

    /// <summary>
    /// Resets the title bar
    /// </summary>
    public void Reset()
    {
        SetPromptToJoin();
        StartGameButton.Disabled = false;
        JoinButton.Disabled = false;
    }

    /// <summary>
    /// Creates a join message
    /// </summary>
    /// <param name="name">The player's name</param>
    /// <param name="color">The player's color</param>
    public void CreateSpawnMessage(string name, string color)
    {
        var newJoinMessage = (JoinMessage)packedJoinMessageScene.Instantiate();
        newJoinMessage.SetSpawnMsg(name, new Color(color), "joined!");
        JoinMessageContainer.AddChild(newJoinMessage);
    }

    /// <summary>
    /// Creates a fall message
    /// </summary>
    /// <param name="name">The player's name</param>
    /// <param name="color">The player's color</param>
    public void CreateFallMessage(string name, string color)
    {
        var newJoinMessage = (JoinMessage)packedJoinMessageScene.Instantiate();
        newJoinMessage.SetDeathMsg(name, new Color(color), "fell.");
        JoinMessageContainer.AddChild(newJoinMessage);
    }
    #endregion

    #region Godot Stuff
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        packedJoinMessageScene = GD.Load<PackedScene>("res://Scenes/join_message.tscn");
        SetPromptToJoin();
    }
    #endregion
}
