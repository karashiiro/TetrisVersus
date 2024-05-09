using UnityEngine;
using VRC.SDK3.Data;

namespace UnityExtensions
{
    public static class ColorExtensions
    {
        public static DataToken GetToken(this Color color)
        {
            return new DataToken(color);
        }
    }
}