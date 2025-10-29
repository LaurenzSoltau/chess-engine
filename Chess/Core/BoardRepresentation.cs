using System;
using System.Diagnostics.CodeAnalysis;

namespace Chess
{
    public static class BoardRepresentation
    {
        public static int IndexFromCoord(int rank, int file)
        {
            if (rank < 0 || rank > 7 || file < 0 || file > 7)
            {
                throw new ArgumentOutOfRangeException("Invalid rank or file");
            }
            return rank * 8 + file;
        }

        public static (int rank, int file) CoordFromIndex(int index)
        {
            if (index > 63 || index < 0)
            {
                throw new ArgumentOutOfRangeException("Invalid index");
            }
            return (index / 8, index % 8);
        }

        public static int PerspectiveIndex(int index, bool fromWhitePerspective)
        {
            (int rank, int file) = CoordFromIndex(index);

            if (!fromWhitePerspective)
            {
                rank = 7 - rank;
                file = 7 - file;
            }

            return IndexFromCoord(rank, file);
        }

        public static string IndexToAlgebraic(int index)
        {
            int rank = CoordFromIndex(index).rank;
            int file = CoordFromIndex(index).file;

            char rankChar = (char)('1' + rank);
            char fileChar = (char)('a' + file);

            return fileChar.ToString() + rankChar.ToString();
        }
        public static int AlgebraicToIndex(string square)
        {
            char file = square[0];
            char rank = square[1];

            int fileNumber = file - 'a';
            int rankNumber = rank - '1';

            return rankNumber * 8 + fileNumber;

        }
        public static bool IsLightSquare(int squareIndex)
        {
            int rank = squareIndex / 8;
            int file = squareIndex % 8;
            return (rank + file) % 2 == 0;
        }

        public static PositionInfo BoardToPositionInfo(Board board)
        {
            PositionInfo posInfo = new();
            board.Squares.CopyTo(posInfo.Squares, 0);
            posInfo.EnPassantSquare = board.EnPassantSquare;
            (bool, bool) whiteCastleRights = board.HasColorCastleRight(Piece.White);
            (bool, bool) blackCastleRights = board.HasColorCastleRight(Piece.Black);
            posInfo.WhiteCastleKingside = whiteCastleRights.Item1;
            posInfo.WhiteCastleQueenside = whiteCastleRights.Item2;
            posInfo.BlackCastleKingside = blackCastleRights.Item1;
            posInfo.BlackCastleQueenside = blackCastleRights.Item2;
            posInfo.HalfMoveClock = board.HalfMoveClock;
            posInfo.FullMoveClock = board.FullMoveClock;
            return posInfo;
        }
    }
}