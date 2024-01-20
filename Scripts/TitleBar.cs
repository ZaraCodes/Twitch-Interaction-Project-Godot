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
    #endregion

    #region Methods

    #endregion

    #region Godot Stuff
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
    #endregion
}
