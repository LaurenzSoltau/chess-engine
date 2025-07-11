using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Chess
{

    public static class Zobrist
    {
        public static readonly ulong[,,] pieceSquareNumbers = new ulong[2, 6, 64];
        public static readonly ulong[] castlingRightsNumbers = new ulong[16];
        public static readonly ulong[] enPassantFileNumbers = new ulong[8];
        public static readonly ulong blackToMove;
        static readonly string randomNumberFilePath = "randomNumbers.txt";
        static readonly Random rng = new();

        static Zobrist()
        {
            var randomNumbers = readRandomNumbers();
            //fill squarePieceTables;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    pieceSquareNumbers[0, i, j] = randomNumbers.Dequeue();
                    pieceSquareNumbers[1, i, j] = randomNumbers.Dequeue();
                }
            }

            for (int i = 0; i < 16; i++)
            {
                castlingRightsNumbers[i] = randomNumbers.Dequeue();
            }

            for (int i = 0; i < 8; i++)
            {
                enPassantFileNumbers[i] = randomNumbers.Dequeue();
            }

            blackToMove = randomNumbers.Dequeue();
        }

        static void writeRandomNumbers()
        {
            int numOfRandomNumbers = 793;
            string[] numberStrings = new string[numOfRandomNumbers];

            byte[] buffer = new byte[8];

            for (int i = 0; i < numOfRandomNumbers; i++)
            {
                rng.NextBytes(buffer);
                ulong randomNumber = BitConverter.ToUInt64(buffer, 0);
                numberStrings[i] = randomNumber.ToString();
            }
            File.WriteAllText(randomNumberFilePath, string.Join(",", numberStrings));
        }

        static Queue<ulong> readRandomNumbers()
        {
            Queue<ulong> randomNumberQueue = new();

            if (!File.Exists(randomNumberFilePath))
            {
                writeRandomNumbers();
            }

            string randomNumberString = File.ReadAllText(randomNumberFilePath);
            string[] numberStrings = randomNumberString.Split(",");

            foreach (string numberString in numberStrings)
            {
                randomNumberQueue.Enqueue(UInt64.Parse(numberString));
            }
            return randomNumberQueue;
        }

        public static ulong GenerateKey(Board board)
        {
            ulong key = 0;

            // xor piece rng nums for every piece on the board
            for (int i = 0; i < board.Squares.Length; i++)
            {
                int piece = board.Squares[i];
                if (piece == Piece.None) continue;
                int colorIndex = piece > 0 ? 0 : 1;
                int pieceIndex = Math.Abs(piece) - 1;
                key ^= pieceSquareNumbers[colorIndex, pieceIndex, i];
            }

            // xor rng num depending on castling right 4 bits
            key ^= castlingRightsNumbers[board.CastlingRights];

            // xor rng num for enpassant square
            if (board.EnPassantSquare != -1)
            {
                int enPassantFile = board.EnPassantSquare % 8;
                key ^= enPassantFileNumbers[enPassantFile];
            }

            if (board.ColourToMove == Piece.Black)
            {
                key ^= blackToMove;
            }
            return key;
        }

    }
}