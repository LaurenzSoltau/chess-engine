using System;
using System.Collections.Generic;
using Godot;

namespace Chess.Bot
{
    public class Evaluation
    {
        public const int pawnValue = 100;
        public const int knightValue = 320;
        public const int bishopValue = 330;
        public const int rookValue = 500;
        public const int queenValue = 900;

        const int knightWeight = 1; const int bishopWeight = 1;
        const int rookWeight = 2;
        const int queenWeight = 4;
        const int totalPhase = knightWeight * 4 + bishopWeight * 4 + rookWeight * 4 + queenWeight * 2;


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
            int phase = totalPhase;

            //calculate phase for interpolation
            phase -= board.GetPieceList(Piece.WhiteKnight).Count * knightWeight;
            phase -= board.GetPieceList(Piece.BlackKnight).Count * knightWeight;
            phase -= board.GetPieceList(Piece.WhiteBishop).Count * bishopWeight;
            phase -= board.GetPieceList(Piece.BlackBishop).Count * bishopWeight;
            phase -= board.GetPieceList(Piece.WhiteRook).Count * rookWeight;
            phase -= board.GetPieceList(Piece.BlackRook).Count * rookWeight;
            phase -= board.GetPieceList(Piece.WhiteQueen).Count * queenWeight;
            phase -= board.GetPieceList(Piece.BlackQueen).Count * queenWeight;


            int whiteEval = 0;
            int blackEval = 0;
            int whiteMaterial = CountMaterial(Piece.White);
            int blackMaterial = CountMaterial(Piece.Black);

            whiteEval += whiteMaterial;
            blackEval += blackMaterial;

            whiteEval += EvaluatePieceSquareTables(Piece.White, phase);
            blackEval += EvaluatePieceSquareTables(Piece.Black, phase);


            int eval = whiteEval - blackEval;
            int perspective = board.ColourToMove == Piece.White ? 1 : -1;
            return eval * perspective;

        }


        int CountMaterial(int color)
        {
            int material = 0;
            bool isWhite = color == Piece.White;
            int pawnPiece = isWhite ? Piece.WhitePawn : Piece.BlackPawn;
            int rookPiece = isWhite ? Piece.WhiteRook : Piece.BlackRook;
            int knightPiece = isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
            int bishopPiece = isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
            int queenPiece = isWhite ? Piece.WhiteQueen : Piece.BlackQueen;

            material += board.GetPieceList(pawnPiece).Count * pieceValues[Math.Abs(pawnPiece)];
            material += board.GetPieceList(rookPiece).Count * pieceValues[Math.Abs(rookPiece)];
            material += board.GetPieceList(knightPiece).Count * pieceValues[Math.Abs(knightPiece)];
            material += board.GetPieceList(bishopPiece).Count * pieceValues[Math.Abs(bishopPiece)];
            material += board.GetPieceList(queenPiece).Count * pieceValues[Math.Abs(queenPiece)];

            return material;
        }

        int EvaluatePieceSquareTables(int color, int phase)
        {
            int eval = 0;
            bool isWhite = color == Piece.White;
            int pawnPiece = isWhite ? Piece.WhitePawn : Piece.BlackPawn;
            int rookPiece = isWhite ? Piece.WhiteRook : Piece.BlackRook;
            int knightPiece = isWhite ? Piece.WhiteKnight : Piece.BlackKnight;
            int bishopPiece = isWhite ? Piece.WhiteBishop : Piece.BlackBishop;
            int queenPiece = isWhite ? Piece.WhiteQueen : Piece.BlackQueen;
            eval += EvaluatePieceSquareTable(PieceSquareTable.pawns, board.GetPieceList(pawnPiece), color, phase);
            eval += EvaluatePieceSquareTable(PieceSquareTable.knights, board.GetPieceList(knightPiece), color, phase);
            eval += EvaluatePieceSquareTable(PieceSquareTable.bishops, board.GetPieceList(bishopPiece), color, phase);
            eval += EvaluatePieceSquareTable(PieceSquareTable.rooks, board.GetPieceList(rookPiece), color, phase);
            eval += EvaluatePieceSquareTable(PieceSquareTable.queens, board.GetPieceList(queenPiece), color, phase);
            // extra for king needs to be fixed
            int kingMiddle = PieceSquareTable.Read(PieceSquareTable.kings, false, board.kings[isWhite ? 0 : 1], isWhite);
            int kingEnd = PieceSquareTable.Read(PieceSquareTable.kings, true, board.kings[isWhite ? 0 : 1], isWhite);
            eval += (kingMiddle * phase + kingEnd * (totalPhase - phase) + totalPhase / 2) / totalPhase;

            return eval;
        }

        int EvaluatePieceSquareTable(int[][] table, PieceList pieceList, int color, int phase)
        {
            bool isEndgame = true;
            bool isWhite = color == Piece.White;
            int middleValue = 0;
            int endValue = 0;

            for (int i = 0; i < pieceList.Count; i++)
            {
                int square = pieceList.occupiedSquares[i];
                middleValue += PieceSquareTable.Read(table, !isEndgame, square, isWhite);
                endValue += PieceSquareTable.Read(table, isEndgame, square, isWhite);
            }
            int score = (middleValue * phase + endValue * (totalPhase - phase) + totalPhase / 2) / totalPhase;
            return score;
        }
    }
}