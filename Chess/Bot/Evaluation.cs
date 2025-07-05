using System;
using System.Collections.Generic;

namespace Chess.Bot
{
    public class Evaluation
    {
        const int pawnValue = 100;
        const int knightValue = 300;
        const int bishopValue = 300;
        const int rookValue = 500;
        const int queenValue = 900;

        readonly Dictionary<int, int> pieceValues = new()
        {
            {Piece.WhitePawn, pawnValue},
            {Piece.WhiteKnight, knightValue},
            {Piece.WhiteBishop, bishopValue},
            {Piece.WhiteRook, rookValue},
            {Piece.WhiteQueen, queenValue}
        };

        Board board;


        public int Evaluate(Board board)
        {
            this.board = board;
            int whiteEval = 0;
            int blackEval = 0;
            int whiteMaterial = CountMaterial(Piece.White);
            int blackMaterial = CountMaterial(Piece.Black);

            whiteEval += whiteMaterial;
            blackEval += blackMaterial;


            int eval = whiteEval - blackEval;
            int perspective = board.ColourToMove == Piece.White ? 1 : -1;
            return eval * perspective;

        }


        int CountMaterial(int color)
        {
            int material = 0;
            foreach (int piece in board.Squares)
            {
                int whitePiece = Math.Abs(piece);
                if (piece == Piece.None || whitePiece == Piece.WhiteKing) continue;

                if (Piece.IsColor(piece, color))
                {
                    material += pieceValues[whitePiece];
                }
            }

            return material;
        }


    }
}