using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                board.MakeMove(move, true);
                int eval = -Search(negativeInfinity, positiveInfinity, depth - 1, 1);
                board.UnmakeMove(move, true);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }
            sw.Stop();
            botDiagnostics = new((int)sw.ElapsedMilliseconds, bestEval, depthSearched);
        }

        int Search(int alpha, int beta, int depth, int ply)
        {
            depthSearched = Math.Max(depthSearched, ply);

            if (board.repitionTable.ContainsKey(board.zobristKey))
            {
                return 0;
            }


            if (depth == 0)
            {
                return Quiesence(alpha, beta, 8, ply + 1);
            }
            var legalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove);

            if (legalMoves.Count == 0)
            {
                return board.IsKingInCheck(board.ColourToMove) ? -immediateMateScore : 0;
            }

            int bestEval = -9999999;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move, true);
                int eval = -Search(-beta, -alpha, depth - 1, ply + 1);
                board.UnmakeMove(move, true);

                bestEval = Math.Max(eval, bestEval);
                alpha = Math.Max(alpha, eval);

                if (alpha >= beta) break;
            }
            return bestEval;
        }

        int Quiesence(int alpha, int beta, int qDepth, int ply)
        {
            depthSearched = Math.Max(depthSearched, ply);

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
            if (captureMoves.Count == 0) return alpha;

            foreach (var move in captureMoves)
            {
                board.MakeMove(move, true);
                int score = -Quiesence(-beta, -alpha, qDepth - 1, ply + 1);
                board.UnmakeMove(move, true);

                if (score >= beta) return beta;

                if (score > alpha) alpha = score;
            }
            return alpha;
        }
    }
}