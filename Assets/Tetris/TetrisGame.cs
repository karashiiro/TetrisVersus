using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Tetris
{
    public class TetrisGame : UdonSharpBehaviour
    {
        private VRCPlayerApi Player { get; set; }

        [field: SerializeField] public PlayArea PlayArea { get; set; }

        public void SetOwningPlayer(VRCPlayerApi player)
        {
            Player = player;
        }

        private void Awake()
        {
            // Set the owner to the first player in the instance for now
            Player = VRCPlayerApi.GetPlayerById(1);
            Player.Immobilize(true);
            Player.SetJumpImpulse(0);
        }

        /// <summary>
        /// Block rotations, controlled with the controller triggers.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!value) return;

            if (args.handType == HandType.LEFT)
            {
                PlayArea.RotateControlledGroupLeft();
            }
            else
            {
                PlayArea.RotateControlledGroupRight();
            }
        }

        /// <summary>
        /// Hard drop.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!value) return;
            PlayArea.HardDrop();
        }

        /// <summary>
        /// Horizontal block movement - supports left/right controls.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            var sign = Math.Sign(value);
            if (sign == 0)
            {
                PlayArea.DisableAutoRepeat();
                return;
            }

            PlayArea.MoveControlledGroup(sign, 0);
            PlayArea.PrepareAutoRepeat(sign == -1 ? AutoRepeatDirection.Left : AutoRepeatDirection.Right);
        }

        /// <summary>
        /// Vertical block movement - only supports dropping a block down.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            const int down = -1;

            var direction = Math.Sign(value);
            PlayArea.SoftDrop(direction == down);
        }
    }
}