using UdonSharp;
using VRC.SDK3.Data;

namespace Tetris
{
    public class BlockGroup : UdonSharpBehaviour
    {
        private readonly DataDictionary group = new DataDictionary();

        public Block this[int x, int y]
        {
            get => Get(x, y);
            set => Add(value, x, y);
        }

        public void Add(Block block, int localX, int localY)
        {
            // Encode the group-local position in the dictionary key
            var key = Key(localX, localY);
            var value = new DataToken(block);
            group.Add(key, value);
        }

        public Block Get(int localX, int localY)
        {
            var key = Key(localX, localY);
            return (Block)group[key].Reference;
        }

        public DataList GetEncodedPositions()
        {
            return group.GetKeys();
        }

        public static void DecodePosition(DataToken position, out int localX, out int localY)
        {
            var parts = position.String.Split(',');
            localX = int.Parse(parts[0]);
            localY = int.Parse(parts[1]);
        } 

        private static DataToken Key(int localX, int localY)
        {
            return new DataToken($"{localX},{localY}");
        }
    }
}