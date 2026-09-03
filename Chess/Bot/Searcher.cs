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
        MoveOrdering moveOrdering;

        readonly Board board;
        readonly Evaluation evaluation;
        readonly MoveGenerator moveGenerator;

        public Searcher(Board board)
        {
            this.board = board;
            moveGenerator = new();
            evaluation = new Evaluation();
            moveOrdering = new();
        }

        public void StartSearch(int maxDepth)
        {
            depthSearched = 0;
            var bestEval = negativeInfinity;
            bestMove = new();

            var sw = new Stopwatch();
            sw.Start();

            for (int searchDepth = 1; searchDepth <= maxDepth; searchDepth++)
            {
                int currentBestEval = negativeInfinity;
                Move currentBestMove = new();

                var legalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove);
                moveOrdering.OrderMoves(legalMoves, board);

                foreach (Move move in legalMoves)
                {
                    board.MakeMove(move, true);
                    int eval = -Search(negativeInfinity, positiveInfinity, searchDepth - 1, 1);
                    board.UnmakeMove(move, true);

                    if (eval > currentBestEval)
                    {
                        currentBestEval = eval;
                        currentBestMove = move;
                    }
                }

                bestEval = currentBestEval;
                bestMove = currentBestMove;
            }

            sw.Stop();

            botDiagnostics = new((int)sw.ElapsedMilliseconds, bestEval, depthSearched);
        }

        int Search(int alpha, int beta, int depth, int ply)
        {
            depthSearched = Math.Max(depthSearched, ply);

            if (board.IsRepetition())
            {
                return 0;
            }

            if (depth == 0)
            {
                return Quiesence(alpha, beta, 100, ply + 1);
            }
            var legalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove);
            moveOrdering.OrderMoves(legalMoves, board);

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

            if (board.IsRepetition())
            {
                return 0;
            }

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
            moveOrdering.OrderMoves(captureMoves, board);

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