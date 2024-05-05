using JetBrains.Annotations;
using UdonSharp;

namespace Tetris
{
    public class Hold : UdonSharpBehaviour
    {
        [CanBeNull] private BlockGroup current;

        public BlockGroup Exchange(BlockGroup group)
        {
            var last = current;
            current = group;
            return last;
        }
    }
}