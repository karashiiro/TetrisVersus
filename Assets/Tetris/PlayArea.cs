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

        public const int RequiredNetworkBufferSize = BlockGroup.RequiredNetworkBufferSizeBase +
                                                     BlockGroup.RequiredNetworkBufferSizePerBlock * Width * Height +
                                                     Queue.RequiredNetworkBufferSize +
                                                     Hold.RequiredNetworkBufferSize;

        private readonly Vector2Int boundsMin = new Vector2Int(0, 0);
        private readonly Vector2Int boundsMax = new Vector2Int(Width, Height);

        private readonly DataDictionary palette = new DataDictionary
        {
            { ShapeType.None.GetToken(), new DataToken(Color.grey) },
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

        private double gravityPerTick = GetGravityPerTick();
        private double gravityProgress;
        private int goalProgress;
        private int level = 1;

        private bool softDropEnabled;

        private bool canExchangeWithHold = true;
        private bool ownedByLocalPlayer;

        private int queuedGarbageLines;

        [CanBeNull] private BlockGroup controlledBlockGroup;
        [CanBeNull] private BlockGroup ghostPiece;

        [field: SerializeField] public UdonSharpBehaviour NotifyEventsTo { get; set; }
        [field: SerializeField] public BlockFactory BlockFactory { get; set; }
        [field: SerializeField] public BlockGroup Grid { get; set; }
        [field: SerializeField] public Hold Hold { get; set; }
        [field: SerializeField] public Queue Queue { get; set; }
        [field: SerializeField] public LockTimer LockTimer { get; set; }
        [field: SerializeField] public AutoRepeatTimer AutoRepeatTimer { get; set; }
        [field: SerializeField] public EntryDelayTimer EntryDelayTimer { get; set; }

        public int LastLinesCleared { get; private set; }

        private void Awake()
        {
            if (NotifyEventsTo == null) Debug.LogWarning("PlayArea.Awake: NotifyEventsTo is null.");
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
            Debug.Log("PlayArea.Start: Initializing play area");
            randomBag = RandomGenerator.NewSequence(out randomBagIndex);
            SRSHelpers.NewDataTable(out srsTables, out srsTranslationBuffer);

            // Fill the queue initially
            RefillQueue();
        }

        public void SetOwned(bool value)
        {
            ownedByLocalPlayer = value;
        }

        public bool ShouldSerialize()
        {
            return ownedByLocalPlayer && Grid.ShouldSerialize();
        }

        public int SerializeInto(byte[] buffer, int offset)
        {
            var nWritten = Queue.SerializeInto(buffer, offset);
            nWritten += Hold.SerializeInto(buffer, offset + nWritten);
            nWritten += Grid.SerializeInto(buffer, offset + nWritten, boundsMin, boundsMax);
            return nWritten;
        }

        public int DeserializeFrom(byte[] buffer, int offset)
        {
            var nRead = Queue.DeserializeFrom(buffer, offset, BlockFactory, palette);
            nRead += Hold.DeserializeFrom(buffer, offset + nRead, BlockFactory, palette);
            nRead += Grid.DeserializeFrom(buffer, offset + nRead, boundsMin, boundsMax, BlockFactory, palette);
            return nRead;
        }

        public void Clear()
        {
            Debug.Log("PlayArea.Clear: Clearing play area");

            EntryDelayTimer.ResetTimer();
            AutoRepeatTimer.ResetTimer();
            LockTimer.ResetTimer();
            SetOwned(false);

            LastLinesCleared = 0;
            queuedGarbageLines = 0;
            level = 1;
            goalProgress = 0;
            gravityProgress = 0;
            gravityPerTick = GetGravityPerTick();
            softDropEnabled = false;

            if (controlledBlockGroup != null)
            {
                Destroy(controlledBlockGroup.gameObject);
                controlledBlockGroup = null;
            }

            Grid.Clear();
            Hold.Clear();
            Queue.Clear();

            randomBag = RandomGenerator.NewSequence(out randomBagIndex);
            SRSHelpers.NewDataTable(out srsTables, out srsTranslationBuffer);
            RefillQueue();
        }

        public void Tick()
        {
            if (!ownedByLocalPlayer) return;

            // Load the next shape if we don't have an active one yet
            if (controlledBlockGroup == null)
            {
                EntryDelayTimer.BeginTimer();
                return;
            }

            // Increment gravity progress
            if (!softDropEnabled)
            {
                gravityProgress += gravityPerTick;
            }
            else
            {
                gravityProgress += gravityPerTick * 20;
            }

            // Do updates for the current controlled block group
            while (gravityProgress >= 1)
            {
                HandleControlledBlockGravity();
                gravityProgress--;
            }
        }

        public void SendGarbage(int lines = 1)
        {
            queuedGarbageLines += lines;
        }

        private void LoadQueuedGarbage()
        {
            if (queuedGarbageLines <= 0) return;

            // Push all existing blocks upwards
            for (var y = Height - queuedGarbageLines - 1; y >= 0; y--)
            {
                for (var x = 0; x < Width; x++)
                {
                    var block = Grid[x, y];
                    if (block == null) continue;

                    Grid[x, y + queuedGarbageLines] = block;
                    block.transform.Translate(new Vector3(0, queuedGarbageLines));
                    Grid[x, y] = null;
                }
            }

            for (var y = 0; y < queuedGarbageLines; y++)
            {
                var garbageLine = CreateGarbageLine();
                AddBlocks(garbageLine, 0, y);
            }

            queuedGarbageLines = 0;
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

            return group;
        }

        public void ExchangeHold()
        {
            if (!canExchangeWithHold || controlledBlockGroup == null) return;
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

            // Check if the game should end after this lock
            if (IsGroupOut(controlledBlockGroup) && NotifyEventsTo != null)
            {
                Debug.Log("PlayArea.LockControlledGroup: LOCK OUT");
                NotifyEventsTo.SendCustomEvent("PlayAreaOnLockOut");
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

            // Load any queued garbage lines
            LoadQueuedGarbage();

            if (ghostPiece != null) Destroy(ghostPiece.gameObject);

            // Re-enable the hold function
            canExchangeWithHold = true;
        }

        public void LoadNextShape()
        {
            Debug.Log("PlayArea.LoadNextShape: Loading next controlled group");

            var shape = Queue.Pop(transform);
            if (shape == null) return;
            AddControlledBlocks(shape);
            ReplicateControlledGroupToGhost();

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
                    Debug.LogError($"PlayArea.RefillQueue: Failed to get color for shape: {nextShapeType}");
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
            // Check if the game should end after this spawn
            if (WillGroupBlockOut(group) && NotifyEventsTo != null)
            {
                Debug.Log("PlayArea.AddControlledBlocks: TOP OUT");
                NotifyEventsTo.SendCustomEvent("PlayAreaOnTopOut");
            }

            // Move the block group into the play area at the limit line
            AddBlocks(group, 5, LimitHeight);
            SetControlledBlockGroup(group);
        }

        private bool IsGroupOut([CanBeNull] BlockGroup group)
        {
            if (group == null) return false;

            foreach (var block in group.GetBlocks())
            {
                if (Grid.TryGetPosition(block, out var x, out var y) && y > LimitHeight)
                {
                    return true;
                }
            }

            return false;
        }

        private bool WillGroupBlockOut([CanBeNull] BlockGroup group)
        {
            if (group == null) return false;

            foreach (var pos in group.GetEncodedPositions())
            {
                if (!group.TryDecodePosition(pos, out var localX, out var localY)) continue;
                if (Grid[5 + localX, LimitHeight + localY] != null)
                {
                    return true;
                }
            }

            return false;
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
            MoveGroup(controlledBlockGroup, 0, AllowedMaximumDrop());

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

            ReplicateControlledGroupToGhost();
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

            var srsDisplacement = new Vector2Int(dXSrs, dYSrs);
            var updates = $"PlayArea.RotateGroup: Rotating {blocks.Length} blocks: ";
            for (var i = 0; i < blocks.Length; i++)
            {
                if (!group.TryGetPositionAbsolute(blocks[i], out var localX, out var localY)) continue;
                if (!Grid.TryDecodePosition(originalPositions[i], out var x, out var y)) continue;

                var angle = rotation.AsDegrees();
                var position = new Vector2Int(localX, localY);
                var displacement = position.Rotate(angle) - position + srsDisplacement;

                var targetX = x + displacement.x;
                var targetY = y + displacement.y;
                Grid[targetX, targetY] = blocks[i];

                updates += $"(<{x}, {y}> -> <{targetX}, {targetY}>) ";
            }

            Debug.Log(updates);

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

            ReplicateControlledGroupToGhost();
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
                if (IsGroupMovementValid(group, rotation, translation[0], translation[1],
                        caller: nameof(IsGroupMovementValidSRS)))
                {
                    dX = translation[0];
                    dY = translation[1];
                    return true;
                }
            }

            return false;
        }

        private bool IsGroupMovementValid(BlockGroup group, Rotation rotation, int dX, int dY,
            string caller = "unknown")
        {
            var srsTranslation = new Vector2Int(dX, dY);
            foreach (var block in group.GetBlocks())
            {
                // Rotate the local position of the block to get displacements, then validate those displacements
                if (!group.TryGetPositionAbsolute(block, out var localX, out var localY,
                        caller: nameof(IsGroupMovementValid)))
                {
                    Debug.LogError($"IsGroupMovementValid({caller}): Could not get absolute block position in group");
                    return false;
                }

                var position = new Vector2Int(localX, localY);
                var displacement = position.Rotate(rotation.AsDegrees()) - position + srsTranslation;
                if (!IsBlockMovementValid(block, displacement.x, displacement.y, caller: nameof(IsGroupMovementValid)))
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
                if (!IsBlockMovementValid(block, dX, dY, caller: nameof(IsGroupMovementValid)))
                {
                    // We're either at the edge of the grid, or there's a block where we want to go
                    return false;
                }
            }

            return true;
        }

        private bool IsBlockMovementValid(Block block, int dX, int dY, string caller = "unknown")
        {
            if (!Grid.TryGetPosition(block, out var x, out var y, caller: nameof(IsBlockMovementValid)))
            {
                var gridBlocks = Grid.GetBlocks();
                Debug.LogError(gridBlocks == null
                    ? $"PlayArea.IsBlockMovementValid({caller}): Grid.GetBlocks() returned null!"
                    : $"PlayArea.IsBlockMovementValid({caller}): Could not get block position, gridCount={gridBlocks.Length}");

                return false;
            }

            var targetX = x + dX;
            var targetY = y + dY;
            return IsIndexInBounds(targetX, targetY) && IsLocationAvailable(targetX, targetY);
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

            IncrementGoalProgress(linesToRemove.Count);

            // Remove all the cleared lines at once
            var lines = linesToRemove.ToIntArray();
            ClearLines(lines);

            if (lines.Length > 0 && NotifyEventsTo != null)
            {
                Debug.Log("PlayArea.HandleLineClears: Sending cleared lines to listeners.");
                LastLinesCleared = lines.Length;
                NotifyEventsTo.SendCustomEvent("PlayAreaOnClearedLines");
            }
        }

        private void IncrementGoalProgress(int lines)
        {
            goalProgress += lines;

            while (goalProgress >= 10)
            {
                level++;
                goalProgress -= 10;
                gravityPerTick = GetGravityPerTick(level);
                Debug.Log($"PlayArea.IncrementGoalProgress: Fall speed set to {GetFallSpeed(level)}");
            }
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
                var targetX = bottomLeftX + localX;
                var targetY = bottomLeftY + localY;
                var existing = Grid[targetX, targetY];
                if (existing != null)
                {
                    Debug.LogWarning($"PlayArea.CopyBlocksFromGroup: Overwriting block at {targetX}, {targetY}");
                    Destroy(existing.gameObject);
                }

                Grid[targetX, targetY] = group[localX, localY];
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

        private void ReplicateControlledGroupToGhost()
        {
            if (controlledBlockGroup == null) return;

            if (!PaletteHelpers.TryGetColor(palette, controlledBlockGroup.Type, out var color))
            {
                Debug.LogError(
                    $"PlayArea.ReplicateControlledGroupAsGhost: Failed to get color for shape: {controlledBlockGroup.Type}");
            }

            if (ghostPiece != null) Destroy(ghostPiece.gameObject);

            var gp = ghostPiece = BlockFactory.CreateShape(controlledBlockGroup.Type, color);
            gp.transform.SetParent(transform);
            gp.transform.SetLocalPositionAndRotation(controlledBlockGroup.transform.localPosition,
                gp.transform.localRotation);
            gp.SetOrientation(controlledBlockGroup.Orientation);
            gp.EnableGhostMode();

            gp.Translate(new Vector2(0, AllowedMaximumDrop()));
        }

        private int AllowedMaximumDrop()
        {
            if (controlledBlockGroup == null) return 0;

            var currY = Height;
            foreach (var block in controlledBlockGroup.GetBlocks())
            {
                if (!Grid.TryGetPosition(block, out var x, out var y)) continue;
                if (y < currY) currY = y;
            }

            var dY = 0;
            for (var y = 0; y >= -currY; y--)
            {
                if (!IsGroupMovementValid(controlledBlockGroup, 0, y))
                {
                    break;
                }

                dY = y;
            }

            return dY;
        }

        private static bool IsIndexInBounds(int x, int y)
        {
            return y >= 0 && y < Height && x >= 0 && x < Width;
        }

        private static double GetGravityPerTick(int level = 1)
        {
            return 1 / GetFallSpeed(level) / 60;
        }

        private static double GetFallSpeed(int level = 1)
        {
            return Math.Pow(0.8 - (level - 1) * 0.007, level - 1);
        }
    }
}