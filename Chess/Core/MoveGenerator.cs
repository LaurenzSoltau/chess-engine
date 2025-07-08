using System;
using System.Collections.Generic;
using System.Linq;

namespace Chess
{
    public class MoveGenerator
    {
        List<Move> moves;
        // N E S W, NE, SE, SW, NW
        readonly int[] whitePromotionPieces = [Piece.WhiteQueen, Piece.WhiteBishop,
        Piece.WhiteKnight, Piece.WhiteRook];

        readonly int[] blackPromotionPieces = [Piece.BlackQueen, Piece.BlackBishop,
        Piece.BlackKnight, Piece.BlackRook];
        int myColor;
        bool onlyCapturesAndPromotions;

        Board board;
        MovementData moveData;

        public MoveGenerator()
        {
            moveData = new();
        }

        public List<Move> GenerateLegalMoves(Board board, int color, bool onlyCapturesAndPromotions = false)
        {
            var pseudoLegalMoves = GeneratePseudoLegalMoves(board, color, onlyCapturesAndPromotions);
            List<Move> legalMoves = [];
            for (int i = 0; i < pseudoLegalMoves.Count; i++)
            {
                Move move = pseudoLegalMoves[i];
                board.MakeMove(move);
                if (!board.IsKingInCheck(-board.ColourToMove))
                {
                    legalMoves.Add(move);
                }
                board.UnmakeMove(move);

            }
            return legalMoves;
        }

        public List<Move> GeneratePseudoLegalMoves(Board board, int color, bool onlyCapturesAndPromotions = false, bool excludeKingMoves = false)
        {
            this.board = board;
            this.onlyCapturesAndPromotions = onlyCapturesAndPromotions;
            Init(color);
            for (int squareIndex = 0; squareIndex < board.Squares.Length; squareIndex++)
            {
                int pieceCode = board.Squares[squareIndex];
                int pieceCodeWhite = Math.Abs(pieceCode);
                if (!Piece.IsColor(pieceCode, color)) continue;

                if (pieceCodeWhite == Piece.WhiteBishop || pieceCodeWhite == Piece.WhiteRook || pieceCodeWhite == Piece.WhiteQueen)
                {
                    GenerateSlidingPieceMoves(squareIndex, pieceCode);
                }

                if (pieceCodeWhite == Piece.WhitePawn)
                {
                    GeneratePawnPieceMoves(squareIndex, pieceCode);
                }

                if (pieceCodeWhite == Piece.WhiteKnight)
                {
                    GenerateKnightPieceMoves(squareIndex, pieceCode);
                }
                if (pieceCodeWhite == Piece.WhiteKing && !excludeKingMoves)
                {
                    GenerateKingPieceMoves(squareIndex, pieceCode);
                }
            }
            return moves;
        }

        void Init(int color)
        {
            moves = new List<Move>(64);
            myColor = color;
        }

        void GenerateSlidingPieceMoves(int fromSquare, int pieceCode)
        {
            // limit directions if piece is bishop or rook (inlcude all if queen)
            int dirStart = Math.Abs(pieceCode) == Piece.WhiteBishop ? 4 : 0;
            int dirEnd = Math.Abs(pieceCode) == Piece.WhiteRook ? 4 : 8;


            for (int i = dirStart; i < dirEnd; i++)
            {
                int targetSquare = fromSquare;
                while (true)
                {
                    int prevSquare = targetSquare;
                    targetSquare += moveData.directionOffsets[i];

                    if (!IsOnBoard(prevSquare, targetSquare, moveData.directionOffsets[i])) break;

                    int targetSquarePiece = board.Squares[targetSquare];

                    // break if piece of same color is in the way;
                    if (Piece.IsColor(targetSquarePiece, myColor)) break;

                    if (!onlyCapturesAndPromotions)
                    {
                        Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                        moves.Add(newMove);
                    }
                    else
                    {
                        if (targetSquarePiece != Piece.None)
                        {
                            Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                            moves.Add(newMove);
                        }
                    }

                    //break after capture.
                    if (targetSquarePiece != Piece.None) break;
                }
            }
        }

