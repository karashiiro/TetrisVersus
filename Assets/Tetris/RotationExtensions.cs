namespace Tetris
{
    public static class RotationExtensions
    {
        public static float AsDegrees(this Rotation rotation)
        {
            return rotation == Rotation.Left ? -90 : 90;
        }
    }
}