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
        private int count;

        public bool IsFull => incoming[tail] != null;
        public bool IsEmpty => incoming[head] == null;

        [CanBeNull]
        public BlockGroup Pop(Transform parent)
        {
            if (IsEmpty) return null;

            var next = incoming[head];
            incoming[head] = null;
            head = GetNextIndex(head);
            count--;

            next.transform.SetParent(parent);
            UpdatePositions();
            return next;
        }

        public bool Push(BlockGroup group)
        {
            if (IsFull) return false;

            incoming[tail] = group;
            tail = GetNextIndex(tail);
            count++;

            group.transform.SetParent(transform, false);
            UpdatePositions();
            return true;
        }

        private void UpdatePositions()
        {
            Debug.Log($"Queue.UpdatePositions: head={head}, tail={tail}");

            var i = head;
            for (var j = 0; j < count; j++)
            {
                incoming[i].SetPosition(new Vector2(0, -j * 3));
                i = GetNextIndex(i);
            }
        }

        private int GetNextIndex(int currentIndex)
        {
            return ++currentIndex % incoming.Length;
        }
    }
}