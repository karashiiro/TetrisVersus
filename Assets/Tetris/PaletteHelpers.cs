using System.Globalization;
using UnityEngine;

namespace Tetris
{
    public class PaletteHelpers
    {
        public static Color DefaultColor()
        {
            return Color.black;
        }

        public static Color FromHex(string color)
        {
            if (color.Length < 6) return DefaultColor();
            var colorClean = color.Substring(color.Length - 6);
            var r = int.Parse(colorClean.Substring(0, 2), NumberStyles.HexNumber) / 255f;
            var g = int.Parse(colorClean.Substring(2, 2), NumberStyles.HexNumber) / 255f;
            var b = int.Parse(colorClean.Substring(4, 2), NumberStyles.HexNumber) / 255f;
            return new Color(r, g, b);
        }
    }
}