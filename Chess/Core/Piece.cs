using System;

namespace Chess
{
    public class Piece
    {

        public const int White = 8;
        public const int Black = -8;
        public const int None = 0;
        public const int WhiteKing = 1;
        public const int WhiteQueen = 2;
        public const int WhitePawn = 3;
        public const int WhiteRook = 4;
        public const int WhiteKnight = 5;
        public const int WhiteBishop = 6;

        public const int BlackKing = -1;
        public const int BlackQueen = -2;
        public const int BlackPawn = -3;
        public const int BlackRook = -4;
        public const int BlackKnight = -5;
        public const int BlackBishop = -6;

        public static bool IsWhite(int pieceCode)
        {
            return pieceCode > 0;
        }

        public static bool IsColor(int pieceCode, int colorIndex)
        {
            return Math.Sign(pieceCode) == Math.Sign(colorIndex);
        }

        public static int GetColor(int pieceCode)
        {
            if (pieceCode > 0) return 8;
            if (pieceCode < 0) return -8;
            return 0;
        }
    }
    
}