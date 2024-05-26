namespace Tetris.Blocks
{
    public enum BlockState
    {
        AtRest,
        Controlled,
        Held,
        NetworkLimit = 0b00000111,
    }
}