using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class Block : UdonSharpBehaviour
    {
        private readonly int ShaderColorKey = Shader.PropertyToID("_Color");

        public BlockState State { get; set; }

        /// <summary>
        /// Translates the block according to the provided movement vector. The block should
        /// have a scale of 1 relative to its parent.
        /// </summary>
        /// <param name="movement">The movement vector.</param>
        public void Move(Vector2 movement)
        {
            transform.SetLocalPositionAndRotation(new Vector3(movement.x, movement.y), Quaternion.identity);
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