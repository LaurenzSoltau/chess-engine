namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        public ulong zobristKey;
        public ulong[] repetitionHistory = new ulong[2048];
        public int historyLength;


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
            int whitePawns = GetPieceList(Piece.WhitePawn).Count;
            int whiteBishops = GetPieceList(Piece.WhiteBishop).Count;
            int whiteQueens = GetPieceList(Piece.WhiteQueen).Count;
            int whiteRooks = GetPieceList(Piece.WhiteRook).Count;
            int whiteKnights = GetPieceList(Piece.WhiteKnight).Count;
            int whiteAllPieces = whitePawns + whiteBishops + whiteQueens + whiteRooks + whiteKnights;
            int[] whiteBishopSquares = [.. GetPieceList(Piece.WhiteBishop).occupiedSquares.Take(GetPieceList(Piece.WhiteBishop).Count)];

            int blackPawns = GetPieceList(Piece.BlackPawn).Count;
            int blackBishops = GetPieceList(Piece.BlackBishop).Count;
            int blackQueens = GetPieceList(Piece.BlackQueen).Count;
            int blackRooks = GetPieceList(Piece.BlackRook).Count;
            int blackKnights = GetPieceList(Piece.BlackKnight).Count;
            int[] blackBishopSquares = [.. GetPieceList(Piece.BlackBishop).occupiedSquares.Take(GetPieceList(Piece.BlackBishop).Count)];
            int blackAllPieces = blackPawns + blackBishops + blackQueens + blackRooks + blackKnights;

            // no draw if there are pawns, queens or rooks
            int pawnsQueensAndRooks = whitePawns + blackPawns + whiteQueens + blackQueens + whiteRooks + blackRooks;
            if (pawnsQueensAndRooks > 0) return false;

            // draw if king vs. king
            int allPieces = whiteAllPieces + blackAllPieces;
            if (allPieces == 0) return true;

            // draw if Bishop and king vs. king
            if (whiteBishops == 1 && whiteAllPieces == 1 && blackAllPieces == 0) return true;
            if (blackBishops == 1 && blackAllPieces == 1 && whiteAllPieces == 0) return true;

            // draw if knight or two knights and king vs. king
            if (whiteKnights < 3 && whiteAllPieces == whiteKnights && blackAllPieces == 0) return true;
            if (blackKnights < 3 && blackAllPieces == blackKnights && whiteAllPieces == 0) return true;

            // draw if bishop and king vs. bishop and king with bishops on the same colour;
            if (whiteBishops == 1 && blackBishops == 1 && whiteAllPieces == 1 && blackAllPieces == 1 &&
                BoardRepresentation.IsLightSquare(blackBishopSquares[0]) == BoardRepresentation.IsLightSquare(whiteBishopSquares[0])) return true;

            // draw if king and multiple bishops of same color vs. king
            if (whiteBishops > 0 && whiteAllPieces == whiteBishops && blackAllPieces == 0 && AllBishopsOnSameColor(whiteBishopSquares)) return true;
            if (blackBishops > 0 && blackAllPieces == blackBishops && whiteAllPieces == 0 && AllBishopsOnSameColor(blackBishopSquares)) return true;

            // draw if bishops and king vs bishops and king and bishops on teh same color
            if (whiteBishops > 0 && blackBishops > 0 && whiteBishops == whiteAllPieces && blackBishops == blackAllPieces &&
                AllBishopsOnSameColor(whiteBishopSquares) && AllBishopsOnSameColor(blackBishopSquares)) return true;

            return false;
        }


        static bool AllBishopsOnSameColor(int[] bishopSquares)
        {
            if (bishopSquares.Length == 0)
                return false;

            bool firstColor = BoardRepresentation.IsLightSquare(bishopSquares[0]);

            foreach (int square in bishopSquares)
            {
                if (BoardRepresentation.IsLightSquare(square) != firstColor)
                    return false;
            }

            return true;
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


        public void LoadPosition(string fenString)
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
            zobristKey = Zobrist.GenerateKey(this);
            repetitionHistory[0] = zobristKey;
            historyLength = 1;
        }
        public void MakeMove(Move move, bool inSearch = false)
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
            int whitedMovingPiece = Math.Abs(movingPiece);
            int capturedPiece = move.CapturedPiece;
            int whitedCapturedPiece = Math.Abs(move.CapturedPiece);
            int promotionPiece = move.PromotionPiece;
            int whitedPromotionPiece = Math.Abs(promotionPiece);

            bool isCastling = move.IsCastling;
            bool isEnPassant = move.IsEnPassant;
            bool isCaptureMove = capturedPiece != Piece.None;
            bool isPromotion = promotionPiece != Piece.None;

            int pieceListColorIndex = ColourToMove == Piece.Black ? 1 : 0;


            // handle Captures
            if (isCaptureMove)
            {
                int captureSquare = move.To;
                if (isEnPassant)
                {
                    captureSquare = toSquare + (ColourToMove == Piece.White ? -8 : 8);
                    Squares[captureSquare] = Piece.None;
                }
                zobristKey ^= Zobrist.pieceSquareNumbers[Math.Abs(pieceListColorIndex - 1), whitedCapturedPiece - 1, captureSquare];
                GetPieceList(capturedPiece).RemovePiece(captureSquare);
            }

            // Handle castling and update Rook in PieceList
            if (isCastling)
            {
                int fromSquareRook = 0;
                int toSquareRook = 0;
                int rookPiece = ColourToMove == Piece.White ? Piece.WhiteRook : Piece.BlackRook;

                // Kingside or queenside castling
                if (toSquare == fromSquare + 2)
                {
                    // Kingside: move rook
                    fromSquareRook = fromSquare + 3;
                    toSquareRook = fromSquare + 1;
                    Squares[fromSquareRook] = Piece.None;
                    Squares[toSquareRook] = rookPiece;
                }
                else if (toSquare == fromSquare - 2)
                {
                    // Queenside: move rook
                    fromSquareRook = fromSquare - 4;
                    toSquareRook = fromSquare - 1;
                    Squares[fromSquareRook] = Piece.None;
                    Squares[toSquareRook] = rookPiece;
                }
                GetPieceList(rookPiece).MovePiece(fromSquareRook, toSquareRook);
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, Piece.WhiteRook - 1, fromSquareRook];
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, Piece.WhiteRook - 1, toSquareRook];
            }

            if (isPromotion)
            {
                Squares[toSquare] = promotionPiece;
                pawns[pieceListColorIndex].RemovePiece(fromSquare);
                GetPieceList(promotionPiece).AddPiece(toSquare);
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedPromotionPiece - 1, toSquare];

            }
            else
            {
                if (whitedMovingPiece == Piece.WhiteKing)
                {
                    kings[pieceListColorIndex] = toSquare;
                }
                else
                {
                    GetPieceList(movingPiece).MovePiece(fromSquare, toSquare);
                }

                Squares[toSquare] = movingPiece;
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, toSquare];
            }

            zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, fromSquare];
            Squares[fromSquare] = Piece.None;

            // Update Gamestate
            EnPassantSquare = (Math.Abs(fromRow - toRow) == 2 && Math.Abs(movingPiece) == Piece.WhitePawn) ? (fromSquare + toSquare) / 2 : -1;
            if (safeInfo.oldEnPassantSquare != -1)
                zobristKey ^= Zobrist.enPassantFileNumbers[safeInfo.oldEnPassantSquare % 8];
            if (EnPassantSquare != -1)
                zobristKey ^= Zobrist.enPassantFileNumbers[EnPassantSquare % 8];

            UpdateCastleRights(move);
            if (safeInfo.oldCastlingRights != CastlingRights)
            {
                zobristKey ^= Zobrist.castlingRightsNumbers[safeInfo.oldCastlingRights];
                zobristKey ^= Zobrist.castlingRightsNumbers[CastlingRights];
            }

            HalfMoveClock++;
            FullMoveClock = ColourToMove == Piece.Black ? FullMoveClock + 1 : FullMoveClock;
            ColourToMove = -ColourToMove;
            zobristKey ^= Zobrist.blackToMove;

            if (whitedMovingPiece == Piece.WhitePawn || capturedPiece != Piece.None)
            {
                HalfMoveClock = 0;
            }
            repetitionHistory[historyLength++] = zobristKey;
        }




        public void UnmakeMove(Move move, bool inSearch = false)
        {
            // restore Gamestate
            plyCount--;
            UnmakeMoveInformation savedInfo = unmakeMoveInformationTest[plyCount];

            zobristKey ^= Zobrist.blackToMove;
            if (EnPassantSquare != -1)
                zobristKey ^= Zobrist.enPassantFileNumbers[EnPassantSquare % 8];
            if (savedInfo.oldEnPassantSquare != -1)
                zobristKey ^= Zobrist.enPassantFileNumbers[savedInfo.oldEnPassantSquare % 8];

            if (savedInfo.oldCastlingRights != CastlingRights)
            {
                zobristKey ^= Zobrist.castlingRightsNumbers[CastlingRights];
                zobristKey ^= Zobrist.castlingRightsNumbers[savedInfo.oldCastlingRights];
            }

            EnPassantSquare = savedInfo.oldEnPassantSquare;
            CastlingRights = savedInfo.oldCastlingRights;
            HalfMoveClock = savedInfo.oldHalfMoveClock;
            FullMoveClock = savedInfo.oldFullMoveClock;
            ColourToMove = -ColourToMove;


            int fromSquare = move.From;
            int toSquare = move.To;
            int movingPiece = move.MovingPiece;
            int whitedMovingPiece = Math.Abs(movingPiece);
            int capturedPiece = move.CapturedPiece;
            int whitedCapturedPiece = Math.Abs(capturedPiece);
            int promotionPiece = move.PromotionPiece;

            bool isCaptureMove = capturedPiece != Piece.None;
            bool isCastling = move.IsCastling;
            bool isEnPassant = move.IsEnPassant;
            bool isPromotion = promotionPiece != Piece.None;

            int pieceListColorIndex = ColourToMove == Piece.White ? 0 : 1;

            // handle captures and add captured Piece to Piece List
            if (isCaptureMove)
            {
                int captureSquare = toSquare;
                if (isEnPassant)
                {
                    captureSquare = toSquare + (Piece.IsWhite(movingPiece) ? -8 : 8);
                    Squares[toSquare] = Piece.None;
                    zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, toSquare];
                }
                GetPieceList(capturedPiece).AddPiece(captureSquare);
                zobristKey ^= Zobrist.pieceSquareNumbers[Math.Abs(pieceListColorIndex - 1), whitedCapturedPiece - 1, captureSquare];
                Squares[captureSquare] = capturedPiece;
            }
            else
            {
                Squares[toSquare] = Piece.None;
            }

            // handle castling and update rookPiecelist
            if (isCastling)
            {
                int rookPiece = ColourToMove == Piece.White ? Piece.WhiteRook : Piece.BlackRook;
                int fromSquareRook = 0;
                int toSquareRook = 0;
                if (toSquare == fromSquare + 2) // Kingside
                {
                    fromSquareRook = fromSquare + 3;
                    toSquareRook = fromSquare + 1;
                }
                else if (move.To == move.From - 2) // Queenside
                {
                    fromSquareRook = fromSquare - 4;
                    toSquareRook = fromSquare - 1;
                }

                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, Piece.WhiteRook - 1, toSquareRook];
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, Piece.WhiteRook - 1, fromSquareRook];
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, toSquare];
                Squares[toSquareRook] = Piece.None;
                Squares[fromSquareRook] = rookPiece;
                rooks[pieceListColorIndex].MovePiece(toSquareRook, fromSquareRook);
                Squares[toSquare] = Piece.None;
            }

            if (isPromotion)
            {
                Squares[toSquare] = isCaptureMove ? capturedPiece : Piece.None;
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, Math.Abs(promotionPiece) - 1, toSquare];
                GetPieceList(promotionPiece).RemovePiece(toSquare);
                pawns[pieceListColorIndex].AddPiece(fromSquare);
            }
            else
            {
                if (whitedMovingPiece == Piece.WhiteKing)
                {
                    kings[pieceListColorIndex] = fromSquare;
                }
                else
                {
                    GetPieceList(movingPiece).MovePiece(toSquare, fromSquare);
                }

            }
            if (!isEnPassant && !isCastling && promotionPiece == 0)
                zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, toSquare];
            zobristKey ^= Zobrist.pieceSquareNumbers[pieceListColorIndex, whitedMovingPiece - 1, fromSquare];
            Squares[fromSquare] = movingPiece;

            historyLength--;
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

        public (bool Queenside, bool Kingside) HasColorCastleRight(int color)
        {
            if (color == Piece.White) return ((CastlingRights & WhiteQueensideMask) != 0, (CastlingRights & WhiteKingsideMask) != 0);
            return ((CastlingRights & BlackQueensideMask) != 0, (CastlingRights & BlackKingsideMask) != 0);
        }

        public bool IsKingInCheck(int color)
        {
            int kingSquare = color == Piece.White ? kings[0] : kings[1];
            return moveGenerator.IsSquareAttacked(kingSquare, -color, this);
        }


        public bool IsThreefoldRepetition()
        {
            int start = Math.Max(0, historyLength - HalfMoveClock - 1);
            int count = 0;

            for (int i = start; i < historyLength; i++)
            {
                if (repetitionHistory[i] == zobristKey)
                {
                    count++;
                }
            }
            if (count > 2) return true;
            return false;
        }

        public bool IsRepetition()
        {
            int start = Math.Max(0, historyLength - HalfMoveClock - 1);
            for (int i = start; i < historyLength - 1; i++)
            {
                if (repetitionHistory[i] == zobristKey)
                    return true;
            }
            return false;
        }

        public void TestPieceListConsistency()
        {
            int color = ColourToMove;

            int pawnPiece = color == Piece.White ? Piece.WhitePawn : Piece.BlackPawn;
            int[] occupiedSquares = GetPieceList(pawnPiece).occupiedSquares;
            for (int i = 0; i < GetPieceList(pawnPiece).Count; i++)
            {
                Debug.Assert(Squares[occupiedSquares[i]] == pawnPiece);
            }

            int bishopPiece = color == Piece.White ? Piece.WhiteBishop : Piece.BlackBishop;
            occupiedSquares = GetPieceList(bishopPiece).occupiedSquares;
            for (int i = 0; i < GetPieceList(bishopPiece).Count; i++)
            {
                Debug.Assert(Squares[occupiedSquares[i]] == bishopPiece);
            }

            int rookPiece = color == Piece.White ? Piece.WhiteRook : Piece.BlackRook;
            occupiedSquares = GetPieceList(rookPiece).occupiedSquares;
            for (int i = 0; i < GetPieceList(rookPiece).Count; i++)
            {
                Debug.Assert(Squares[occupiedSquares[i]] == rookPiece);
            }

            int queenPiece = color == Piece.White ? Piece.WhiteQueen : Piece.BlackQueen;
            occupiedSquares = GetPieceList(queenPiece).occupiedSquares;
            for (int i = 0; i < GetPieceList(queenPiece).Count; i++)
            {
                Debug.Assert(Squares[occupiedSquares[i]] == queenPiece);
            }

            int knightPiece = color == Piece.White ? Piece.WhiteKnight : Piece.BlackKnight;
            occupiedSquares = GetPieceList(knightPiece).occupiedSquares;
            for (int i = 0; i < GetPieceList(knightPiece).Count; i++)
            {
                Debug.Assert(Squares[occupiedSquares[i]] == knightPiece);
            }
        }
    }
}