        void GeneratePawnPieceMoves(int fromSquare, int pieceCode)
        {
            int direction = Piece.IsWhite(pieceCode) ? 8 : -8;
            int startRank = Piece.IsWhite(pieceCode) ? 1 : 6;
            int promotionRank = Piece.IsWhite(pieceCode) ? 6 : 1;
            int currentRank = fromSquare / 8;
            bool isOnPromotionRank = currentRank == promotionRank;

            int forwardOne = fromSquare + direction;


            //single step
            if (IsOnBoard(fromSquare, forwardOne, direction) && board.Squares[forwardOne] == Piece.None)
            {
                if (isOnPromotionRank)
                {
                    GeneratePromotionMoves(fromSquare, forwardOne, pieceCode, Piece.None);
                }
                else
                {
                    if (!onlyCapturesAndPromotions)
                    {
                        Move newMove = new(fromSquare, forwardOne, pieceCode);
                        moves.Add(newMove);
                    }
                }

                //double step
                int forwardTwo = forwardOne + direction;
                if ((fromSquare / 8) == startRank && board.Squares[forwardTwo] == Piece.None)
                {
                    if (isOnPromotionRank)
                    {
                        GeneratePromotionMoves(fromSquare, forwardTwo, pieceCode, Piece.None);
                    }
                    else
                    {
                        if (!onlyCapturesAndPromotions)
                        {
                            Move newMove = new(fromSquare, forwardTwo, pieceCode);
                            moves.Add(newMove);
                        }
                    }
                }


            }
            //capture moves
            int[] captureOffsets = { direction + 1, direction - 1 };
            for (int i = 0; i < captureOffsets.Length; i++)
            {
                int offset = captureOffsets[i];
                //normal capture
                int targetSquare = fromSquare + offset;
                if (!IsOnBoard(fromSquare, targetSquare, offset)) continue;

                int targetPiece = board.Squares[targetSquare];

                if (targetPiece != Piece.None && !Piece.IsColor(targetPiece, myColor))
                {
                    if (isOnPromotionRank)
                    {
                        GeneratePromotionMoves(fromSquare, targetSquare, pieceCode, targetPiece);
                    }
                    else
                    {
                        if (!onlyCapturesAndPromotions)
                        {
                            Move newMove = new(fromSquare, targetSquare, pieceCode, targetPiece);
                            moves.Add(newMove);
                        }
                    }
                }

                //en passant
                if (targetSquare == board.EnPassantSquare)
                {
                    int capturedPawnSquare = targetSquare - direction;
                    int capturedPiece = board.Squares[capturedPawnSquare];
                    if (Math.Abs(capturedPiece) == Piece.WhitePawn && !Piece.IsColor(capturedPiece, myColor))
                    {
                        Move newMove = new(fromSquare, targetSquare, pieceCode, capturedPiece, Piece.None, true);
                        moves.Add(newMove);
                    }
                }
            }

        }

        void GeneratePromotionMoves(int fromSquare, int toSquare, int movingPiece, int capturedPiece)
        {
            int[] promotionPiece = board.ColourToMove == Piece.White ? whitePromotionPieces : blackPromotionPieces;
            for (int i = 0; i < promotionPiece.Length; i++)
            {
                int _promotionPiece = promotionPiece[i];
                moves.Add(new Move(fromSquare, toSquare, movingPiece, capturedPiece, _promotionPiece, false, false));
            }
        }

