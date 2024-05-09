using VRC.SDK3.Data;

namespace Tetris
{
    public static class ShapeTypeExtensions
    {
        public static DataToken GetToken(this ShapeType type)
        {
            return new DataToken((int)type);
        }
    }
}