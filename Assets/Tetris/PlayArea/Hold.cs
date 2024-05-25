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

        public int DeserializeFrom(byte[] buffer, int offset, Transform parent, BlockFactory blockFactory,
            DataDictionary palette)
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
                Exchange(ref group, parent, BlockState.AtRest);
            }

            return RequiredNetworkBufferSize;
        }

        /// <summary>
        /// Exchanges the provided block group with the block group currently stored in the hold.
        /// </summary>
        /// <param name="group">The block group to exchange.</param>
        /// <param name="parent"></param>
        /// <param name="newState"></param>
        public void Exchange([CanBeNull] ref BlockGroup group, [NotNull] Transform parent, BlockState newState)
        {
            if (current == null && group == null) return;

            if (current == null && group != null)
            {
                ClaimGroup(group);
                current = group;
                group = null;
            }
            else if (current != null && group == null)
            {
                ReleaseCurrent(parent, newState);
                group = current;
                current = null;
            }
            else if (current != null && group != null)
            {
                ClaimGroup(group);
                ReleaseCurrent(parent, newState);

                // ReSharper disable once SwapViaDeconstruction
                var last = current;
                current = group;
                group = last;
            }
        }

        private void ClaimGroup([NotNull] BlockGroup group)
        {
            group.transform.SetParent(transform, false);
            group.SetPosition(new Vector2(0, 0));
            group.SetOrientation(Orientation.Origin);
            group.SetState(BlockState.Held);
        }

        private void ReleaseCurrent([NotNull] Transform parent, BlockState newState)
        {
            Debug.Assert(current != null);
            current.transform.SetParent(parent, false);
            current.SetState(newState);
        }
    }
}