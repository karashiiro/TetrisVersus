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

        private void Start()
        {
            // Set the owner to the first player in the instance for now
            Player = VRCPlayerApi.GetPlayerById(1);
            Player.Immobilize(true);

            // Do a few ticks so we know things are working
            for (var i = 0; i < 70; i++)
            {
                Debug.Log($"Current tick: {i}");
                PlayArea.Tick();
            }
        }

        /// <summary>
        /// Horizontal block movement - supports left/right controls.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            var sign = Math.Sign(value);
            if (sign == 0) return;
            PlayArea.MoveControlledGroup(sign, 0);

            // TODO: Run ticks on a scheduler instead of here
            PlayArea.Tick();
        }

        /// <summary>
        /// Vertical block movement - only supports dropping a block down.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            var sign = Math.Sign(value);
            if (sign != -1) return;
            PlayArea.MoveControlledGroup(0, sign);

            // TODO: Run ticks on a scheduler instead of here
            PlayArea.Tick();
        }
    }
}