using Godot;
using System;

public partial class TitleBar : HBoxContainer
{
    #region Fields
    /// <summary>Reference to the map name label</summary>
    [Export] public RichTextLabel MapName { get; private set; }

    [Export] public RichTextLabel PlayerNumbers { get; private set; }

    [Export] public Button StartGameButton { get; private set; }

    [Export] public Button JoinButton { get; private set; }

    [Export] public BoxContainer JoinMessageContainer { get; private set; }

    [Export] private RichTextLabel prompt;

    [Export] private string JoinPrompt;
    
    [Export] private string RacePrompt;

    private PackedScene packedJoinMessageScene;
    #endregion

    #region Methods
    public void DisableStartButton()
    {
        StartGameButton.Disabled = true;
    }

    public void DisableTestButton()
    {
        JoinButton.Disabled = true;
    }

    public void SetPromptToRace()
    {
        prompt.Text = RacePrompt;
    }

    public void SetPromptToJoin()
    {
        prompt.Text = JoinPrompt;
    }

    public void Reset()
    {
        SetPromptToJoin();
        StartGameButton.Disabled = false;
        JoinButton.Disabled = false;
    }

    public void CreateSpawnMessage(string name, string color)
    {
        var newJoinMessage = (JoinMessage)packedJoinMessageScene.Instantiate();
        newJoinMessage.SetSpawnMsg(name, new Color(color), "joined!");
        JoinMessageContainer.AddChild(newJoinMessage);
    }

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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
    #endregion
}
