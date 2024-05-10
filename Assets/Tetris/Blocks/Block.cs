using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.Blocks
{
    public class Block : UdonSharpBehaviour
    {
        private readonly int ShaderColorKey = Shader.PropertyToID("_Color");

        public BlockState State { get; set; }

        public DataToken Token => new DataToken(this);

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
            var blockRenderer = GetComponent<Renderer>();
            if (blockRenderer == null) return;
            blockRenderer.material.SetColor(ShaderColorKey, color);
        }
    }
}