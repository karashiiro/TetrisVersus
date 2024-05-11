using JetBrains.Annotations;
using Tetris.Blocks;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Queue : UdonSharpBehaviour
    {
        private const int QueueSize = 5;

        private readonly BlockGroup[] incoming = new BlockGroup[QueueSize];

        private int head;
        private int tail;

        public bool IsFull => incoming[tail] != null;
        public bool IsEmpty => incoming[head] == null;

        [CanBeNull]
        public BlockGroup Pop(Transform parent)
        {
            if (IsEmpty) return null;
            var next = incoming[head];
            incoming[head] = null;
            head = GetNextIndex(head);
            next.transform.SetParent(parent);
            return next;
        }

        public bool Push(BlockGroup group)
        {
            if (IsFull) return false;
            incoming[tail] = group;
            tail = GetNextIndex(tail);
            group.transform.SetParent(transform);
            return true;
        }

        private int GetNextIndex(int currentIndex)
        {
            return ++currentIndex % incoming.Length;
        }
    }
}