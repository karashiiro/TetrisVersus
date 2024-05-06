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

        private readonly Block[] grid = new Block[Width * Height];

        [CanBeNull] private BlockGroup controlledBlockGroup;

        [field: SerializeField] public Hold Hold { get; set; }

        private Block this[int x, int y]
        {
            get => grid[y * Width + x];
            set => grid[y * Width + x] = value;
        }

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
            controlledBlockGroup.Translate(Vector2.down);
        }

        private void CopyBlocksFromGroup(BlockGroup group, int bottomLeftX)
        {
            foreach (var pos in group.GetEncodedPositions())
            {
                BlockGroup.DecodePosition(pos, out var localX, out var localY);
                this[bottomLeftX + localX, LimitHeight + localY] = group[localX, localY];
            }
        }
    }
}