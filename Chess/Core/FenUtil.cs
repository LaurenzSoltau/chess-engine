using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Chess;

namespace Chess
{
    public static class FenUtil
    {
        readonly static Dictionary<char, int> symbolToPiece = new() {
        {'P', Piece.WhitePawn},
        {'R', Piece.WhiteRook},
        {'N', Piece.WhiteKnight},
        {'B', Piece.WhiteBishop},
        {'Q', Piece.WhiteQueen},
        {'K', Piece.WhiteKing},
        {'p', Piece.BlackPawn},
        {'r', Piece.BlackRook},
        {'n', Piece.BlackKnight},
        {'b', Piece.BlackBishop},
        {'q', Piece.BlackQueen},
        {'k', Piece.BlackKing}
    };
        public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        //need to add Errorhandling
        public static PositionInfo PositionInfoFromFen(string fenString)
        {
            PositionInfo posInfo = new();
            string[] fenFields = new string[6];
            fenFields = fenString.Split(' ');

            // load pieces into squares
            string[] rowsInformation = fenFields[0].Split('/');
            for (int i = 0; i < rowsInformation.Length; i++)
            {
                string currentRowString = rowsInformation[i];
                //get start square of each row
                int row = 7 - i;
                int currentSquareIndex = Chess.Board.GetIndex(row, 0);
                foreach (char symbol in currentRowString)
                {
                    // if symbol is number, skip those square
                    if (symbol >= '1' && symbol <= '8')
                    {
                        currentSquareIndex += symbol - '0';
                    }
                    else
                    {
                        posInfo.Squares[currentSquareIndex] = symbolToPiece[symbol];
                        currentSquareIndex += 1;
                    }
                }
            }

            // NextToMove information
            posInfo.WhiteToMove = fenFields[1] == "w";

            // casteRights
            string castleRightsString = fenFields[2];
            foreach (char symbol in castleRightsString)
            {

                if (symbol == '-') break;
                if (symbol == 'K') posInfo.WhiteCastleKingside = true;
                if (symbol == 'Q') posInfo.WhiteCastleQueenside = true;
                if (symbol == 'k') posInfo.BlackCastleKingside = true;
                if (symbol == 'q') posInfo.BlackCastleQueenside = true;
            }

            //en Passant square
            string enPassantString = fenFields[3];
            if (enPassantString != "-")
            {
                posInfo.EnPassantSquare = Board.AlgebraicToIndex(enPassantString);
            }

            // halfMoveClock
            posInfo.HalfMoveClock = 0;
            posInfo.FullMoveClock = 0;
            if (fenFields.Length > 4)
            {
                string HalfMoveClockString = fenFields[4];
                posInfo.HalfMoveClock = int.Parse(HalfMoveClockString);
            }
            if (fenFields.Length > 5)
            {
                string FullMoveClockString = fenFields[5];
                posInfo.FullMoveClock = int.Parse(FullMoveClockString);
            }

            return posInfo;
        }
    }

}