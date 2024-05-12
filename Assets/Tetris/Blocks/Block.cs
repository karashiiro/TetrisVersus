﻿using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.Blocks
{
    /// <summary>
    /// A single block (mino). Blocks only track their own internal state, and do not know anything
    /// about block groups they may be contained in. A block can be a member of multiple groups, but
    /// only one group should be responsible for mutating the GameObject this behavior is attached to.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Block : UdonSharpBehaviour
    {
        public const int RequiredNetworkBufferSize = 1;

        [field: SerializeField] public Renderer TargetRenderer { get; set; }

        public BlockState State { get; set; }

        public DataToken Token => new DataToken(this);

        /// <summary>
        /// Serializes this block into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to serialize into.</param>
        /// <param name="offset">The offset within the buffer to serialize the data at.</param>
        /// <returns>The number of bytes that were written.</returns>
        public int SerializeInto(byte[] buffer, int offset)
        {
            buffer[offset] = (byte)State;
            return RequiredNetworkBufferSize;
        }

        private void Awake()
        {
            if (TargetRenderer == null)
            {
                TargetRenderer = GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Sets the position of the block according to the provided position vector. The block should
        /// have a scale of 1 relative to its parent.
        /// </summary>
        /// <param name="position">The position vector.</param>
        public void SetPosition(Vector2 position)
        {
            transform.SetLocalPositionAndRotation(new Vector3(position.x, position.y), Quaternion.identity);
        }

        /// <summary>
        /// Sets the color of the block.
        /// </summary>
        /// <param name="color">The color to set the block to.</param>
        public void SetColor(Color color)
        {
            if (TargetRenderer == null) return;
            TargetRenderer.material.color = color;
        }
    }
}