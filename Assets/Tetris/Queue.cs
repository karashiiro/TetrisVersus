using UdonSharp;

namespace Tetris
{
    public class Queue : UdonSharpBehaviour
    {
        private const int QueueSize = 5;

        private readonly BlockGroup[] incoming = new BlockGroup[QueueSize];

        private int nextIndex = 0;

        public BlockGroup Pop()
        {
            if (incoming[nextIndex] == null) return null;
            var next = incoming[nextIndex];
            incoming[nextIndex] = null;
            nextIndex = nextIndex++ % incoming.Length;
            return next;
        }
    }
}