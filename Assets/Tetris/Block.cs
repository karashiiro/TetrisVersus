using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class Block : UdonSharpBehaviour
    {
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
    }
}