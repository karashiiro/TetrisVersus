using UnityEngine;

namespace Tetris
{
    public static class ShapeTypeHelpers
    {
        public static ShapeType GetRandom()
        {
            return (ShapeType)Random.Range((int)ShapeType.MinValue, (int)ShapeType.MaxValue + 1);
        }
    }
}