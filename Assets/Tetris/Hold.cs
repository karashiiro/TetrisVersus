using JetBrains.Annotations;
using Tetris.Blocks;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Hold : UdonSharpBehaviour
    {
        [CanBeNull] private BlockGroup current;

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