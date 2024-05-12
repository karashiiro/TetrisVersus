using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.Blocks
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Block : UdonSharpBehaviour
    {
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
            return 1;
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