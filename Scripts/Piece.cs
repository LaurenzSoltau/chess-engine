using Godot;
using System;

public partial class Piece : Node2D
{
    private Sprite2D sprite;
    public int pieceCode;

    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public void Setup(Texture2D texture, Vector2 position)
    {
        sprite.Texture = texture;
        Position = position;
    }

}
