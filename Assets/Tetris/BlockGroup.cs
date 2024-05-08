using DataTokenExtensions;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris
{
    public class BlockGroup : UdonSharpBehaviour
    {
        private readonly DataDictionary group = new DataDictionary();
        private readonly DataDictionary groupPositions = new DataDictionary();

        private float orientation;

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
            var value = new DataToken(block);
            group.SetValue(key, value);
            groupPositions.SetValue(value, key);
        }

        /// <summary>
        /// Removes a block from the block group.
        /// </summary>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        public void Remove(int localX, int localY)
        {
            var key = Key(localX, localY);
            if (group.Remove(key, out var block))
            {
                groupPositions.Remove(block);
            }
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

        public bool TryGetPosition(Block block, out int localX, out int localY)
        {
            localX = -1;
            localY = -1;

            var blockToken = new DataToken(block);
            return groupPositions.TryGetValue(blockToken, TokenType.String, out var positionToken) &&
                   TryDecodePosition(positionToken, out localX, out localY);
        }

        public void GetBounds(out int minX, out int minY, out int maxX, out int maxY)
        {
            // Set everything to 0 by default
            minX = minY = maxX = maxY = 0;

            // Find the bounds by iterating over the known positions
            foreach (var pos in GetEncodedPositions())
            {
                if (!TryDecodePosition(pos, out var x, out var y)) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
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
            var rotatedTranslation = Quaternion.AngleAxis(-orientation, Vector3.forward) *
                                     new Vector3(translation.x, translation.y);
            transform.Translate(rotatedTranslation);
        }

        /// <summary>
        /// Rotates the entire group of blocks according to the provided angle.
        /// </summary>
        /// <param name="angle"></param>
        public void Rotate(float angle)
        {
            transform.Rotate(Vector3.forward, angle, Space.Self);
            orientation = (orientation + angle) % 360;
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
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        /// <returns></returns>
        public static bool TryDecodePosition(DataToken position, out int localX, out int localY)
        {
            var parts = position.String.Split(',');
            var xParsed = int.TryParse(parts[0], out localX);
            var yParsed = int.TryParse(parts[1], out localY);
            return xParsed && yParsed;
        }

        private static DataToken Key(int localX, int localY)
        {
            return new DataToken($"{localX},{localY}");
        }
    }
}