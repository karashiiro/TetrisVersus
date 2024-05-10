using JetBrains.Annotations;
using Tetris.Blocks;
using UdonSharp;

namespace Tetris
{
    public class Hold : UdonSharpBehaviour
    {
        [CanBeNull] private BlockGroup current;

        /// <summary>
        /// Exchanges the provided block group with the block group currently stored in the hold.
        /// </summary>
        /// <param name="group">The block group to store.</param>
        /// <returns>The stored block group prior to the method being called, or null if none was present.</returns>
        [CanBeNull]
        public BlockGroup Exchange(BlockGroup group)
        {
            var last = current;
            current = group;
            return last;
        }
    }
}