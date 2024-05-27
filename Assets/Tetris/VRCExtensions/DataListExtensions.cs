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

        public static T[] ToReferenceArray<T>(this DataList list) where T : class
        {
            var arr = new T[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                if (list.TryGetValue(i, TokenType.Reference, out var value))
                {
                    arr[i] = value.As<T>();
                }
            }

            return arr;
        }
    }
}