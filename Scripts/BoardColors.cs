using Godot;

public class BoardColors
{

    public static SquareColors lightSquares = new(
        new Color("#F0D9B5"),
        new Color(1.0f, 0.9f, 0.4f, 0.6f),
        new Color(0.4f, 0.6f, 1.0f, 0.5f),
        new Color(0.4f, 1.0f, 0.4f, 0.5f),
        new Color(1.0f, 0.4f, 0.4f, 0.7f)
    );
     public static SquareColors darkSquares = new(
        new Color("#B58863"),
        new Color(1.0f, 0.9f, 0.4f, 0.6f),
        new Color(0.4f, 0.6f, 1.0f, 0.5f),
        new Color(0.4f, 1.0f, 0.4f, 0.5f),
        new Color(1.0f, 0.4f, 0.4f, 0.7f)
    );


    public struct SquareColors(Color normal, Color highlightMove, Color highlightLastMove, Color highlightPossibleMove, Color highlightCheck)
    {
        public Color normal = normal;
        public Color highlightMove = highlightMove;
        public Color highlightLastMove = highlightLastMove;
        public Color highlightPossibleMove = highlightPossibleMove;
        public Color highlightCheck = highlightCheck;
    }
}