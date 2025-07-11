using System.Diagnostics;
using System.Runtime.InteropServices;
using Godot;

namespace Chess
{
    public static class ZobristTest
    {
        static Board board;

        static void InitTest()
        {
            board = new();
        }
        public static void TestGenerateKey()
        {
            InitTest();
            board.LoadPosition(FenUtil.StartFen);
            ulong firstHashValue = Zobrist.GenerateKey(board);

            board.LoadPosition(FenUtil.StartFen);
            ulong secondHashValue = Zobrist.GenerateKey(board);

            Debug.Assert(firstHashValue == secondHashValue);

        }

        public static void TestKeyUpdate()
        {
            InitTest();
            board.LoadPosition("rnbqkbnr/ppppp1pp/8/5p2/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 1");
            Move move = new(28, 37, Piece.WhitePawn, Piece.BlackPawn);
            ulong firstHash = board.zobristKey;
            board.MakeMove(move);
            board.UnmakeMove(move);
            ulong secondHash = board.zobristKey;

            GD.Print(firstHash == secondHash);
            Debug.Assert(firstHash == secondHash);
        }

    }
}