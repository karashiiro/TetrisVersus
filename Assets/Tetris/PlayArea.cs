using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class PlayArea : UdonSharpBehaviour
    {
        private const int Width = 10;
        private const int LimitHeight = 20;
        private const int Height = LimitHeight + 2;

        private readonly Block[] grid = new Block[Width * Height];

        [field: SerializeField] public BlockFactory BlockFactory { get; set; }

        [field: SerializeField] public Hold Hold { get; set; }

        private Block this[int x, int y]
        {
            get => grid[y * Width + x];
            set => grid[y * Width + x] = value;
        }

        /// <summary>
        /// Adds a 2x2 tetra to the play area, with the specified x-position for
        /// its bottom-left block.
        /// </summary>
        /// <param name="bottomLeftX"></param>
        public void AddControlledSquare(int bottomLeftX)
        {
            var squareGroup = BlockFactory.CreateControlledSquare();
            CopyBlocksFrom(squareGroup, bottomLeftX);
        }

        public void Tick()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var block = this[x, y];
                    if (block != null && block.State == BlockState.Controlled)
                    {
                        HandleControlledBlockTick(block);
                    }
                }
            }
        }

        /// <summary>
        /// Fall by one space. The entire group of blocks should move as a single entity.
        /// If any blocks in the group have an at-rest block beneath them, then mark the
        /// whole group as at-rest and end the tick.
        /// </summary>
        /// <param name="block"></param>
        private void HandleControlledBlockTick(Block block)
        {
        }

        private void CopyBlocksFrom(BlockGroup group, int bottomLeftX)
        {
            foreach (var pos in group.GetEncodedPositions())
            {
                BlockGroup.DecodePosition(pos, out var localX, out var localY);
                this[bottomLeftX + localX, LimitHeight + localY] = group[localX, localY];
            }
        }
    }
}