using Godot;
using System;
using System.Collections.Generic;

public partial class FinishTrigger : Area3D
{
    [Signal]
    public delegate void OnMarbleFinishedEventHandler(string id);

    private List<string> finishedMarbles;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BodyEntered += FinishPlayer;
        finishedMarbles = new List<string>();
    }

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

    public void Reset()
    {
        finishedMarbles.Clear();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
