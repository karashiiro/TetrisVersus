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
            if (!group.TryGetValue(key, TokenType.Reference, out var block)) return null;
            return (Block)block.Reference;
        }

        public bool TryGetPosition(Block block, out int localX, out int localY)
        {
            localX = -1;
            localY = -1;

            var blockToken = new DataToken(block);
            if (groupPositions.TryGetValue(blockToken, TokenType.String, out var positionToken))
            {
                DecodePosition(positionToken, out localX, out localY);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the state of all blocks in the group.
        /// </summary>
        /// <param name="state">The desired block state.</param>
        public void SetState(BlockState state)
        {
            foreach (var token in group.GetValues().ToArray())
            {
                var block = (Block)token.Reference;
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
            transform.Translate(new Vector3(translation.x, translation.y));
        }

        /// <summary>
        /// Sets the color of the blocks in the group.
        /// </summary>
        /// <param name="color">The color to set the blocks to.</param>
        public void SetColor(Color color)
        {
            foreach (var token in group.GetValues().ToArray())
            {
                var block = (Block)token.Reference;
                block.SetColor(color);
            }
        }

        /// <summary>
        /// Retrieves the encoded positions of all blocks within the group. Use <see cref="DecodePosition"/>
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
                blocks[i] = (Block)tokens[i].Reference;
            }

            return blocks;
        }

        /// <summary>
        /// Decodes the provided encoded block position into raw positional values, relative to the group.
        /// </summary>
        /// <param name="position">The encoded block position.</param>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        public static void DecodePosition(DataToken position, out int localX, out int localY)
        {
            var parts = position.String.Split(',');
            localX = int.Parse(parts[0]);
            localY = int.Parse(parts[1]);
        }

        private static DataToken Key(int localX, int localY)
        {
            return new DataToken($"{localX},{localY}");
        }
    }
}