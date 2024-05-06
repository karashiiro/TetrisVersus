using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class Block : UdonSharpBehaviour
    {
        public BlockState State { get; set; }
        [CanBeNull] public BlockGroup Group { get; set; }

        public void Move(Vector2 movement)
        {
            var xDiff = movement.x / transform.localScale.x;
            var yDiff = movement.y / transform.localScale.y;
            transform.Translate(xDiff, yDiff, 0);
        }
    }
}