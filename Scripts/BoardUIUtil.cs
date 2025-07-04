using System;

public static class BoardUIUtil
{
    public const string PieceScenePath = "res://Scenes/piece.tscn";
    public static string GetPieceTexturePath(int pieceCode)
    {
        string color = pieceCode > 0 ? "white" : "black";
        string type = pieceCode switch
        {
            Chess.Piece.WhitePawn or Chess.Piece.BlackPawn   => "pawn",
            Chess.Piece.WhiteKnight or Chess.Piece.BlackKnight => "knight",
            Chess.Piece.WhiteBishop or Chess.Piece.BlackBishop => "bishop",
            Chess.Piece.WhiteRook or Chess.Piece.BlackRook     => "rook",
            Chess.Piece.WhiteQueen or Chess.Piece.BlackQueen   => "queen",
            Chess.Piece.WhiteKing or Chess.Piece.BlackKing     => "king",
            _ => "unknown"
        };

        return $"res://Assets/{color}-{type}.png";
        
    }
}