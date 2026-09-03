using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace Chess.Testing
{
    public class Perft
    {

        public struct TestResults(int nodeCount, int elapsedTime, string resultString, int depth)
        {
            public int depth = depth;
            public int nodeCount = nodeCount;
            public int elapsedTime = elapsedTime;
            public string resultString = resultString;
        }

        public TestResults[] testResults;
        public int TestCount;
        public Test[] tests;
        public string fenString;
        public int depth;
        public bool divide;
        public Dictionary<string, int> PerftDivideResults;
        Board board;
        public TimeSpan lastTestElapsedTime;
        public TimeSpan totalElapsedTime;
        MoveGenerator moveGenerator;

        public Perft()
        {
            TestCount = TestUtil.GetTests().Length;
        }

        void Init()
        {
            tests = TestUtil.GetTests();
            testResults = new TestResults[tests.Length];
            board = new Board();
            moveGenerator = new();
        }

        public void RunSingleTestFen(string fen, int depth)
        {
            Init();
            board.LoadPosition(fen);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int numNodes = Search(depth);
            sw.Stop();
            GD.Print($"Nodes counted: {numNodes} In: {sw.ElapsedMilliseconds}");
        }

        public void RunSingleTest(int testIndex, bool divide)
        {
            Init();
            Test test = tests[testIndex];
            board.LoadPosition(test.FenString);
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int numNodes = 0;
            if (divide)
            {
                PerftDivideResults = new();
                numNodes = SearchDivide(test.Depth, test.Depth);
                WritePerftDivideResults();
            }
            else
            {
                numNodes = Search(test.Depth);
            }
            sw.Stop();
            string resultString;
            if (numNodes == test.ExpectedNodeCount)
            {
                resultString = "Passed! Count was correct!";
            }
            else
            {
                resultString = $"Failed! Counted: {numNodes} but expected {test.ExpectedNodeCount}";
            }
            testResults[testIndex] = new TestResults(numNodes, (int)sw.ElapsedMilliseconds, resultString, test.Depth);
        }

        public void RunFullSuite()
        {
            for (int i = 0; i < TestUtil.GetTests().Length; i++)
            {
                RunSingleTest(i + 1, false);
            }
        }
        public int SearchDivide(int startDepth, int currentDepth)
        {
            if (currentDepth == 0)
            {
                // Reached leaf node, count as 1
                return 1;
            }
            List<Move> moves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove); 
            int numLocalNodes = 0;

            foreach (var move in moves)
            {
                board.MakeMove(move, true);
                int count = SearchDivide(startDepth, currentDepth - 1);
                board.UnmakeMove(move, true);

                numLocalNodes += count;

                if (currentDepth == startDepth)
                {
                    PerftDivideResults.Add(TestUtil.GenerateMoveName(move), count);
                }
            }

            return numLocalNodes;
        }
        public int Search(int depth)
        {
            var pseudoLegalMoves = moveGenerator.GenerateLegalMoves(board, board.ColourToMove); 

            if (depth == 1)
            {
                return pseudoLegalMoves.Count;
            }

            int numLocalNodes = 0;

            for (int i = 0; i < pseudoLegalMoves.Count; i++)
            {
                Move move = pseudoLegalMoves[i];
                board.MakeMove(move, true);
                int numMovesForThisNode = Search(depth - 1);
                numLocalNodes += numMovesForThisNode;
                board.UnmakeMove(move, true);
            }
            return numLocalNodes;

        }

        void WritePerftDivideResults()
        {
            using StreamWriter writer = new(TestUtil.PERFTDIVIDERESULTSPATH);
            foreach (var kvp in PerftDivideResults)
            {
                writer.WriteLine($"{kvp.Key}: {kvp.Value}");

            }
        }

    }

}