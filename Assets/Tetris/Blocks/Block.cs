using System;
using UdonSharp;
using UnityEngine;
using UnityExtensions;
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
        public const int RequiredNetworkBufferSize = 2;

        private readonly int emission = Shader.PropertyToID("_EmissionColor");

        [field: SerializeField] public Renderer TargetRenderer { get; set; }
        [field: SerializeField] public Material GhostMaterial { get; set; }

        public BlockState State { get; set; }
        public ShapeType ShapeType { get; set; }

        public DataToken Token => new DataToken(this);

        private void Awake()
        {
            if (TargetRenderer == null)
            {
                TargetRenderer = GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Serializes this block into the provided buffer.
        /// </summary>
        /// <param name="buffer">The buffer to serialize into.</param>
        /// <param name="offset">The offset within the buffer to serialize the data at.</param>
        /// <returns>The number of bytes that were written.</returns>
        public int SerializeInto(byte[] buffer, int offset)
        {
            buffer[offset] = Convert.ToByte(State);
            buffer[offset] |= 0b10000000;
            buffer[offset + 1] = Convert.ToByte(ShapeType);
            return RequiredNetworkBufferSize;
        }

        public static int SerializeEmpty(byte[] buffer, int offset)
        {
            buffer[offset] = 0;
            return RequiredNetworkBufferSize;
        }

        public static bool ShouldDeserialize(byte[] buffer, int offset)
        {
            return (buffer[offset] & 0b10000000) >> 7 == 1;
        }

        public void DeserializeFrom(byte[] buffer, int offset)
        {
            State = (BlockState)Convert.ToInt32(buffer[offset] & 0b01111111);
            ShapeType = (ShapeType)Convert.ToInt32(buffer[offset + 1]);
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
        /// <param name="nextColor">The color to set the block to.</param>
        public void SetColor(Color nextColor)
        {
            if (TargetRenderer == null) return;
            TargetRenderer.material.color = nextColor;
            TargetRenderer.material.SetColor(emission, nextColor);
        }

        public void EnableGhostMode()
        {
            if (TargetRenderer == null) return;
            var originalColor = TargetRenderer.material.color;
            TargetRenderer.material = GhostMaterial;
            TargetRenderer.material.color = originalColor.WithAlpha(0.5f);
        }
    }
}