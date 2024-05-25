using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.UnityExtensions
{
    public static class ColorExtensions
    {
        public static DataToken GetToken(this Color color)
        {
            return new DataToken(color);
        }

        public static Color WithAlpha(this Color color, float value)
        {
            return new Color(color.r, color.g, color.b, value);
        }
    }
}