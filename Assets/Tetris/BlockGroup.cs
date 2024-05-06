﻿using JetBrains.Annotations;
using UdonSharp;
using VRC.SDK3.Data;

namespace Tetris
{
    public class BlockGroup : UdonSharpBehaviour
    {
        private readonly DataDictionary group = new DataDictionary();

        [CanBeNull]
        public Block this[int x, int y]
        {
            get => Get(x, y);
            set => Add(value, x, y);
        }

        /// <summary>
        /// Adds a block to the block group.
        /// </summary>
        /// <param name="block">The block to add to the group.</param>
        /// <param name="localX">The block's x-position, local to the group.</param>
        /// <param name="localY">The block's y-position, local to the group.</param>
        public void Add(Block block, int localX, int localY)
        {
            // Encode the group-local position in the dictionary key
            var key = Key(localX, localY);
            var value = new DataToken(block);
            group.Add(key, value);
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
            return (Block)group[key].Reference;
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