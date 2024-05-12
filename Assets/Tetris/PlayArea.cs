using System;
using JetBrains.Annotations;
using Tetris.Blocks;
using Tetris.Timers;
using UdonSharp;
using UnityEngine;
using UnityExtensions;
using VRC.SDK3.Data;
using VRCExtensions;
using Random = UnityEngine.Random;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayArea : UdonSharpBehaviour
    {
        private const int Width = 10;
        private const int LimitHeight = 20;
        private const int BufferHeight = 20;
        private const int Height = LimitHeight + BufferHeight;
        private const decimal SoftDropGravityBonus = 0.8m;

        public const int RequiredNetworkBufferSize = Block.RequiredNetworkBufferSize * Width * Height;

        private readonly DataDictionary palette = new DataDictionary
        {
            { ShapeType.O.GetToken(), new DataToken(Color.yellow) },
            { ShapeType.I.GetToken(), new DataToken(Color.cyan) },
            { ShapeType.S.GetToken(), new DataToken(Color.green) },
            { ShapeType.Z.GetToken(), new DataToken(Color.red) },
            { ShapeType.T.GetToken(), new DataToken(Color.magenta) },
            { ShapeType.L.GetToken(), new DataToken(PaletteHelpers.FromHex("ff7425")) },
            { ShapeType.J.GetToken(), new DataToken(Color.blue) },
        };

        private ShapeType[] randomBag;
        private int randomBagIndex = RandomGenerator.SequenceLength - 1;

        private DataDictionary srsTables;
        private int[] srsTranslationBuffer;

        private decimal gravityPerTick = 1m / 32;
        private decimal gravityProgress;

        private bool softDropEnabled;

        private bool canExchangeWithHold = true;

        [CanBeNull] private BlockGroup controlledBlockGroup;

        [field: SerializeField] public BlockFactory BlockFactory { get; set; }
        [field: SerializeField] public BlockGroup Grid { get; set; }
        [field: SerializeField] public Hold Hold { get; set; }
        [field: SerializeField] public Queue Queue { get; set; }
        [field: SerializeField] public LockTimer LockTimer { get; set; }
        [field: SerializeField] public AutoRepeatTimer AutoRepeatTimer { get; set; }
        [field: SerializeField] public EntryDelayTimer EntryDelayTimer { get; set; }

        private void Awake()
        {
            if (BlockFactory == null) Debug.LogError("PlayArea.Awake: BlockFactory is null.");
            if (Grid == null) Debug.LogError("PlayArea.Awake: Grid is null.");
            if (Hold == null) Debug.LogError("PlayArea.Awake: Hold is null.");
            if (Queue == null) Debug.LogError("PlayArea.Awake: Queue is null.");
            if (LockTimer == null) Debug.LogError("PlayArea.Awake: LockTimer is null.");
            if (AutoRepeatTimer == null) Debug.LogError("PlayArea.Awake: AutoRepeatTimer is null.");
            if (EntryDelayTimer == null) Debug.LogError("PlayArea.Awake: EntryDelayTimer is null.");
        }

        private void Start()
        {
            Debug.Log("Initializing play area");
            randomBag = RandomGenerator.NewSequence(out randomBagIndex);
            SRSHelpers.NewDataTable(out srsTables, out srsTranslationBuffer);

            // Fill the queue initially
            RefillQueue();
        }

        public bool ShouldSerialize()
        {
            return Grid.ShouldSerialize();
        }

        public int SerializeInto(byte[] buffer, int offset)
        {
            return Grid.SerializeInto(buffer, offset);
        }

        public void Tick()
        {
            // Load the next shape if we don't have an active one yet
            if (controlledBlockGroup == null)
            {
                EntryDelayTimer.BeginTimer();
                return;
            }

            // Increment gravity progress
            gravityProgress += gravityPerTick;
            if (softDropEnabled) gravityProgress += SoftDropGravityBonus;

            // Do updates for the current controlled block group
            while (gravityProgress >= 1)
            {
                HandleControlledBlockGravity();
                gravityProgress--;
            }
        }

        public void SendGarbage(int lines = 1)
        {
            // Push all existing blocks upwards
            // TODO: Avoid possible race condition where garbage causes the controlled group to get stuck in another block
            for (var y = Height - lines - 1; y >= 0; y--)
            {
                for (var x = 0; x < Width; x++)
                {
                    var block = Grid[x, y];
                    if (block == null) continue;

                    Grid[x, y + lines] = block;
                    block.transform.Translate(new Vector3(0, lines));
                    Grid[x, y] = null;
                }
            }

            for (var y = 0; y < lines; y++)
            {
                var garbageLine = CreateGarbageLine();
                AddBlocks(garbageLine, 0, y);
            }
        }

        private BlockGroup CreateGarbageLine()
        {
            var hole = Random.Range(0, Width);
            var group = BlockFactory.CreateBlockGroup();
            for (var x = 0; x < Width; x++)
            {
                if (x == hole) continue;
                BlockFactory.CreateBlock(group, x, 0);
            }

            group.SetColor(Color.grey);
            return group;
        }

        public void ExchangeHold()
        {
            if (!canExchangeWithHold) return;
            canExchangeWithHold = false;

            RemoveBlocksFromGroup(controlledBlockGroup);
            Hold.Exchange(ref controlledBlockGroup, BlockState.Controlled);

            if (controlledBlockGroup != null)
            {
                AddControlledBlocks(controlledBlockGroup);
            }
            else
            {
                LoadNextShape();
            }
        }

        public void LockControlledGroup()
        {
            // Validate that we should actually be locking
            if (IsGroupMovementValid(controlledBlockGroup, 0, -1))
            {
                return;
            }

            // Lock the controlled block group
            if (controlledBlockGroup != null)
            {
                controlledBlockGroup.SetState(BlockState.AtRest);
                controlledBlockGroup = null;
            }

            // Re-parent stray blocks - useful for seeing discrepancies between world positions and raw data
            Grid.ClaimUncontrolledBlocks();

            // Check if any lines were cleared
            HandleLineClears();

            // Re-enable the hold function
            canExchangeWithHold = true;
        }

        public void LoadNextShape()
        {
            var shape = Queue.Pop(transform);
            if (shape == null) return;
            AddControlledBlocks(shape);

            // Refill the queue immediately
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
            AddBlocks(group, 5, LimitHeight);
            SetControlledBlockGroup(group);
        }

        private void AddBlocks(BlockGroup group, int x, int y)
        {
            group.transform.SetParent(transform, true);
            group.SetPosition(new Vector2(x, y));
            CopyBlocksFromGroup(group, x, y);
        }

        private void SetControlledBlockGroup(BlockGroup group)
        {
            group.SetState(BlockState.Controlled);
            controlledBlockGroup = group;
        }

        /// <summary>
        /// Fall by one space. The entire group of blocks should move as a single entity.
        /// If any blocks in the group have an at-rest block beneath them, then enable the
        /// lock timer to let the group settle.
        /// </summary>
        private void HandleControlledBlockGravity()
        {
            if (controlledBlockGroup == null) return;

            // Validate that the group is still active going into the next tick
            foreach (var block in controlledBlockGroup.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y, caller: nameof(HandleControlledBlockGravity)))
                    continue;

                var nextBlock = Grid[x, y - 1];
                if (y == 0 || nextBlock != null && nextBlock.State == BlockState.AtRest)
                {
                    // We're either at the bottom of the grid, or there's a block beneath us
                    LockTimer.BeginTimer();
                    return;
                }
            }

            // Now that we've determined that the entire group is still active, copy it down by one space
            MoveControlledGroup(0, -1);
        }

        public void SoftDrop(bool isEnabled)
        {
            softDropEnabled = isEnabled;
        }

        public void HardDrop()
        {
            // Drop the block as far as possible
            bool landed;
            do
            {
                landed = !MoveGroup(controlledBlockGroup, 0, -1);
            } while (!landed);

            // Clear the lock timer and lock the controlled group immediately
            LockTimer.ResetTimer();
            LockControlledGroup();
        }

        public void RotateControlledGroupLeft()
        {
            if (RotateGroup(controlledBlockGroup, Rotation.Left))
            {
                // Reset the lock timer on successful rotations, but keep it running if it's active
                LockTimer.ResetTimerWhileLocking();
            }
        }

        public void RotateControlledGroupRight()
        {
            if (RotateGroup(controlledBlockGroup, Rotation.Right))
            {
                // Reset the lock timer on successful rotations, but keep it running if it's active
                LockTimer.ResetTimerWhileLocking();
            }
        }

        private bool RotateGroup(BlockGroup group, Rotation rotation)
        {
            if (group == null) return false;

            // Validate that the group can rotate in the requested direction
            if (!IsGroupMovementValidSRS(group, rotation, out var dXSrs, out var dYSrs))
            {
                return false;
            }

            // Copy the group to the desired location
            var blocks = group.GetBlocks();
            var originalPositions = Grid.GetEncodedPositionsForBlocks(blocks);
            for (var i = 0; i < blocks.Length; i++)
            {
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;
                Grid[x, y] = null;
            }

            var srsDisplacement = new Vector2(dXSrs, dYSrs);
            for (var i = 0; i < blocks.Length; i++)
            {
                if (!group.TryGetPositionAbsolute(blocks[i], out var localX, out var localY)) continue;
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;

                var angle = rotation.AsDegrees();
                var position = new Vector2(localX, localY);
                var displacement = position.Rotate(angle) - position + srsDisplacement;

                var targetX = x + Convert.ToInt32(displacement.x);
                var targetY = y + Convert.ToInt32(displacement.y);
                Grid[targetX, targetY] = blocks[i];
            }

            // Move the group in world space
            group.Translate(srsDisplacement);
            group.Rotate(rotation);

            return true;
        }

        public void DisableAutoRepeat()
        {
            PrepareAutoRepeat(AutoRepeatDirection.None);
        }

        public void PrepareAutoRepeat(AutoRepeatDirection direction)
        {
            if (direction == AutoRepeatDirection.None)
            {
                AutoRepeatTimer.ResetTimer();
            }
            else
            {
                AutoRepeatTimer.BeginTimerWithDirection(direction);
            }
        }

        public void MoveControlledGroup(int dX, int dY)
        {
            if (MoveGroup(controlledBlockGroup, dX, dY))
            {
                // Reset the lock timer on successful moves, but keep it running if it's active
                LockTimer.ResetTimerWhileLocking();
            }
        }

        private bool MoveGroup(BlockGroup group, int dX, int dY)
        {
            if (group == null) return false;

            // Validate that the group can move in the requested direction
            if (!IsGroupMovementValid(group, dX, dY))
            {
                return false;
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

            return true;
        }

        private bool IsGroupMovementValidSRS(BlockGroup group, Rotation rotation, out int dX, out int dY)
        {
            dX = dY = 0;

            var translations = SRSHelpers.GetPossibleTranslations(srsTables, srsTranslationBuffer, group.Type,
                group.Orientation, rotation);
            foreach (var translation in translations)
            {
                if (IsGroupMovementValid(group, rotation, translation[0], translation[1]))
                {
                    dX = translation[0];
                    dY = translation[1];
                    return true;
                }
            }

            return false;
        }

        private bool IsGroupMovementValid(BlockGroup group, Rotation rotation, int dX, int dY)
        {
            var srsTranslation = new Vector2(dX, dY);
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
                var displacement = position.Rotate(rotation.AsDegrees()) - position + srsTranslation;
                if (!IsBlockMovementValid(block, Convert.ToInt32(displacement.x), Convert.ToInt32(displacement.y)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsGroupMovementValid(BlockGroup group, int dX, int dY)
        {
            if (group == null) return false;

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

        private void CopyBlocksFromGroup(BlockGroup group, int bottomLeftX, int bottomLeftY)
        {
            foreach (var pos in group.GetEncodedPositions())
            {
                if (!group.TryDecodePosition(pos, out var localX, out var localY)) continue;
                Grid[bottomLeftX + localX, bottomLeftY + localY] = group[localX, localY];
            }
        }

        private void RemoveBlocksFromGroup(BlockGroup group)
        {
            foreach (var block in group.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y)) continue;
                Grid[x, y] = null;
            }
        }

        private static bool IsIndexInBounds(int x, int y)
        {
            return y >= 0 && y < Height && x >= 0 && x < Width;
        }
    }
}