using Godot;
using System;
using Chess.Testing;

public partial class TestSuite : Control
{
    [Export] PackedScene TestButtonScene;
    [Export] PackedScene TestLabelScene;
    TestLabel[] testLabels;
    Test[] tests;
    VBoxContainer buttonContainer;
    VBoxContainer labelContainer;
    Perft tester;
    public override void _Ready()
    {
        tester = new();
        buttonContainer = GetNode<VBoxContainer>("Container/ButtonContainer");
        labelContainer = GetNode<VBoxContainer>("Container/ResultContainer");
        tests = TestUtil.GetTests();
        testLabels = new TestLabel[tests.Length];
        for (int i = 0; i < tests.Length; i++)
        {
            var testButton = TestButtonScene.Instantiate<TestButton>();
            buttonContainer.AddChild(testButton);
            testButton.testIndex = i + 1;
            testButton.label.Text = $"Run Test {testButton.testIndex}";
            testButton.Pressed += () => HandleButtonPressed(testButton);
        }

    }

    void HandleButtonPressed(TestButton button)
    {
        int index = button.testIndex - 1;
        tester.RunSingleTest(index, false);
        var label = TestLabelScene.Instantiate<TestLabel>();
        labelContainer.AddChild(label);
        Perft.TestResults testResults = tester.testResults[index];
        string boilerPlateText = "Counted: {0} Nodes at Depth: {1} in {2}ms";
        string labelString = String.Format(boilerPlateText, testResults.nodeCount, testResults.depth, testResults.elapsedTime) + $"  ({testResults.resultString})";
        label.Text = labelString;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                RunTestsAsync();
            }
        }
    }

    private async void RunTestsAsync()
{
    for (int i = 0; i < tester.TestCount; i++)
    {
        tester.RunSingleTest(i, false);

        var label = TestLabelScene.Instantiate<TestLabel>();
        labelContainer.AddChild(label);

        Perft.TestResults testResults = tester.testResults[i];
        string boilerPlateText = "Counted: {0} Nodes at Depth: {1} in {2}ms";
        string labelString = String.Format(boilerPlateText, testResults.nodeCount, testResults.depth, testResults.elapsedTime) + $"  ({testResults.resultString})";
        label.Text = labelString;

        await ToSignal(GetTree(), "process_frame"); // ← UI hat einen Frame zum Zeichnen
    }
}
}
