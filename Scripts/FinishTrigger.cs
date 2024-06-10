using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Trigger that lets a player finish
/// </summary>
public partial class FinishTrigger : Area3D
{
    /// <summary>Signal that gets emitted when a player finishes</summary>
    /// <param name="id">The player's user id</param>
    [Signal]
    public delegate void OnMarbleFinishedEventHandler(string id);

    /// <summary>List of ids of players who finished the race</summary>
    private List<string> finishedMarbles;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BodyEntered += FinishPlayer;
        finishedMarbles = new List<string>();
    }

    /// <summary>
    /// Lets a player finish
    /// </summary>
    /// <param name="body"></param>
    private void FinishPlayer(Node3D body)
    {
        PlayerMarble parent;
        try
        {
            parent = body.GetParent<PlayerMarble>();
        }
        catch
        {
            return;
        }
        if (parent != null)
        {
            if (!finishedMarbles.Contains(parent.ID))
            {
                EmitSignal(SignalName.OnMarbleFinished, parent.ID);
                finishedMarbles.Add(parent.ID);
            }
        }
    }

    /// <summary>
    /// Resets the trigger's data
    /// </summary>
    public void Reset()
    {
        finishedMarbles.Clear();
    }
}
