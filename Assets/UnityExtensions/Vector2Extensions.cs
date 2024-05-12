using System;
using UnityEngine;

namespace UnityExtensions
{
    public static class Vector2IntExtensions
    {
        public static Vector2Int Rotate(this Vector2Int vector, float angleDegrees)
        {
            var angleRadians = angleDegrees * Math.PI / 180;
            var x = Convert.ToInt32(vector.x * Math.Cos(angleRadians) - vector.y * Math.Sin(angleRadians));
            var y = Convert.ToInt32(vector.x * Math.Sin(angleRadians) + vector.y * Math.Cos(angleRadians));
            return new Vector2Int(x, y);
        }
    }
}