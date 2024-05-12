using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace Tetris
{
    /// <summary>
    /// Game logic controller for a single player. In networked configurations, this is responsible for
    /// serializing all of a player's game state for syncing.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TetrisGame : UdonSharpBehaviour
    {
        [UdonSynced] private readonly byte[] networkState = new byte[PlayArea.RequiredNetworkBufferSize];

        private VRCPlayerApi Player { get; set; }

        [field: SerializeField] public PlayArea PlayArea { get; set; }

        private void Awake()
        {
            // Set the owner to the first player in the instance for now
            Player = VRCPlayerApi.GetPlayerById(1);
            Player.Immobilize(true);
            Player.SetJumpImpulse(0);
        }

        public override void PostLateUpdate()
        {
            if (PlayArea.ShouldSerialize())
            {
                RequestSerialization();
            }
        }

        public override void OnPreSerialization()
        {
            PlayArea.SerializeInto(networkState);
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

        public override void InputDrop(bool value, UdonInputEventArgs args)
        {
            // TODO: Use a different event for VR
            if (!value) return;
            PlayArea.ExchangeHold();
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