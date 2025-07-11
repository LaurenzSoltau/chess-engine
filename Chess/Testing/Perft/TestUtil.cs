using System;
using System.IO;

namespace Chess.Testing
{
    public static class TestUtil
    {
        public const string SUITEPATH = "./Chess/Testing/Perft/TestSuite.txt";
        public const string PERFTDIVIDERESULTSPATH = "./Chess/Testing/Perft/PerftDivideResults.txt";

        public static Test[] GetTests()
        {
            string[] testLines = File.ReadAllLines(SUITEPATH);
            Test[] tests = new Test[testLines.Length];

            for (int i = 0; i < testLines.Length; i++)
            {
                Test test = createTest(testLines[i]);
                tests[i] = test;
            }
            return tests;
        }

        static Test createTest(string testLine)
        {
            string[] testFields = testLine.Split(",");
            string fen = testFields[2];
            int depth = int.Parse(testFields[0]);
            long expectedNodeCount = int.Parse(testFields[1]);
            Test newTest = new(fen, depth, expectedNodeCount);

            return newTest;
        }

        public static string GenerateMoveName(Move move)
        {
            string fromSquare = BoardRepresentation.IndexToAlgebraic(move.From);
            string toSquare = BoardRepresentation.IndexToAlgebraic(move.To);
            string promotion = "";
            int WhitePromotionPiece = Math.Abs(move.PromotionPiece);

            switch (WhitePromotionPiece)
            {
                case Piece.WhiteRook:
                    promotion += "r";
                    break;

                case Piece.WhiteKnight:
                    promotion += "n";
                    break;

                case Piece.WhiteBishop:
                    promotion += "b";
                    break;

                case Piece.WhiteQueen:
                    promotion += "q";
                    break;
            }

            return fromSquare + toSquare + promotion;
        }
    }
}