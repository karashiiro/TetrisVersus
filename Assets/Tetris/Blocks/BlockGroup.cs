﻿using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityExtensions;
using VRC.SDK3.Data;
using VRCExtensions;

namespace Tetris.Blocks
{
    /// <summary>
    /// A group of blocks. Groups of blocks can be moved together by repositioning the transform that
    /// this behavior is attached to, without moving each block individually.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class BlockGroup : UdonSharpBehaviour
    {
        private readonly DataDictionary group = new DataDictionary();
        private readonly DataDictionary groupPositions = new DataDictionary();

        private bool shouldRequestSerialization;

        public Orientation Orientation { get; private set; }

        /// <summary>
        /// The group's shape type. Note that this does not reflect the number of blocks remaining in the
        /// group (in the event that some were cleared). This is only used for SRS.
        /// </summary>
        public ShapeType Type { get; set; }

        [CanBeNull]
        public Block this[int x, int y]
        {
            get => Get(x, y);
            set
            {
                if (value == null)
                {
                    Remove(x, y);
                }
                else
                {
                    Add(value, x, y);
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            // Clean up block groups when all of their children are destroyed
            if (transform.childCount == 0)
            {
                Destroy(gameObject);
            }
        }

        public bool ShouldSerialize()
        {
            return shouldRequestSerialization;
        }

        public void SerializeInto(byte[] buffer)
        {
            // TODO: stub
            shouldRequestSerialization = false;
        }

        /// <summary>
        /// Adds a block to the block group.
        /// </summary>
        /// <param name="block">The block to add to the group.</param>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        public void Add(Block block, int localX, int localY)
        {
            if (block == null) return;

            // Encode the group-local position in the dictionary key
            var key = Key(localX, localY);
            var value = block.Token;
            group.SetValue(key, value);
            groupPositions.SetValue(value, key);
            shouldRequestSerialization = true;
        }

        /// <summary>
        /// Removes a block from the block group.
        /// </summary>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        public void Remove(int localX, int localY)
        {
            var key = Key(localX, localY);
            if (!group.Remove(key, out var block)) return;
            groupPositions.Remove(block);
            shouldRequestSerialization = true;
        }

        /// <summary>
        /// Retrieves a block from the block group. Does not remove the block from the group.
        /// </summary>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        /// <returns>The retrieved block.</returns>
        [CanBeNull]
        public Block Get(int localX, int localY)
        {
            var key = Key(localX, localY);
            return group.TryGetValue(key, TokenType.Reference, out var block) ? block.As<Block>() : null;
        }

        public bool TryGetPosition([CanBeNull] Block block, out int localX, out int localY, string caller = "unknown")
        {
            localX = -1;
            localY = -1;

            if (block == null) return false;

            if (!groupPositions.TryGetValue(block.Token, TokenType.String, out var positionToken))
            {
                Debug.LogWarning($"TryGetPosition({caller}): Failed to get position token for block: {positionToken}");
                return false;
            }

            return TryDecodePosition(positionToken, out localX, out localY);
        }

        public bool TryGetPositionAbsolute([CanBeNull] Block block, out int localX, out int localY,
            string caller = "unknown")
        {
            localX = -1;
            localY = -1;

            if (block == null) return false;

            if (!groupPositions.TryGetValue(block.Token, TokenType.String, out var positionToken))
            {
                Debug.LogWarning(
                    $"TryGetPositionAbsolute({caller}): Failed to get position token for block: {positionToken}");
                return false;
            }

            return TryDecodePositionAbsolute(positionToken, out localX, out localY);
        }

        /// <summary>
        /// Sets this group as the parent of all uncontrolled blocks stored within it, and updates
        /// their positions accordingly.
        /// </summary>
        public void ClaimUncontrolledBlocks()
        {
            foreach (var token in group.GetValues().ToArray())
            {
                var block = token.As<Block>();
                if (block.State == BlockState.Controlled) continue;

                block.transform.SetParent(transform, true);

                if (TryGetPosition(block, out var x, out var y, caller: nameof(ClaimUncontrolledBlocks)))
                {
                    block.SetPosition(new Vector2(x, y));
                }
            }

            shouldRequestSerialization = true;
        }

        /// <summary>
        /// Sets the state of all blocks in the group.
        /// </summary>
        /// <param name="state">The desired block state.</param>
        public void SetState(BlockState state)
        {
            foreach (var token in group.GetValues().ToArray())
            {
                var block = token.As<Block>();
                block.State = state;
            }

            shouldRequestSerialization = true;
        }

        /// <summary>
        /// Sets the position of the entire group of blocks according to the provided position vector.
        /// All involved GameObjects should have a scale of 1 relative to their parents.
        /// </summary>
        /// <param name="position">The position vector.</param>
        public void SetPosition(Vector2 position)
        {
            transform.SetLocalPositionAndRotation(new Vector3(position.x, position.y), Quaternion.identity);
        }

        /// <summary>
        /// Translates the entire group of blocks according to the provided translation vector.
        /// All involved GameObjects should have a scale of 1 relative to their parents.
        /// </summary>
        /// <param name="translation">The translation vector.</param>
        public void Translate(Vector2 translation)
        {
            var rotatedTranslation = Quaternion.AngleAxis(Orientation.Origin.AngleTo(Orientation), Vector3.forward) *
                                     new Vector3(translation.x, translation.y);
            transform.Translate(rotatedTranslation);
        }

        /// <summary>
        /// Rotates the entire group.
        /// </summary>
        /// <param name="rotation">The rotation direction.</param>
        public void Rotate(Rotation rotation)
        {
            transform.Rotate(Vector3.forward, rotation.AsDegrees(), Space.Self);
            Orientation = Orientation.Rotate(rotation);
        }

        public void SetOrientation(Orientation orientation)
        {
            transform.Rotate(Vector3.forward, orientation.AsDegrees(), Space.Self);
            Orientation = orientation;
        }

        /// <summary>
        /// Sets the color of the blocks in the group.
        /// </summary>
        /// <param name="color">The color to set the blocks to.</param>
        public void SetColor(Color color)
        {
            foreach (var token in group.GetValues().ToArray())
            {
                var block = token.As<Block>();
                block.SetColor(color);
            }

            shouldRequestSerialization = true;
        }

        /// <summary>
        /// Retrieves the encoded positions of all blocks within the group. Use <see cref="TryDecodePosition"/>
        /// to retrieve the decoded block positions.
        /// </summary>
        /// <returns>The encoded block positions.</returns>
        public DataToken[] GetEncodedPositions()
        {
            return group.GetKeys().ToArray();
        }

        /// <summary>
        /// Retrieves the encoded positions of all blocks provided. Use <see cref="TryDecodePosition"/>
        /// to retrieve the decoded block positions. All provided blocks are expected to be in the group.
        /// </summary>
        /// <param name="blocks">The blocks to get positions for.</param>
        /// <returns></returns>
        public DataToken[] GetEncodedPositionsForBlocks(Block[] blocks)
        {
            var positions = new DataToken[blocks.Length];
            for (var i = 0; i < blocks.Length; i++)
            {
                positions[i] = groupPositions[blocks[i].Token];
                if (!groupPositions.TryGetValue(blocks[i].Token, TokenType.String, out var position))
                {
                    Debug.LogError($"GetEncodedPositionsForBlocks: Failed to decode block position: {position}");
                }

                positions[i] = position;
            }

            return positions;
        }

        public Block[] GetBlocks()
        {
            var tokens = group.GetValues().ToArray();
            var blocks = new Block[tokens.Length];
            for (var i = 0; i < blocks.Length; i++)
            {
                blocks[i] = tokens[i].As<Block>();
            }

            return blocks;
        }

        /// <summary>
        /// Decodes the provided encoded block position into raw positional values, relative to the group.
        /// </summary>
        /// <param name="position">The encoded block position.</param>
        /// <param name="localX">The block's x-position, relative to the group.</param>
        /// <param name="localY">The block's y-position, relative to the group.</param>
        /// <returns></returns>
        public bool TryDecodePosition(DataToken position, out int localX, out int localY)
        {
            var parts = position.String.Split(',');
            var xParsed = int.TryParse(parts[0], out localX);
            var yParsed = int.TryParse(parts[1], out localY);
            return xParsed && yParsed;
        }

        /// <summary>
        /// Decodes the provided encoded block position into raw positional values, relative to the world.
        /// </summary>
        /// <param name="position">The encoded block position.</param>
        /// <param name="x">The block's x-position, relative to the world.</param>
        /// <param name="y">The block's y-position, relative to the world.</param>
        /// <returns></returns>
        public bool TryDecodePositionAbsolute(DataToken position, out int x, out int y)
        {
            x = y = -1;
            if (!TryDecodePosition(position, out var localX, out var localY)) return false;
            var adjusted = new Vector2(localX, localY).Rotate(Orientation.AsDegrees());
            x = Convert.ToInt32(adjusted.x);
            y = Convert.ToInt32(adjusted.y);
            return true;
        }

        private static DataToken Key(int localX, int localY)
        {
            return new DataToken($"{localX},{localY}");
        }
    }
}