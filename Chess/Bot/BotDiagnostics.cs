namespace Chess.Bot
{
    public struct BotDiagnostics (int searchTimeMs, int eval, int depthSearched)
    {
        public int searchTimeMs = searchTimeMs;
        public int eval = eval;
        public int depthSearched = depthSearched;
    }
}