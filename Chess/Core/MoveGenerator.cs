using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Godot;

namespace Chess
{
    public class MoveGenerator
    {
        List<Move> moves;
        bool whiteToMove;
        // N E S W, NE, SE, SW, NW
        readonly int[] directionOffsets = [8, 1, -8, -1, 9, -7, -9, 7];
        readonly int[] knightOffsets = [17, 10, -6, -15, -17, -10, 6, 15];

        readonly int[] castleOffsets = [-2, +2];
        readonly int[] whitePromotionPieces = [Piece.WhiteQueen, Piece.WhiteBishop,
        Piece.WhiteKnight, Piece.WhiteRook];

        readonly int[] blackPromotionPieces = [Piece.BlackQueen, Piece.BlackBishop,
        Piece.BlackKnight, Piece.BlackRook];
        int myColor;

        Board board;

        public List<Move> GeneratePseudoLegalMoves(Board board, int color, bool excludeKingMoves = false)
        {
            this.board = board;
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
                    targetSquare += directionOffsets[i];

                    if (!IsOnBoard(prevSquare, targetSquare, directionOffsets[i])) break;

                    int targetSquarePiece = board.Squares[targetSquare];

                    // break if piece of same color is in the way;
                    if (Piece.IsColor(targetSquarePiece, myColor)) break;

                    Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                    moves.Add(newMove);

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
            int currentRank = Board.GetCoords(fromSquare).row;
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
                    Move newMove = new(fromSquare, forwardOne, pieceCode);
                    moves.Add(newMove);
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
                        Move newMove = new(fromSquare, forwardTwo, pieceCode);
                        moves.Add(newMove);
                    }
                }


            }
            //capture moves
            int[] captureOffsets = { direction + 1, direction - 1 };
            foreach (int offset in captureOffsets)
            {
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
                        Move newMove = new(fromSquare, targetSquare, pieceCode, targetPiece);
                        moves.Add(newMove);
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
            foreach (int _promotionPiece in promotionPiece)
            {
                moves.Add(new Move(fromSquare, toSquare, movingPiece, capturedPiece, _promotionPiece, false, false));
            }
        }

        void GenerateKnightPieceMoves(int fromSquare, int pieceCode)
        {
            foreach (int offset in knightOffsets)
            {
                int targetSquare = fromSquare + offset;
                if (!IsKnightOnBoard(fromSquare, targetSquare)) continue;

                int targetSquarePiece = board.Squares[targetSquare];
                if (Piece.IsColor(targetSquarePiece, myColor)) continue;

                Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                moves.Add(newMove);
            }
        }

        void GenerateKingPieceMoves(int fromSquare, int pieceCode)
        {
            // check normal moves
            foreach (int offset in directionOffsets)
            {
                int targetSquare = fromSquare + offset;
                if (!IsOnBoard(fromSquare, targetSquare, offset)) continue;

                int targetSquarePiece = board.Squares[targetSquare];
                if (Piece.IsColor(targetSquarePiece, myColor)) continue;

                Move newMove = new(fromSquare, targetSquare, pieceCode, targetSquarePiece);
                moves.Add(newMove);
            }

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
            foreach (int square in squaresToCheck)
            {
                if (board.Squares[square] != Piece.None) return false;
            }

            //check if current or one of the path squares is under Attack 
            squaresToCheck = [fromSquare, squaresToCheck[0], squaresToCheck[1]];
            foreach (int square in squaresToCheck)
            {
                if (IsSquareAttacked(square, -myColor)) return false;
            }

            return true;
        }

        public bool IsSquareAttacked(int square, int byColor)
        {
            //check enemyKing
            foreach (int offset in directionOffsets)
            {
                int kingSquare = square + offset;
                if (!IsOnBoard(kingSquare, square, offset)) continue;
                int piece = board.Squares[kingSquare];
                if (Math.Abs(piece) == Piece.WhiteKing && Piece.GetColor(piece) == byColor)
                {
                    return true;
                }
            }

            //check enemy pawns attackPattern
            for (int i = 0; i < Board.BoardSize; i++)
            {
                int piece = board.Squares[i];
                if (Math.Abs(piece) == Piece.WhitePawn && Piece.IsColor(piece, byColor))
                {
                    int[] attackOffsets = Piece.IsWhite(piece) ? [7, 9] : [-9, -7];
                    foreach (int offset in attackOffsets)
                    {
                        if (i + offset == square) return true;
                    }
                }
            }



            MoveGenerator generator = new();
            List<Move> enemeyMoves = generator.GeneratePseudoLegalMoves(board, byColor, true);

            foreach (Move move in enemeyMoves)
            {
                if (move.To == square) return true;
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
            if (directionOffsets.Contains(direction) &&
                Math.Abs(targetCol - fromCol) > 1)
            {
                return false;
            }


            return true;
        }

        bool IsKnightOnBoard(int from, int to)
        {
            if (to < 0 || to > 63) return false;
            int fromRow = Board.GetCoords(from).row;
            int toRow = Board.GetCoords(to).row;

            int fromCol = Board.GetCoords(from).col;
            int toCol = Board.GetCoords(to).col;

            int colDiff = Math.Abs(fromCol - toCol);
            int rowDiff = Math.Abs(fromRow - toRow);

            return (colDiff == 2 && rowDiff == 1) || (colDiff == 1 && rowDiff == 2);

        }

    }
}