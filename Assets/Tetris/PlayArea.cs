using System;
using DataTokenExtensions;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityExtensions;
using VRC.SDK3.Data;

namespace Tetris
{
    public class PlayArea : UdonSharpBehaviour
    {
        private const int Width = 10;
        private const int LimitHeight = 20;
        private const int BufferHeight = 20;
        private const int Height = LimitHeight + BufferHeight;

        private readonly DataDictionary palette = new DataDictionary
        {
            { ShapeType.Square.GetToken(), new DataToken(Color.yellow) },
            { ShapeType.Straight.GetToken(), new DataToken(Color.cyan) },
            { ShapeType.LeftSkew.GetToken(), new DataToken(Color.green) },
            { ShapeType.RightSkew.GetToken(), new DataToken(Color.red) },
            { ShapeType.T.GetToken(), new DataToken(Color.magenta) },
            { ShapeType.LeftL.GetToken(), new DataToken(PaletteHelpers.FromHex("ff7425")) },
            { ShapeType.RightL.GetToken(), new DataToken(Color.blue) },
        };

        private ShapeType[] randomBag;
        private int randomBagIndex = RandomGenerator.SequenceLength - 1;

        [CanBeNull] private BlockGroup controlledBlockGroup;

        [field: SerializeField] public BlockFactory BlockFactory { get; set; }
        [field: SerializeField] public BlockGroup Grid { get; set; }
        [field: SerializeField] public Hold Hold { get; set; }
        [field: SerializeField] public Queue Queue { get; set; }

        private void Start()
        {
            Debug.Log("Initializing play area");
            randomBag = RandomGenerator.NewSequence(out randomBagIndex);
        }

        public void Tick()
        {
            // Refill the queue immediately in case we just started
            RefillQueue();

            // Handle the current controlled block group
            HandleControlledBlockTick();

            // Re-parent stray blocks - useful for seeing discrepancies between world positions and raw data
            Grid.ClaimUncontrolledBlocks();

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
            while (!Queue.IsFull)
            {
                var nextShapeType = RandomGenerator.GetNextShape(randomBag, ref randomBagIndex);
                if (!palette.TryGetValue(nextShapeType.GetToken(), TokenType.Reference, out var colorToken))
                {
                    Debug.LogError($"RefillQueue: Failed to get color for shape: {nextShapeType}");
                    colorToken = new DataToken(Color.grey);
                }

                var nextColor = colorToken.As<Color>();
                var nextShape = BlockFactory.CreateShape(nextShapeType, nextColor);
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
                if (!Grid.TryGetPosition(block, out var x, out var y, caller: nameof(HandleControlledBlockTick)))
                    continue;

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

        public void RotateControlledGroupLeft()
        {
            RotateGroup(controlledBlockGroup, 90);
        }

        public void RotateControlledGroupRight()
        {
            RotateGroup(controlledBlockGroup, -90);
        }

        private void RotateGroup(BlockGroup group, float angle)
        {
            if (group == null) return;

            // Validate that the group can rotate in the requested direction
            if (!IsGroupMovementValid(group, angle))
            {
                return;
            }

            // Copy the group to the desired location
            var blocks = group.GetBlocks();
            var originalPositions = Grid.GetEncodedPositionsForBlocks(blocks);
            for (var i = 0; i < blocks.Length; i++)
            {
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;
                Grid[x, y] = null;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                if (!group.TryGetPositionAbsolute(blocks[i], out var localX, out var localY)) continue;
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;

                var position = new Vector2(localX, localY);
                var displacement = position.Rotate(angle) - position;

                var targetX = x + Convert.ToInt32(displacement.x);
                var targetY = y + Convert.ToInt32(displacement.y);
                Grid[targetX, targetY] = blocks[i];
            }

            // Rotate the group in world space
            group.Rotate(angle);
        }

        public void MoveControlledGroup(int dX, int dY)
        {
            MoveGroup(controlledBlockGroup, dX, dY);
        }

        private void MoveGroup(BlockGroup group, int dX, int dY)
        {
            if (group == null) return;

            // Validate that the group can move in the requested direction
            if (!IsGroupMovementValid(group, dX, dY))
            {
                return;
            }

            // Copy the group to the desired location
            var blocks = group.GetBlocks();
            var originalPositions = Grid.GetEncodedPositionsForBlocks(blocks);
            for (var i = 0; i < blocks.Length; i++)
            {
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;
                Grid[x, y] = null;
            }

            for (var i = 0; i < blocks.Length; i++)
            {
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;
                Grid[x + dX, y + dY] = blocks[i];
            }

            // Move the group in world space
            group.Translate(new Vector2(dX, dY));
        }

        private bool IsGroupMovementValid(BlockGroup group, float angle)
        {
            foreach (var block in group.GetBlocks())
            {
                // Rotate the local position of the block to get displacements, then validate those displacements
                if (!group.TryGetPositionAbsolute(block, out var localX, out var localY,
                        caller: nameof(IsGroupMovementValid)))
                {
                    Debug.LogError("IsGroupMovementValid: Could not get absolute block position in group");
                    return false;
                }

                var position = new Vector2(localX, localY);
                var displacement = position.Rotate(angle) - position;
                if (!IsBlockMovementValid(block, Convert.ToInt32(displacement.x), Convert.ToInt32(displacement.y)))
                {
                    Debug.Log(
                        $"IsGroupMovementValid: Movement from local position {position} to {position + displacement} is invalid");
                    return false;
                }
            }

            return true;
        }

        private bool IsGroupMovementValid(BlockGroup group, int dX, int dY)
        {
            foreach (var block in group.GetBlocks())
            {
                if (!IsBlockMovementValid(block, dX, dY))
                {
                    // We're either at the edge of the grid, or there's a block where we want to go
                    return false;
                }
            }

            return true;
        }

        private bool IsBlockMovementValid(Block block, int dX, int dY)
        {
            if (!Grid.TryGetPosition(block, out var x, out var y, caller: nameof(IsBlockMovementValid)))
            {
                Debug.LogError("IsBlockMovementValid: Could not get block position");
                return false;
            }

            var targetX = x + dX;
            var targetY = y + dY;
            if (!IsIndexInBounds(targetX, targetY))
            {
                Debug.Log($"IsBlockMovementValid: Destination <{targetX}, {targetY}> is out of bounds");
                return false;
            }

            if (!IsLocationAvailable(targetX, targetY))
            {
                Debug.Log(
                    $"IsBlockMovementValid: Destination <{targetX}, {targetY}> is occupied by another resting block");
                return false;
            }

            return true;
        }

        private bool IsLocationAvailable(int x, int y)
        {
            var existingBlock = Grid[x, y];
            return existingBlock == null || existingBlock.State != BlockState.AtRest;
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
                if (!group.TryDecodePosition(pos, out var localX, out var localY)) continue;
                Grid[bottomLeftX + localX, LimitHeight + localY] = group[localX, localY];
            }
        }

        private static bool IsIndexInBounds(int x, int y)
        {
            return y >= 0 && y < Height && x >= 0 && x < Width;
        }
    }
}