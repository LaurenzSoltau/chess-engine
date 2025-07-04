namespace Chess
{
    public class PositionInfo
    {
        public int[] Squares;
        public bool WhiteToMove;
        public bool WhiteCastleKingside;
        public bool WhiteCastleQueenside;
        public bool BlackCastleKingside;
        public bool BlackCastleQueenside;
        public int EnPassantSquare;
        public int HalfMoveClock;
        public int FullMoveClock;

        public PositionInfo()
        {
            Squares = new int[64];
            EnPassantSquare = -1;
            WhiteCastleKingside = false;
            WhiteCastleQueenside = false;
            BlackCastleKingside = false;
            BlackCastleQueenside = false;
        }
    }
}