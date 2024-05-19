using System;
using Tetris.Timers;
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

        private bool rotateLeftHeld;
        private bool rotateRightHeld;

        [UdonSynced] private GameState currentState;

        [field: SerializeField] public PlayArea PlayArea { get; set; }
        [field: SerializeField] public TickDriver TickDriver { get; set; }

        private void Awake()
        {
            if (PlayArea == null) Debug.LogError("TetrisGame.Awake: PlayArea is null.");
            if (TickDriver == null) Debug.LogError("TetrisGame.Awake: TickDriver is null.");
        }

        public override void Interact()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("TetrisGame.Interact: Already owner, skipping ownership request");
                InitGame(VRCPlayerApi.GetPlayerById(1));
                return;
            }

            // Interact is always triggered by the local player
            Debug.Log("TetrisGame.Interact: Requesting ownership of game");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // OnOwnershipRequest is triggered by the requesting player (who should always be the interact player?)
            // The current owner still needs to approve the request after this event resolves.
            var shouldRequest = currentState == GameState.NotStarted;
            Debug.Log($"TetrisGame.OnOwnershipRequest: shouldRequest={shouldRequest}");

            // Unfreeze the player if they were the owner and are having ownership reassigned
            if (shouldRequest && Networking.IsOwner(gameObject))
            {
                // TODO: Make this not break for the instance owner
                Networking.LocalPlayer.Immobilize(false);
                Networking.LocalPlayer.SetJumpImpulse();
            }

            return shouldRequest;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            // Before transfer approval, this is invoked by the requesting player. Ownership can still be transferred
            // back to the original owner if the request is rejected (this is still local to the requester). However,
            // after transfer approval, this is invoked by all players except for the requester. We need to assume
            // that the request succeeds, but be able to roll back safely if it doesn't.
            InitGame(player);
        }

        private void InitGame(VRCPlayerApi player)
        {
            Debug.Log($"TetrisGame.Start: Set owner to {player.displayName}");
            if (player.isLocal)
            {
                Debug.Log("TetrisGame.Start: Owner is local player");
                PlayArea.SetOwned(true);

                player.Immobilize(true);
                player.SetJumpImpulse(0);
            }

            SetGameState(GameState.Playing);
        }

        private void SetGameState(GameState nextState)
        {
            switch (nextState)
            {
                case GameState.Playing:
                    TickDriver.enabled = true;
                    break;
                case GameState.Stopped:
                    TickDriver.enabled = false;
                    break;
            }
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
            PlayArea.SerializeInto(networkState, 0);
        }

        public override void OnDeserialization()
        {
            PlayArea.DeserializeFrom(networkState, 0);
        }

        /// <summary>
        /// Block rotations, controlled with the controller triggers.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!Networking.IsOwner(gameObject)) return;

            // For some reason, InputUse fires twice on KBM inputs, unlike in the editor. We use some
            // additional state to avoid re-rotating a block until the rotate button is released.
            if (args.handType == HandType.LEFT)
            {
                if (!rotateLeftHeld && value)
                {
                    PlayArea.RotateControlledGroupLeft();
                    rotateLeftHeld = true;
                }
                else if (!value)
                {
                    rotateLeftHeld = false;
                }
            }
            else
            {
                if (!rotateRightHeld && value)
                {
                    PlayArea.RotateControlledGroupRight();
                    rotateRightHeld = true;
                }
                else if (!value)
                {
                    rotateRightHeld = false;
                }
            }
        }

        public override void InputDrop(bool value, UdonInputEventArgs args)
        {
            if (!Networking.IsOwner(gameObject)) return;

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
            if (!Networking.IsOwner(gameObject)) return;

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
            if (!Networking.IsOwner(gameObject)) return;

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
            if (!Networking.IsOwner(gameObject)) return;

            const int down = -1;

            var direction = Math.Sign(value);
            PlayArea.SoftDrop(direction == down);
        }
    }
}