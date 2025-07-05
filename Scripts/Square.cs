using Godot;
using System;

public partial class Square : Node2D
{
    ColorRect baseRect;
    ColorRect highlightRect;
    public bool isLight;

    public override void _Ready()
    {
        highlightRect = GetNode<ColorRect>("Highlight");
        baseRect = GetNode<ColorRect>("Base");
    }

    public void Setup(bool isLight, Vector2 size)
    {
        this.isLight = isLight;
        baseRect.Size = size;
        highlightRect.Size = size;
        highlightRect.Color = new Color(0, 0, 0, 0);
        baseRect.Color = isLight ? BoardColors.lightSquares.normal : BoardColors.darkSquares.normal;
    }

    public void SetHighlightColor(Color color)
    {
        highlightRect.Color = color;
    }


}
