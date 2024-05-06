using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class Queue : UdonSharpBehaviour
    {
        private const int QueueSize = 5;

        private readonly BlockGroup[] incoming = new BlockGroup[QueueSize];

        private int head;
        private int tail;

        [CanBeNull]
        public BlockGroup Pop()
        {
            if (incoming[head] == null) return null;
            var next = incoming[head];
            incoming[head] = null;
            head = GetNextIndex(head);
            return next;
        }

        public bool Push(BlockGroup group)
        {
            if (incoming[tail] != null) return false;
            incoming[tail] = group;
            tail = GetNextIndex(tail);
            return true;
        }

        private int GetNextIndex(int currentIndex)
        {
            return ++currentIndex % incoming.Length;
        }
    }
}