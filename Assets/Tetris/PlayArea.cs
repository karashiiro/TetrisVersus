﻿using System;
using DataTokenExtensions;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris
{
    public class PlayArea : UdonSharpBehaviour
    {
        private const int Width = 10;
        private const int LimitHeight = 20;
        private const int Height = LimitHeight + 2;

        [CanBeNull] private BlockGroup controlledBlockGroup;

        [field: SerializeField] public BlockFactory BlockFactory { get; set; }
        [field: SerializeField] public BlockGroup Grid { get; set; }

        [field: SerializeField] public Hold Hold { get; set; }
        [field: SerializeField] public Queue Queue { get; set; }

        public void Tick()
        {
            // Refill the queue immediately in case we just started
            RefillQueue();

            // Handle the current controlled block group
            HandleControlledBlockTick();

            // Check if any lines were cleared
            HandleLineClears();

            // If we placed a shape, load a new one
            if (controlledBlockGroup == null)
            {
                LoadNextShape();
            }
        }

        private void LoadNextShape()
        {
            var shape = Queue.Pop(transform);
            if (shape == null) return;
            AddControlledBlocks(shape);

            // Load the next shape immediately
            RefillQueue();
        }

        private void RefillQueue()
        {
            var i = 0;
            while (!Queue.IsFull)
            {
                var nextShape = BlockFactory.CreateSquare(i++ % 2 == 0 ? Color.red : Color.blue);
                if (!Queue.Push(nextShape))
                {
                    Destroy(nextShape.gameObject);
                    break;
                }
            }
        }

        /// <summary>
        /// Adds controlled blocks to the play area at the limit line.
        /// </summary>
        /// <param name="group">The group of blocks to add.</param>
        private void AddControlledBlocks(BlockGroup group)
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
            MoveControlledGroup(0, -1);
        }

        public void MoveControlledGroup(int dX, int dY)
        {
            MoveGroup(controlledBlockGroup, dX, dY);
        }
        
        private void MoveGroup(BlockGroup group, int dX, int dY)
        {
            if (group == null) return;

            // Validate that the group can move in the requested direction
            if (!IsGroupMovementValid(group, dX, dY)) return;

            // Copy the group to the desired location, iterating along the direction
            // of travel to avoid accidentally overwriting blocks
            var initialCount = Grid.GetBlocks().Length;
            group.GetBounds(out var minX, out var minY, out var maxX, out var maxY);

            // Bottom to top
            for (var localY = minY; localY <= maxY; localY++)
            {
                if (Math.Sign(dX) > 0)
                {
                    // Right to left
                    for (var localX = maxX; localX >= minX; localX--)
                    {
                        MoveBlockData(group[localX, localY], dX, dY);
                    }
                }
                else
                {
                    // Left to right
                    for (var localX = minX; localX <= maxX; localX++)
                    {
                        MoveBlockData(group[localX, localY], dX, dY);
                    }
                }
            }

            var finalCount = Grid.GetBlocks().Length;
            if (finalCount != initialCount)
            {
                Debug.LogError($"MoveGroup: Lost {initialCount - finalCount} blocks while moving");
            }

            // Move the group in the world space
            group.Translate(new Vector2(dX, dY));
        }

        private bool IsGroupMovementValid(BlockGroup group, int dX, int dY)
        {
            foreach (var block in group.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y)) continue;

                var targetX = x + dX;
                var targetY = y + dY;
                var nextBlock = Grid[targetX, targetY];
                if (!IsIndexInBounds(targetX, targetY) || nextBlock != null && nextBlock.State == BlockState.AtRest)
                {
                    // We're either at the edge of the grid, or there's a block where we want to go
                    return false;
                }
            }

            return true;
        }

        private void MoveBlockData(Block block, int dX, int dY)
        {
            if (block == null || !Grid.TryGetPosition(block, out var x, out var y)) return;
            Grid[x, y] = null;
            Grid[x + dX, y + dY] = block;
        }

        private void HandleLineClears()
        {
            var linesToRemove = new DataList();

            // Check which lines should be removed
            for (var y = 0; y < Height; y++)
            {
                var shouldRemoveLine = true;
                for (var x = 0; x < Width; x++)
                {
                    if (Grid[x, y] == null || Grid[x, y].State != BlockState.AtRest)
                    {
                        shouldRemoveLine = false;
                        break;
                    }
                }

                if (shouldRemoveLine)
                {
                    linesToRemove.Add(y);
                }
            }

            // Remove all the cleared lines at once
            var lines = linesToRemove.ToIntArray();
            ClearLines(lines);
        }

        private void ClearLines(int[] ys)
        {
            foreach (var y in ys)
            {
                Debug.Log($"ClearLines: {y}");
                for (var x = 0; x < Width; x++)
                {
                    var block = Grid[x, y];
                    if (block == null) continue;
                    Destroy(block.gameObject);
                    Grid[x, y] = null;
                }
            }

            // Move the blocks above the cleared rows down one space
            for (var i = ys.Length - 1; i >= 0; i--)
            {
                var y = ys[i];
                for (var x = 0; x < Width; x++)
                {
                    for (var checkY = y + 1; checkY < Height; checkY++)
                    {
                        if (Grid[x, checkY] == null) continue;

                        var block = Grid[x, checkY - 1] = Grid[x, checkY];
                        block.transform.Translate(new Vector3(0, -1));
                        Grid[x, checkY] = null;
                    }
                }
            }
        }

        private void CopyBlocksFromGroup(BlockGroup group, int bottomLeftX)
        {
            foreach (var pos in group.GetEncodedPositions())
            {
                if (!BlockGroup.TryDecodePosition(pos, out var localX, out var localY)) continue;
                Grid[bottomLeftX + localX, LimitHeight + localY] = group[localX, localY];
            }
        }

        private static bool IsIndexInBounds(int x, int y)
        {
            return y >= 0 && y < Height && x >= 0 && x < Width;
        }
    }
}