using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class PlayArea : UdonSharpBehaviour
    {
        private const int Width = 10;
        private const int LimitHeight = 20;
        private const int Height = LimitHeight + 2;

        [CanBeNull] private BlockGroup controlledBlockGroup;

        [field: SerializeField] public BlockGroup Grid { get; set; }

        [field: SerializeField] public Hold Hold { get; set; }

        [field: SerializeField] public Queue Queue { get; set; }

        /// <summary>
        /// Adds controlled blocks to the play area at the limit line.
        /// </summary>
        /// <param name="group">The group of blocks to add.</param>
        public void AddControlledBlocks(BlockGroup group)
        {
            // Move the block group into the play area at the limit line
            group.SetPosition(new Vector2(5, LimitHeight));
            SetControlledBlockGroup(group);
            CopyBlocksFromGroup(group, 5);
        }

        private void SetControlledBlockGroup(BlockGroup group)
        {
            group.SetState(BlockState.Controlled);
            controlledBlockGroup = group;
        }

        public void Tick()
        {
            HandleControlledBlockTick();
        }

        /// <summary>
        /// Fall by one space. The entire group of blocks should move as a single entity.
        /// If any blocks in the group have an at-rest block beneath them, then mark the
        /// group as at-rest and end the tick.
        /// </summary>
        private void HandleControlledBlockTick()
        {
            if (controlledBlockGroup == null) return;

            // Validate that the group is still active going into the next tick
            foreach (var block in controlledBlockGroup.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y)) continue;

                var nextBlock = Grid[x, y - 1];
                if (y == 0 || nextBlock != null && nextBlock.State == BlockState.AtRest)
                {
                    // We're either at the bottom of the grid, or there's a block beneath us
                    controlledBlockGroup.SetState(BlockState.AtRest);
                    controlledBlockGroup = null;
                    return;
                }
            }

            // Now that we've determined that the entire group is still active, copy it down by one space
            foreach (var block in controlledBlockGroup.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y)) continue;

                var nextBlock = Grid[x, y - 1];
                if (y != 0 && nextBlock == null)
                {
                    Grid[x, y] = null;
                    Grid[x, y - 1] = block;
                }
            }

            // Move the group in the world space
            controlledBlockGroup.Translate(Vector2.down);
        }

        private void CopyBlocksFromGroup(BlockGroup group, int bottomLeftX)
        {
            foreach (var pos in group.GetEncodedPositions())
            {
                BlockGroup.DecodePosition(pos, out var localX, out var localY);
                Grid[bottomLeftX + localX, LimitHeight + localY] = group[localX, localY];
            }
        }
    }
}