using System.Reflection.Metadata.Ecma335;

namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class Board
    {
        public const int BoardSize = 64;

        public int[] Squares;

        // white is index 0, black is index 1
        public int[] kings;
        public PieceList[] pawns;
        public PieceList[] bishops;
        public PieceList[] knights;
        public PieceList[] rooks;
        public PieceList[] queens;
        public PieceList[][] AllPieceLists;

        public int EnPassantSquare;
        public int HalfMoveClock;
        public int FullMoveClock;

        const int WhiteKingsideMask = 1 << 0;
        const int WhiteQueensideMask = 1 << 1;
        const int BlackKingsideMask = 1 << 2;
        const int BlackQueensideMask = 1 << 3;
        public int CastlingRights;


        public struct UnmakeMoveInformation
        {
            public int oldEnPassantSquare;
            public int oldCastlingRights;
            public int oldHalfMoveClock;
            public int oldFullMoveClock;

        }

        public UnmakeMoveInformation[] unmakeMoveInformationTest = new UnmakeMoveInformation[1000];
        public int plyCount;
         
        public int ColourToMove;

        readonly MoveGenerator moveGenerator;

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

        void Init()
        {
            plyCount = 0;
            CastlingRights = 0;
            Squares = new int[BoardSize];
            EnPassantSquare = -1;
            kings = new int[2];
            pawns = [new(8), new(8)];
            bishops = [new(10), new(10)];
            knights = [new(10), new(10)];
            rooks = [new(10), new(10)];
            queens = [new(9), new(9)];
            AllPieceLists = [
                queens,
                pawns,
                rooks,
                knights,
                bishops    
            ];
        }

        public PieceList GetPieceList(int piece)
        {
            int colorIndex = piece < 0 ? 1 : 0;
            int pieceIndex = Math.Abs(piece) - 2;
            return AllPieceLists[pieceIndex][colorIndex];
        }


        public void LoadPosition(String fenString)
        {
            PositionInfo posInfo = FenUtil.PositionInfoFromFen(fenString);
            Init();
            EnPassantSquare = posInfo.EnPassantSquare;
            ColourToMove = posInfo.WhiteToMove ? Piece.White : Piece.Black;

            //load squares and piecelists
            for (int i = 0; i < posInfo.Squares.Length; i++)
            {
                int piece = posInfo.Squares[i];
                if (piece == Piece.None) continue;
                Squares[i] = piece;

                if (Math.Abs(piece) != Piece.WhiteKing)
                {
                    PieceList pieceList = GetPieceList(piece);
                    pieceList.AddPiece(i);
                }
                else
                {
                    kings[piece > 0 ? 0 : 1] = i;
                }
            }

            //Load Castle Rights from Posinfo
            if (posInfo.WhiteCastleKingside) CastlingRights |= WhiteKingsideMask;
            if (posInfo.WhiteCastleQueenside) CastlingRights |= WhiteQueensideMask;
            if (posInfo.BlackCastleKingside) CastlingRights |= BlackKingsideMask;
            if (posInfo.BlackCastleQueenside) CastlingRights |= BlackQueensideMask;
            HalfMoveClock = posInfo.HalfMoveClock;
            FullMoveClock = posInfo.FullMoveClock;
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
            // Store moveInfo for Unmake move
            UnmakeMoveInformation safeInfo = new()
            {
                oldCastlingRights = CastlingRights,
                oldEnPassantSquare = EnPassantSquare,
                oldHalfMoveClock = HalfMoveClock,
                oldFullMoveClock = FullMoveClock
            };
            unmakeMoveInformationTest[plyCount] = safeInfo;
            plyCount++;

            
            int fromSquare = move.From;
            int toSquare = move.To;
            int fromRow = fromSquare / 8;
            int toRow = toSquare / 8;

            int movingPiece = move.MovingPiece;
            int capturedPiece = move.CapturedPiece;

            bool isCastling = move.IsCastling;
            bool isEnPassant = move.IsEnPassant;
            bool captureMove = move.CapturedPiece != Piece.None;



            //handle en passant
                if (move.IsEnPassant)
                {
                    int captureSquare = toSquare + (ColourToMove == Piece.White ? -8 : 8);
                    Squares[captureSquare] = Piece.None;
                }

            // Handle castling
            if (move.IsCastling)
            {
                // Kingside or queenside castling
                if (toSquare == fromSquare + 2)
                {
                    // Kingside: move rook
                    Squares[fromSquare + 3] = Piece.None;
                    Squares[fromSquare + 1] = Piece.IsWhite(movingPiece)
                        ? Piece.WhiteRook
                        : Piece.BlackRook;
                }
                else if (toSquare == fromSquare - 2)
                {
                    // Queenside: move rook
                    Squares[fromSquare - 4] = 0;
                    Squares[fromSquare - 1] = Piece.IsWhite(movingPiece)
                        ? Piece.WhiteRook
                        : Piece.BlackRook;
                }
            }

            if (move.PromotionPiece != Piece.None)
            {
                Squares[toSquare] = move.PromotionPiece;
            }
            else
            {
                Squares[toSquare] = movingPiece;
            }

            Squares[fromSquare] = Piece.None;


            // Update Gamestate
            EnPassantSquare = (Math.Abs(fromRow - toRow) == 2 && Math.Abs(movingPiece) == Piece.WhitePawn) ? (fromSquare + toSquare) / 2 : -1;
            UpdateCastleRights(move);
            HalfMoveClock = (Math.Abs(movingPiece) == Piece.WhitePawn || capturedPiece != Piece.None) ? 0 : HalfMoveClock++;
            FullMoveClock = ColourToMove == Piece.Black ? FullMoveClock++ : FullMoveClock; 
            ColourToMove = ColourToMove == Piece.White ? Piece.Black : Piece.White;
        }




        public void UnmakeMove(Move move)
        {
            int fromSquare = move.From;
            int toSquare = move.To;
            int movingPiece = move.MovingPiece;
            int capturedPiece = move.CapturedPiece;

            plyCount--;
            UnmakeMoveInformation savedInfo = unmakeMoveInformationTest[plyCount];
            ColourToMove = -ColourToMove;

            EnPassantSquare = savedInfo.oldEnPassantSquare;

            CastlingRights = savedInfo.oldCastlingRights;
            HalfMoveClock = savedInfo.oldHalfMoveClock;
            FullMoveClock = savedInfo.oldFullMoveClock;

            Squares[toSquare] = Piece.None;

            if (move.PromotionPiece != Piece.None)
            {
                Squares[fromSquare] = movingPiece;
            }
            else
            {
                Squares[fromSquare] = movingPiece;
            }

            if (capturedPiece != Piece.None)
            {
                if (move.IsEnPassant)
                {
                    int captureSquare = toSquare + (Piece.IsWhite(movingPiece) ? -8 : 8);
                    Squares[captureSquare] = capturedPiece;
                }
                else
                {
                    Squares[toSquare] = capturedPiece;
                }
            }

            // Rückgängig machen von Rochade
            if (move.IsCastling)
            {
                if (toSquare == fromSquare + 2) // Kingside
                {
                    // Setze Turm zurück
                    Squares[fromSquare + 3] = Piece.IsWhite(movingPiece) ? Piece.WhiteRook : Piece.BlackRook;
                    Squares[fromSquare + 1] = Piece.None;
                }
                else if (move.To == move.From - 2) // Queenside
                {
                    Squares[fromSquare - 4] = Piece.IsWhite(movingPiece) ? Piece.WhiteRook : Piece.BlackRook;
                    Squares[fromSquare - 1] = Piece.None;
                }
            }

        }


        void UpdateCastleRights(Move move)
        {
            int fromSquare = move.From;
            int toSquare = move.To;
            int movingPiece = move.MovingPiece;
            int capturedPiece = move.CapturedPiece;

            if (movingPiece == Piece.WhiteKing)
            {
                CastlingRights &= ~(WhiteKingsideMask | WhiteQueensideMask);
            }
            else if (movingPiece == Piece.BlackKing)
            {
                CastlingRights &= ~(BlackKingsideMask | BlackQueensideMask);
            }

            if ((movingPiece == Piece.WhiteRook && (fromSquare == 0 || fromSquare == 7)) ||
                (capturedPiece == Piece.WhiteRook && (toSquare == 0 || toSquare == 7)))
            {
                if (fromSquare == 0 || toSquare == 0) CastlingRights &= ~WhiteQueensideMask;
                if (fromSquare == 7 || toSquare == 7) CastlingRights &= ~WhiteKingsideMask;
            }

            if ((movingPiece == Piece.BlackRook && (fromSquare == 56 || fromSquare == 63)) ||
                (capturedPiece == Piece.BlackRook && (toSquare == 56 || toSquare == 63)))
            {
                if (fromSquare == 56 || toSquare == 56) CastlingRights &= ~BlackQueensideMask;
                if (fromSquare == 63 || toSquare == 63) CastlingRights &= ~BlackKingsideMask;
            }
        }

        public static int GetIndex(int row, int col) => row * 8 + col;

        public static (int row, int col) GetCoords(int index)
        {
            if (!IsValidIndex(index))
                throw new ArgumentOutOfRangeException(nameof(index));
            return (index / 8, index % 8);
        }

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
                return ((CastlingRights & WhiteQueensideMask) != 0, (CastlingRights & WhiteKingsideMask) != 0);
            }
            else
            {
                return ((CastlingRights & BlackQueensideMask) != 0, (CastlingRights & BlackKingsideMask) != 0);
            }
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
            return moveGenerator.IsSquareAttacked(kingSquare, -color, this);
        }

    }
}