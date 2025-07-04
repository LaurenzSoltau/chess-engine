using System.Diagnostics.CodeAnalysis;

namespace Chess
{
    public static class BoardRepresentation
    {
        public static int IndexFromCoord(int rank, int file)
        {
            return rank * 8 + file;
        }

        public static (int rank, int file) CoordFromIndex(int index)
        {
            return (index / 8, index % 8);
        }

        public static int PerspectiveIndex(int index, bool fromWhitePerspective)
        {
            (int rank, int file) = CoordFromIndex(index);

            if (!fromWhitePerspective)
            {
                rank = 7 - rank;
                file = 7 - file;
            }

            return IndexFromCoord(rank, file);
        }
    }
}