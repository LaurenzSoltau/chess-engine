namespace Chess.Testing
{
    public struct Test(string fen, int depth, long expectedNodeCount)
    {
        public string FenString = fen;
        public int Depth = depth;
        public long ExpectedNodeCount = expectedNodeCount;
    }

}