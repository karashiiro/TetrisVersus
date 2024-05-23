using System.Globalization;
using Tetris.Blocks;
using UnityEngine;
using VRC.SDK3.Data;
using VRCExtensions;

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

        public static bool TryGetColor(DataDictionary palette, ShapeType shapeType, out Color color)
        {
            color = DefaultColor();
            if (!palette.TryGetValue(shapeType.GetToken(), TokenType.Reference, out var colorToken))
            {
                return false;
            }

            color = colorToken.As<Color>();
            return true;
        }
    }
}