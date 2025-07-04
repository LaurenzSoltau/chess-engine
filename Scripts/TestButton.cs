using Godot;
using System;

public partial class TestButton : Button
{
    public bool isFullSuite;
    public int testIndex;
    public Label label;

    public override void _Ready()
    {
        label = GetNode<Label>("Label");
    }
}
