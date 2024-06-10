using Godot;
using System;

/// <summary>
/// Node that contains the credits in the main menu
/// </summary>
public partial class Credits : Control
{
    /// <summary>
    /// Opens the link to the clicked credits part
    /// </summary>
    /// <param name="meta"></param>
    public void OnURLPressed(Variant meta)
    {
        OS.ShellOpen((string)meta);
    }
}
