namespace Tetris
{
    public static class AutoRepeatDirectionExtensions
    {
        public static int AsTranslation(this AutoRepeatDirection direction)
        {
            switch (direction)
            {
                case AutoRepeatDirection.Left:
                    return -1;
                case AutoRepeatDirection.Right:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}