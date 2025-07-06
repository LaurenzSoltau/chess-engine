using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Godot;

namespace Chess.Bot
{
    public class Searcher
    {
        public Move bestMove;
        int bestEval;

        Board board;
        Evaluation evaluation;
        int rootDepth;


        public Searcher(Board board)
        {
            this.board = board;
            evaluation = new Evaluation();
        }

        public void StartSearch(int depth)
        {
            var legalMoves = board.GenerateLegalMoves();
            var bestEval = -99999;

            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = -Search(-999999, 999999 ,depth - 1);
                board.UnmakeMove(move);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }
        }

        int Search(int alpha, int beta, int depth)
        {
            if (depth == 0)
            {
                return evaluation.Evaluate(board);
            }
            var legalMoves = board.GenerateLegalMoves();

            if (legalMoves.Count == 0)
            {
                return board.IsKingInCheck(board.ColourToMove) ? -99999 : 0;
            }

            int bestEval = -9999999;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = -Search(-alpha, -beta, depth - 1);
                board.UnmakeMove(move);

                bestEval = Math.Max(eval, bestEval);
                alpha = Math.Max(alpha, eval);

                if (alpha >= beta) break;
            }


            return bestEval;
        }
    }
}