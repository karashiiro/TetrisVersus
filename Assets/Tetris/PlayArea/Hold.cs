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
    public class Hold : UdonSharpBehaviour
    {
        public const int RequiredNetworkBufferSize = 1;

        [CanBeNull] private BlockGroup current;

        public void Clear(BlockFactory blockFactory)
        {
            if (current != null)
            {
                blockFactory.ReturnBlockGroup(current);
                current = null;
            }
        }

        public int SerializeInto(byte[] buffer, int offset)
        {
            if (current != null)
            {
                buffer[offset] = Convert.ToByte(current.Type);
            }

            return RequiredNetworkBufferSize;
        }

        public int DeserializeFrom(byte[] buffer, int offset, BlockFactory blockFactory, DataDictionary palette)
        {
            Clear(blockFactory);

            var shapeType = (ShapeType)Convert.ToInt32(buffer[offset]);
            if (shapeType != ShapeType.None)
            {
                if (!palette.TryGetValue(shapeType.GetToken(), TokenType.Reference, out var colorToken))
                {
                    Debug.LogError($"Hold.DeserializeFrom: Failed to get color for shape: {shapeType}");
                    colorToken = new DataToken(PaletteHelpers.DefaultColor());
                }

                var group = blockFactory.CreateShape(shapeType, colorToken.As<Color>());
                Exchange(ref group, BlockState.AtRest);
            }

            return RequiredNetworkBufferSize;
        }

        /// <summary>
        /// Exchanges the provided block group with the block group currently stored in the hold.
        /// </summary>
        /// <param name="group">The block group to exchange.</param>
        /// <param name="newState"></param>
        public void Exchange(ref BlockGroup group, BlockState newState)
        {
            var parent = group.transform.parent;
            var last = current;
            current = group;
            current.transform.SetParent(transform, false);
            current.SetPosition(new Vector2(0, 0));
            current.SetOrientation(Orientation.Origin);
            current.SetState(BlockState.Held);
            group = last;

            if (group != null)
            {
                group.transform.SetParent(parent, false);
                group.SetState(newState);
            }
        }
    }
}