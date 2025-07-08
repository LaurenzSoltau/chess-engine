namespace Chess
{
    public class PieceList
    {
        public int[] occupiedSquares;
        int[] map;
        int numPieces;

        public PieceList(int maxCount = 16)
        {
            occupiedSquares = new int[maxCount];
            map = new int[64];
            numPieces = 0;
        }

        public int Count
        {
            get
            {
                return numPieces;
            }
        }

        public void AddPiece(int square)
        {
            occupiedSquares[numPieces] = square;
            map[square] = numPieces;
            numPieces++;
        }

        public void RemovePiece(int square)
        {
            int pieceIndex = map[square];
            occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1];
            map[occupiedSquares[pieceIndex]] = pieceIndex;
            numPieces--;
        }

        public void MovePiece(int from, int to)
        {
            int pieceIndex = map[from];
            occupiedSquares[pieceIndex] = to;
            map[to] = pieceIndex;
        }




    }


}