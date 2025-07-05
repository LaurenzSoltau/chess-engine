using Chess;
public static class CoordUtil
{
    public static (int rank, int file) FlipForPerspective((int rank, int file) coord, bool fromWhite)
    {
        return fromWhite ? coord : (7 - coord.rank, 7 - coord.file);
    }

    public static int FlipIndexForPerspective(int index, bool fromWhite)
    {
        var coord = BoardRepresentation.CoordFromIndex(index);
        var flipped = FlipForPerspective(coord, fromWhite);
        return BoardRepresentation.IndexFromCoord(flipped.rank, flipped.file);
    }
}