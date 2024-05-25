using System;
using JetBrains.Annotations;
using Tetris.Blocks;
using Tetris.VRCExtensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.PlayArea
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Queue : UdonSharpBehaviour
    {
        public const int Capacity = 5;

        private const int RequiredNetworkBufferSizePerGroup = 1;
        public const int RequiredNetworkBufferSize = Capacity * RequiredNetworkBufferSizePerGroup;

        private readonly BlockGroup[] incoming = new BlockGroup[Capacity];

        private int head;
        private int tail;

        public bool IsFull => incoming[tail] != null;
        public bool IsEmpty => incoming[head] == null;
        public int Count { get; private set; }

        public int SerializeInto(byte[] buffer, int offset)
        {
            buffer[offset] = Convert.ToByte(head);
            buffer[offset + 1] = Convert.ToByte(tail);
            buffer[offset + 2] = Convert.ToByte(Count);

            var nWritten = 0;
            var i = head;
            for (var j = 0; j < Count; j++)
            {
                buffer[offset + nWritten++] = Convert.ToByte(incoming[i].Type);
                i = GetNextIndex(i);
            }

            return RequiredNetworkBufferSize;
        }

        public int DeserializeFrom(byte[] buffer, int offset, BlockFactory blockFactory, DataDictionary palette)
        {
            Clear(blockFactory);

            var nRead = 0;
            for (var i = 0; i < Capacity; i++)
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

        public void Clear(BlockFactory blockFactory)
        {
            head = 0;
            tail = 0;
            Count = 0;

            for (var n = 0; n < Capacity; n++)
            {
                if (incoming[n] != null)
                {
                    blockFactory.ReturnBlockGroup(incoming[n]);
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
            Count--;

            next.transform.SetParent(parent);
            UpdatePositions();
            return next;
        }

        public bool Push(BlockGroup group)
        {
            if (IsFull) return false;

            incoming[tail] = group;
            tail = GetNextIndex(tail);
            Count++;

            group.transform.SetParent(transform, false);
            UpdatePositions();
            return true;
        }

        private void UpdatePositions()
        {
            var i = head;
            for (var j = 0; j < Count; j++)
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