        void GenerateKnightPieceMoves(int fromSquare, int pieceCode)
        {
            for (int i = 0; i < moveData.knightOffsets.Length; i++)
            {
                int offset = moveData.knightOffsets[i];
                int targetSquare = fromSquare + offset;
                if (!IsKnightOnBoard(fromSquare, targetSquare)) continue;

                int targetSquarePiece = board.Squares[targetSquare];
                if (Piece.IsColor(targetSquarePiece, myColor)) continue;

                if (!onlyCapturesAndPromotions)
                {
                    Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                    moves.Add(newMove);
                }
                else
                {
                    if (targetSquarePiece != Piece.None)
                    {
                        Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                        moves.Add(newMove);
                    }
                }
            }
        }

        void GenerateKingPieceMoves(int fromSquare, int pieceCode)
        {
            // check normal moves
            for (int i = 0; i < moveData.directionOffsets.Length; i++)
            {
                int offset = moveData.directionOffsets[i];
                int targetSquare = fromSquare + offset;
                if (!IsOnBoard(fromSquare, targetSquare, offset)) continue;

                int targetSquarePiece = board.Squares[targetSquare];
                if (Piece.IsColor(targetSquarePiece, myColor)) continue;

                if (!onlyCapturesAndPromotions)
                {
                    Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                    moves.Add(newMove);
                }
                else
                {
                    if (targetSquarePiece != Piece.None)
                    {
                        Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                        moves.Add(newMove);
                    }
                }

            }
            if (onlyCapturesAndPromotions) return;

            //check castle moves
            //kingside
            if (board.HasColorCastleRight(Piece.GetColor(pieceCode)).Kingside)
            {
                int kingsideTarget = fromSquare + 2;
                if (IsCastlePathClearAndSafe(fromSquare, kingsideTarget, true))
                {
                    Move newMove = new(fromSquare, kingsideTarget, pieceCode, Piece.None, 0, false, true);
                    moves.Add(newMove);
                }
            }

            if (board.HasColorCastleRight(Piece.GetColor(pieceCode)).Queenside)
            {
                int queensideTarget = fromSquare - 2;
                if (IsCastlePathClearAndSafe(fromSquare, queensideTarget, false))
                {
                    Move newMove = new(fromSquare, queensideTarget, pieceCode, Piece.None, 0, false, true);
                    moves.Add(newMove);
                }
            }
        }

        bool IsCastlePathClearAndSafe(int fromSquare, int toSquare, bool isKingside)
        {
            int[] squaresToCheck = isKingside ? [fromSquare + 1, fromSquare + 2] : [fromSquare - 1, fromSquare - 2, fromSquare - 3];

            // check if path is clear
            for (int i = 0; i < squaresToCheck.Length; i++)
            {
                int square = squaresToCheck[i];
                if (board.Squares[square] != Piece.None) return false;
            }

            //check if current or one of the path squares is under Attack 
            squaresToCheck = [fromSquare, squaresToCheck[0], squaresToCheck[1]];
            for (int i = 0; i < squaresToCheck.Length; i++)
            {
                int square = squaresToCheck[i];
                if (IsSquareAttacked(square, -myColor, board)) return false;
            }

            return true;
        }

