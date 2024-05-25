using System.Globalization;
using Tetris.Blocks;
using Tetris.VRCExtensions;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris
{
    public class PaletteHelpers
    {
        public static Color DefaultColor()
        {
            return Color.black;
        }

        public static DataDictionary DefaultPalette()
        {
            return new DataDictionary
            {
                { ShapeType.None.GetToken(), new DataToken(Color.grey) },
                { ShapeType.O.GetToken(), new DataToken(Color.yellow) },
                { ShapeType.I.GetToken(), new DataToken(Color.cyan) },
                { ShapeType.S.GetToken(), new DataToken(Color.green) },
                { ShapeType.Z.GetToken(), new DataToken(Color.red) },
                { ShapeType.T.GetToken(), new DataToken(Color.magenta) },
                { ShapeType.L.GetToken(), new DataToken(FromHex("ff7425")) },
                { ShapeType.J.GetToken(), new DataToken(Color.blue) },
            };
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