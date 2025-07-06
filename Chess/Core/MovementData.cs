namespace Chess
{
    public class MovementData
    {
        public readonly int[] directionOffsets = [8, 1, -8, -1, 9, -7, -9, 7];
        public readonly int[] knightOffsets = [17, 10, -6, -15, -17, -10, 6, 15];
        public readonly int[] whitePawnAttackOffsets = [7, 9];
        public readonly int[] blackPawnAttackOffsets = [-7, -9];
    }
}