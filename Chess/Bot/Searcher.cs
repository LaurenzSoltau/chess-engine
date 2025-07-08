using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Godot;

namespace Chess.Bot
{
    public class Searcher
    {

        const int immediateMateScore = 100000;
        const int positiveInfinity = 9999999;
        const int negativeInfinity = -positiveInfinity;
        public Move bestMove;
        public BotDiagnostics botDiagnostics;
        int depthSearched;

        readonly Board board;
        readonly Evaluation evaluation;
        readonly MoveGenerator moveGenerator;

        public Searcher(Board board)
        {
            this.board = board;
            moveGenerator = new();
            evaluation = new Evaluation();
        }

        public void StartSearch(int depth)
        {
            depthSearched = 0;
            var legalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove);
            var bestEval = negativeInfinity;
            bestMove = new();
            var sw = new Stopwatch();
            sw.Start();
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = -Search(negativeInfinity, positiveInfinity, depth - 1);
                board.UnmakeMove(move);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }
            sw.Stop();
            botDiagnostics = new((int)sw.ElapsedMilliseconds, bestEval, depthSearched);
        }

        int Search(int alpha, int beta, int depth)
        {
            if (depth == 0)
            {
                return Quiesence(alpha, beta, 5);
            }
            var legalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove);

            if (legalMoves.Count == 0)
            {
                return board.IsKingInCheck(board.ColourToMove) ? -immediateMateScore : 0;
            }

            int bestEval = -9999999;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = -Search(-beta, -alpha, depth - 1);
                board.UnmakeMove(move);

                bestEval = Math.Max(eval, bestEval);
                alpha = Math.Max(alpha, eval);

                if (alpha >= beta) break;
            }
            return bestEval;
        }

        int Quiesence(int alpha, int beta, int qDepth)
        {
            depthSearched++;
            if (qDepth == 0)
            {
                return evaluation.Evaluate(board);
            }

            int standPat = evaluation.Evaluate(board);

            if (standPat >= beta)
            {
                return beta;
            }

            if (alpha < standPat)
            {
                alpha = standPat;
            }

            var captureMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove, true);

            foreach (var move in captureMoves)
            {
                board.MakeMove(move);
                int score = -Quiesence(-beta, -alpha, qDepth - 1);
                board.UnmakeMove(move);

                if (score >= beta) return beta;

                if (score > alpha) alpha = score;
            }
            return alpha;
        }
    }
}