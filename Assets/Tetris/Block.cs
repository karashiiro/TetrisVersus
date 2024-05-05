using JetBrains.Annotations;
using UdonSharp;

namespace Tetris
{
    public class Block : UdonSharpBehaviour
    {
        public BlockState State { get; set; }
        [CanBeNull] public BlockGroup Group { get; set; }
    }
}