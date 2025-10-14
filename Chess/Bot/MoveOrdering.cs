using System;
using System.Collections.Generic;

namespace Chess.Bot
{
    public class MoveOrdering
    {
        readonly Evaluation evaluation;

        public MoveOrdering()
        {
            evaluation = new();
        }

        public void OrderMoves(List<Move> moves, Board board)
        {
            var scoredMoves = new List<(Move move, int score)>(moves.Count);
            foreach(var move in moves)
                scoredMoves.Add((move, scoreMove(move, board)));

            scoredMoves.Sort((a, b) => b.score.CompareTo(a.score));
            moves.Clear();

            foreach (var (move, _) in scoredMoves)
                moves.Add(move);
        }

        int scoreMove(Move move, Board board)
        {
            int capturedPiece = move.CapturedPiece;
            int movingPiece = move.MovingPiece;
            int score = 0;
            if (move.CapturedPiece != Piece.None)
            {
                score += 10000 + 10 * GetPieceValue(capturedPiece) - GetPieceValue(movingPiece);
            }

            if (Math.Abs(move.MovingPiece) == Piece.WhitePawn && move.PromotionPiece != Piece.None)
            {
                score += GetPieceValue(move.PromotionPiece);
            }

            return score;
        }

        int GetPieceValue(int piece)
        {
            return Math.Abs(piece) switch
            {
                Piece.WhitePawn => Evaluation.pawnValue,
                Piece.WhiteBishop => Evaluation.bishopValue,
                Piece.WhiteKnight => Evaluation.knightValue,
                Piece.WhiteRook => Evaluation.rookValue,
                Piece.WhiteQueen => Evaluation.queenValue,
                _ => 0,
            };
        }
    }
}