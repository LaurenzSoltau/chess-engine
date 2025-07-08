namespace Chess
{
    public readonly struct Move
    {
        public int From { get; }
        public int To { get; }
        public int MovingPiece { get; }
        public int CapturedPiece { get; }
        public int PromotionPiece { get; }
        public bool IsEnPassant { get; }
        public bool IsCastling { get; }
        public int movingPieceColor { get; }
        public bool isValid { get; }

        public Move(
            int from, int to, int movingPiece, int capturedPiece = 0,
            int promotionPiece = 0, bool isEnPassant = false,
            bool isCastling = false
        )
        {
            From = from;
            To = to;
            MovingPiece = movingPiece;
            CapturedPiece = capturedPiece;
            PromotionPiece = promotionPiece;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
            movingPieceColor = Piece.GetColor(movingPiece);
            isValid = true;

        }
        public Move() { isValid = false; }
    }
}