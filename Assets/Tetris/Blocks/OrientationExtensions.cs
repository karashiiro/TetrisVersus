namespace Tetris.Blocks
{
    public static class OrientationExtensions
    {
        private const int Limit = (int)Orientation.MaxValue + 1;

        public static Orientation Rotate(this Orientation orientation, Rotation rotation)
        {
            return rotation == Rotation.Left ? orientation.RotateLeft() : orientation.RotateRight();
        }

        public static Orientation RotateRight(this Orientation orientation)
        {
            return (Orientation)(((int)orientation + 1) % Limit);
        }

        public static Orientation RotateLeft(this Orientation orientation)
        {
            return (Orientation)(((int)orientation - 1 + Limit) % Limit);
        }

        public static float AsDegrees(this Orientation orientation)
        {
            return (int)orientation * 90;
        }
    }
}