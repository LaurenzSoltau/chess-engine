using System.Reflection.Metadata.Ecma335;

namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Security.Principal;
    using Godot;

    public class Board
    {
        public const int BoardSize = 64;

        public int[] Squares;
        public int[] kings;

        public int EnPassantSquare;
        public int HalfMoveClock;
        public int FullMoveClock;
        public struct CastlingRights
        {
            public bool WhiteKingside;
            public bool WhiteQueenside;
            public bool BlackKingside;
            public bool BlackQueenside;
        }

        public CastlingRights Castling;

        public struct UnmakeMoveInformation
        {
            public int oldEnPassantSquare;
            public CastlingRights oldCastlingRights;
            public int oldHalfMoveClock;
            public int oldFullMoveClock;

        }

        public Stack<UnmakeMoveInformation> unmakeMoveInformation = new();

        public int ColourToMove;

        MoveGenerator moveGenerator;

        public Board()
        {
            moveGenerator = new();
        }

        public bool IsFiftyMoveRuleReached()
        {
            return HalfMoveClock >= 100;
        }

        public bool IsInsufficientMaterial()
        {
            int whiteBishops = 0;
            int blackBishops = 0;
            int whiteKnights = 0;
            int blackKnights = 0;
            int whiteRooksOrQueens = 0;
            int blackRooksOrQueens = 0;
            int whitePawns = 0;
            int blackPawns = 0;

            List<int> whiteBishopSquares = [];
            List<int> blackBishopSquares = [];

            for (int i = 0; i < Squares.Length; i++)
            {
                int pieceCode = Squares[i];
                if (pieceCode == Piece.None) continue;

                int whitePiece = Math.Abs(pieceCode);

                switch (whitePiece)
                {
                    case Piece.WhitePawn:
                        if (Piece.IsWhite(pieceCode)) whitePawns++; else blackPawns++;
                        break;

                    case Piece.WhiteRook:

                    case Piece.WhiteQueen:
                        if (Piece.IsWhite(pieceCode)) whiteRooksOrQueens++; else blackRooksOrQueens++;
                        break;

                    case Piece.WhiteBishop:
                        if (Piece.IsWhite(pieceCode))
                        {
                            whiteBishops++;
                            whiteBishopSquares.Add(i);
                        }
                        else
                        {
                            blackBishops++;
                            blackBishopSquares.Add(i);
                        }
                        break;

                    case Piece.WhiteKnight:
                        if (Piece.IsWhite(pieceCode)) whiteKnights++; else blackKnights++;
                        break;
                }
            }

            // If any pawns or queens or rooks remain → material sufficient
            if (whitePawns > 0 || blackPawns > 0 || whiteRooksOrQueens > 0 || blackRooksOrQueens > 0)
                return false;

            // More than one minor piece per side → sufficient material
            if (whiteBishops + whiteKnights > 1 || blackBishops + blackKnights > 1)
                return false;

            // If no minor pieces and no pawns → king vs king
            if (whiteBishops == 0 && whiteKnights == 0 && blackBishops == 0 && blackKnights == 0)
                return true;

            // If one side has only one bishop or knight and other side king only → insufficient
            if ((whiteBishops + whiteKnights == 1 && blackBishops + blackKnights == 0) ||
                (blackBishops + blackKnights == 1 && whiteBishops + whiteKnights == 0))
                return true;

            // add different color Bishops logic
            if (whiteBishops == 1 && blackBishops == 1 &&
                whiteKnights + blackKnights == 0 &&
                whiteRooksOrQueens + blackRooksOrQueens == 0 &&
                whitePawns + blackPawns == 0)
            {
                return true;

            }

            if (whiteKnights + blackKnights == 0 &&
    whiteRooksOrQueens + blackRooksOrQueens == 0 &&
    whitePawns + blackPawns == 0 &&
    (whiteBishops + blackBishops > 0))
            {
                if (AllBishopsOnSameColor(whiteBishopSquares, blackBishopSquares))
                    return true; // alle Läufer auf gleicher Farbe → Remis
            }

            return false; // otherwise sufficient material
        }

        bool AllBishopsOnSameColor(List<int> squaresA, List<int> squaresB)
        {
            List<int> allSquares = [.. squaresA, .. squaresB];

            if (allSquares.Count == 0)
                return false;

            bool firstColor = IsLightSquare(allSquares[0]);

            foreach (int square in allSquares)
            {
                if (IsLightSquare(square) != firstColor)
                    return false;
            }

            return true;
        }

        bool IsLightSquare(int squareIndex)
        {
            int rank = squareIndex / 8;
            int file = squareIndex % 8;
            return (rank + file) % 2 == 0;
        }
        public Board Clone()
        {
            Board clone = new Board();
            clone.Init();
            for (int i = 0; i < Squares.Length; i++)
            {
                clone.Squares[i] = Squares[i];
            }
            clone.ColourToMove = this.ColourToMove;
            clone.EnPassantSquare = this.EnPassantSquare;
            clone.Castling = this.Castling;

            return clone;
        }

        void Init()
        {
            Squares = new int[BoardSize];
            EnPassantSquare = -1;
            kings = new int[2];
        }

        public void LoadPosition(String fenString)
        {
            PositionInfo posInfo = FenUtil.PositionInfoFromFen(fenString);
            Init();
            Squares = posInfo.Squares;
            EnPassantSquare = posInfo.EnPassantSquare;
            ColourToMove = posInfo.WhiteToMove ? Piece.White : Piece.Black;
            Castling.WhiteKingside = posInfo.WhiteCastleKingside;
            Castling.WhiteQueenside = posInfo.WhiteCastleQueenside;
            Castling.BlackKingside = posInfo.BlackCastleKingside;
            Castling.BlackQueenside = posInfo.BlackCastleQueenside;
            HalfMoveClock = posInfo.HalfMoveClock;
            FullMoveClock = posInfo.FullMoveClock;
        }

        void UpdateEnPassantSquare(Move move)
        {
            EnPassantSquare = -1;
            int fromRow = GetCoords(move.From).row;
            int toRow = GetCoords(move.To).row;
            if (Math.Abs(fromRow - toRow) == 2 && Math.Abs(move.MovingPiece) == Piece.WhitePawn)
            {
                EnPassantSquare = (move.From + move.To) / 2;
            }
        }

        void UpdateHalfMoveClock(Move move)
        {
            if (move.CapturedPiece != Piece.None || Math.Abs(move.MovingPiece) == Piece.WhitePawn)
            {
                HalfMoveClock = 0;
            }
            else
            {
                HalfMoveClock++;
            }
        }

        void UpdateFullMoveClock(Move move)
        {
            if (ColourToMove == Piece.Black)
            {
                FullMoveClock++;
            }
        }
        public void MakeMove(Move move)
        {
            // updating enPassant Square
            UnmakeMoveInformation safeInfo = new()
            {
                oldCastlingRights = Castling,
                oldEnPassantSquare = EnPassantSquare,
                oldHalfMoveClock = HalfMoveClock,
                oldFullMoveClock = FullMoveClock
            };
            unmakeMoveInformation.Push(safeInfo);

            UpdateEnPassantSquare(move);

            //castling Rights
            UpdateCastleRights(move);

            //handle en passant
            if (move.IsEnPassant)
            {
                int captureSquare = move.To + (ColourToMove == Piece.White ? -8 : 8);
                Squares[captureSquare] = Piece.None;
            }

            // Handle castling
            if (move.IsCastling)
            {
                // Kingside or queenside castling
                if (move.To == move.From + 2)
                {
                    // Kingside: move rook
                    Squares[move.From + 3] = Piece.None;
                    Squares[move.From + 1] = Piece.IsWhite(move.MovingPiece)
                        ? Piece.WhiteRook
                        : Piece.BlackRook;
                }
                else if (move.To == move.From - 2)
                {
                    // Queenside: move rook
                    Squares[move.From - 4] = 0;
                    Squares[move.From - 1] = Piece.IsWhite(move.MovingPiece)
                        ? Piece.WhiteRook
                        : Piece.BlackRook;
                }
            }

            if (move.PromotionPiece != Piece.None)
            {
                Squares[move.To] = move.PromotionPiece;
            }
            else
            {
                Squares[move.To] = move.MovingPiece;
            }

            Squares[move.From] = Piece.None;

            UpdateHalfMoveClock(move);
            UpdateFullMoveClock(move);
            ColourToMove = ColourToMove == Piece.White ? Piece.Black : Piece.White;
        }

        public void UnmakeMove(Move move)
        {
            UnmakeMoveInformation savedInfo = unmakeMoveInformation.Pop();
            ColourToMove = -ColourToMove;

            EnPassantSquare = savedInfo.oldEnPassantSquare;

            Castling = savedInfo.oldCastlingRights;
            HalfMoveClock = savedInfo.oldHalfMoveClock;
            FullMoveClock = savedInfo.oldFullMoveClock;

            Squares[move.To] = Piece.None;

            if (move.PromotionPiece != Piece.None)
            {
                Squares[move.From] = move.MovingPiece;
            }
            else
            {
                Squares[move.From] = move.MovingPiece;
            }

            if (move.CapturedPiece != Piece.None)
            {
                if (move.IsEnPassant)
                {
                    int captureSquare = move.To + (Piece.IsWhite(move.MovingPiece) ? -8 : 8);
                    Squares[captureSquare] = move.CapturedPiece;
                }
                else
                {
                    Squares[move.To] = move.CapturedPiece;
                }
            }

            // Rückgängig machen von Rochade
            if (move.IsCastling)
            {
                if (move.To == move.From + 2) // Kingside
                {
                    // Setze Turm zurück
                    Squares[move.From + 3] = Piece.IsWhite(move.MovingPiece) ? Piece.WhiteRook : Piece.BlackRook;
                    Squares[move.From + 1] = Piece.None;
                }
                else if (move.To == move.From - 2) // Queenside
                {
                    Squares[move.From - 4] = Piece.IsWhite(move.MovingPiece) ? Piece.WhiteRook : Piece.BlackRook;
                    Squares[move.From - 1] = Piece.None;
                }
            }

        }


        void UpdateCastleRights(Move move)
        {
            if (move.MovingPiece == Piece.WhiteKing)
            {
                Castling.WhiteKingside = false;
                Castling.WhiteQueenside = false;
            }
            else if (move.MovingPiece == Piece.BlackKing)
            {
                Castling.BlackKingside = false;
                Castling.BlackQueenside = false;
            }
            if (move.MovingPiece == Piece.WhiteRook)
            {
                if (move.From == 0) Castling.WhiteQueenside = false;
                if (move.From == 7) Castling.WhiteKingside = false;
            }
            else if (move.MovingPiece == Piece.BlackRook)
            {
                if (move.From == 56) Castling.BlackQueenside = false;
                if (move.From == 63) Castling.BlackKingside = false;
            }
            if (move.CapturedPiece == Piece.WhiteRook)
            {
                if (move.To == 0) Castling.WhiteQueenside = false;
                else if (move.To == 7) Castling.WhiteKingside = false;
            }
            else if (move.CapturedPiece == Piece.BlackRook)
            {
                if (move.To == 56) Castling.BlackQueenside = false;
                else if (move.To == 63) Castling.BlackKingside = false;
            }
        }

        public int GetPieceAt(int index)
        {
            if (index < 0 || index >= BoardSize)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Squares[index];
        }

        public static int GetIndex(int row, int col) => row * 8 + col;

        public static (int row, int col) GetCoords(int index)
        {
            if (!IsValidIndex(index))
                throw new ArgumentOutOfRangeException(nameof(index));
            return (index / 8, index % 8);
        }

        public static bool IsWhite(int code) => code > 0;
        public static bool IsBlack(int code) => code < 0;

        private static bool IsValidIndex(int i) => i >= 0 && i < BoardSize;

        public static string IndexToSquareName(int index)
        {
            int rank = GetCoords(index).row;
            int file = GetCoords(index).col;

            char rankChar = (char)('1' + rank);
            char fileChar = (char)('a' + file);

            return fileChar.ToString() + rankChar.ToString();

        }
        public static int AlgebraicToIndex(string square)
        {
            char file = square[0];
            char rank = square[1];

            int fileNumber = file - 'a';
            int rankNumber = rank - '1';

            return rankNumber * BoardSize + fileNumber;

        }

        public (bool Queenside, bool Kingside) HasColorCastleRight(int color)
        {
            if (color == Piece.White)
            {
                return (Castling.WhiteQueenside, Castling.WhiteKingside);
            }
            else
            {
                return (Castling.BlackQueenside, Castling.BlackKingside);
            }
        }

        public List<Move> GenerateLegalMoves()
        {
            List<Move> legalMoves = [];

            var pseudoMoves = moveGenerator.GeneratePseudoLegalMoves(this, ColourToMove);

            for (int i = 0; i < pseudoMoves.Count; i++)
            {
                Move move = pseudoMoves[i];
                MakeMove(move);
                if (!IsKingInCheck(-ColourToMove))
                {
                    legalMoves.Add(move);
                }
                UnmakeMove(move);
            }
            return legalMoves;
        }

        public bool IsKingInCheck(int color)
        {
            int kingSquare = -1;
            for (int i = 0; i < Squares.Length; i++)
            {
                int piece = Squares[i];
                if (piece == (color == Piece.White ? Piece.WhiteKing : Piece.BlackKing))
                {
                    kingSquare = i;
                    break;
                }
            }
            MoveGenerator gen = new();
            return gen.IsSquareAttacked(kingSquare, -color, this);
        }

    }
}