using System;
using UnityEngine;

namespace VectorExtensions
{
    public static class Vector2Extensions
    {
        public static Vector2 Rotate(this Vector2 vector, float angleDegrees)
        {
            var angleRadians = angleDegrees * Math.PI / 180;
            var x = Convert.ToInt32(vector.x * Math.Cos(angleRadians) - vector.y * Math.Sin(angleRadians));
            var y = Convert.ToInt32(vector.x * Math.Sin(angleRadians) + vector.y * Math.Cos(angleRadians));
            return new Vector2(x, y);
        }
    }
}