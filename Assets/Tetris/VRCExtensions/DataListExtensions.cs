using VRC.SDK3.Data;

namespace Tetris.VRCExtensions
{
    public static class DataListExtensions
    {
        public static int[] ToIntArray(this DataList list)
        {
            var arr = new int[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                if (list.TryGetValue(i, TokenType.Int, out var value))
                {
                    arr[i] = value.Int;
                }
            }

            return arr;
        }
    }
}