        public bool IsSquareAttacked(int square, int byColor, Board board)
        {
            int[] pawnAttackOffsets = byColor == Piece.Black ? moveData.whitePawnAttackOffsets : moveData.blackPawnAttackOffsets;
            int[] knightAttackOffsets = moveData.knightOffsets;
            int[] kingAttackoffsets = moveData.directionOffsets;
            int[] bishopAttackOffsets = moveData.directionOffsets[4..8];
            int[] rookAttackOffsets = moveData.directionOffsets[0..4];

            //check  pawn Attacks;
            //attackOffsets are reversed, because we check from the square, which is attacked.
            for (int i = 0; i < pawnAttackOffsets.Length; i++)
            {
                int offset = pawnAttackOffsets[i];
                int attackerSquare = square + offset;
                if (!IsOnBoard(attackerSquare, square, offset)) continue;
                bool isRightPiece = Math.Abs(board.Squares[attackerSquare]) == Piece.WhitePawn;
                bool isRightColor = Piece.IsColor(board.Squares[attackerSquare], byColor);
                if (isRightColor && isRightPiece) return true;
            }

            //check Knight Attacks
            for (int i = 0; i < knightAttackOffsets.Length; i++)
            {
                int offset = knightAttackOffsets[i];
                int attackerSquare = square + offset;
                if (!IsKnightOnBoard(attackerSquare, square)) continue;
                bool isRightPiece = Math.Abs(board.Squares[attackerSquare]) == Piece.WhiteKnight;
                bool isRightColor = Piece.IsColor(board.Squares[attackerSquare], byColor);
                if (isRightColor && isRightPiece) return true;
            }

            // check King Attacks
            for (int i = 0; i < kingAttackoffsets.Length; i++)
            {
                int offset = kingAttackoffsets[i];
                int attackerSquare = square + offset;
                if (!IsOnBoard(attackerSquare, square, offset)) continue;
                bool isRightPiece = Math.Abs(board.Squares[attackerSquare]) == Piece.WhiteKing;
                bool isRightColor = Piece.IsColor(board.Squares[attackerSquare], byColor);
                if (isRightColor && isRightPiece) return true;
            }

            //check sliding pieces
            //check rook and Queen 
            for (int i = 0; i < rookAttackOffsets.Length; i++)
            {
                int offset = rookAttackOffsets[i];

                int attackerSquare = square;
                while (true)
                {
                    int prevSquare = attackerSquare;
                    attackerSquare += offset;
                    if (!IsOnBoard(attackerSquare, prevSquare, offset)) break;

                    int piece = board.Squares[attackerSquare];
                    if (piece != Piece.None)
                    {
                        bool isRook = Math.Abs(piece) == Piece.WhiteRook;
                        bool isQueen = Math.Abs(piece) == Piece.WhiteQueen;
                        bool isRightPiece = isRook || isQueen;
                        bool isRightColor = Piece.IsColor(piece, byColor);
                        if (isRightPiece && isRightColor) return true;

                        break;
                    }
                }
            }

            // check for Bishop and rooks
            for (int i = 0; i < bishopAttackOffsets.Length; i++)
            {
                int offset = bishopAttackOffsets[i];

                int attackerSquare = square;
                while (true)
                {
                    int prevSquare = attackerSquare;
                    attackerSquare += offset;
                    if (!IsOnBoard(attackerSquare, prevSquare, offset)) break;

                    int piece = board.Squares[attackerSquare];
                    if (piece != Piece.None)
                    {
                        bool isBishop = Math.Abs(piece) == Piece.WhiteBishop;
                        bool isQueen = Math.Abs(piece) == Piece.WhiteQueen;
                        bool isRightPiece = isBishop || isQueen;
                        bool isRightColor = Piece.IsColor(piece, byColor);
                        if (isRightPiece && isRightColor) return true;

                        break;
                    }
                }
            }
            return false;
        }

        bool IsOnBoard(int from, int to, int direction)
        {
            if (to < 0 || to > 63 || from < 0 || from > 63)
            {
                return false;
            }

            int fromRow = from / 8;
            int fromCol = from % 8;

            int targetRow = to / 8;
            int targetCol = to % 8;

            // For horizontal and diagonal moves, the column difference must be exactly 1
            if (moveData.directionOffsets.Contains(direction) &&
                Math.Abs(targetCol - fromCol) > 1)
            {
                return false;
            }


            return true;
        }

        bool IsKnightOnBoard(int from, int to)
        {
            if (to < 0 || to > 63 || from < 0 || from > 63) return false;
            int fromRow = from / 8;
            int toRow = to / 8;

            int fromCol = from % 8;
            int toCol = to % 8;

            int colDiff = Math.Abs(fromCol - toCol);
            int rowDiff = Math.Abs(fromRow - toRow);

            return (colDiff == 2 && rowDiff == 1) || (colDiff == 1 && rowDiff == 2);

        }

    }
}