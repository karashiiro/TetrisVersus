using System;
using JetBrains.Annotations;
using Tetris.Blocks;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRCExtensions;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Queue : UdonSharpBehaviour
    {
        private const int QueueSize = 5;

        private const int RequiredNetworkBufferSizePerGroup = 1;
        public const int RequiredNetworkBufferSize = QueueSize * RequiredNetworkBufferSizePerGroup;

        private readonly BlockGroup[] incoming = new BlockGroup[QueueSize];

        private int head;
        private int tail;
        private int count;

        public bool IsFull => incoming[tail] != null;
        public bool IsEmpty => incoming[head] == null;

        public int SerializeInto(byte[] buffer, int offset)
        {
            buffer[offset] = Convert.ToByte(head);
            buffer[offset + 1] = Convert.ToByte(tail);
            buffer[offset + 2] = Convert.ToByte(count);

            var nWritten = 0;
            var i = head;
            for (var j = 0; j < count; j++)
            {
                buffer[offset + nWritten++] = Convert.ToByte(incoming[i].Type);
                i = GetNextIndex(i);
            }

            return RequiredNetworkBufferSize;
        }

        public int DeserializeFrom(byte[] buffer, int offset, BlockFactory blockFactory, DataDictionary palette)
        {
            Clear();

            var nRead = 0;
            for (var i = 0; i < QueueSize; i++)
            {
                var shapeType = (ShapeType)Convert.ToInt32(buffer[offset + nRead]);
                if (shapeType != ShapeType.None)
                {
                    if (!palette.TryGetValue(shapeType.GetToken(), TokenType.Reference, out var colorToken))
                    {
                        Debug.LogError($"Queue.DeserializeFrom: Failed to get color for shape: {shapeType}");
                        colorToken = new DataToken(PaletteHelpers.DefaultColor());
                    }

                    var group = blockFactory.CreateShape(shapeType, colorToken.As<Color>());
                    Push(group);
                }

                nRead += RequiredNetworkBufferSizePerGroup;

                i = GetNextIndex(i);
            }

            return RequiredNetworkBufferSize;
        }

        public void Clear()
        {
            head = 0;
            tail = 0;
            count = 0;

            for (var n = 0; n < QueueSize; n++)
            {
                if (incoming[n] != null)
                {
                    Destroy(incoming[n].gameObject);
                    incoming[n] = null;
                }
            }
        }

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