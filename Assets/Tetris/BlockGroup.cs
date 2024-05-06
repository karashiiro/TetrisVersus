using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
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