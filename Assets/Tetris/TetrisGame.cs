using System;
using Tetris.Timers;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using Random = UnityEngine.Random;

namespace Tetris
{
    /// <summary>
    /// Game logic controller for a single player. In networked configurations, this is responsible for
    /// serializing all of a player's game state for syncing.
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TetrisGame : UdonSharpBehaviour
    {
        private const float JoystickDeadZone = 0.7f;
        private const float TriggerDeadZone = 0.5f;

        [UdonSynced] private readonly byte[] networkState = new byte[Tetris.PlayArea.PlayArea.RequiredNetworkBufferSize];

        private bool moveLeftHeld;
        private bool moveRightHeld;
        private bool rotateLeftHeld;
        private bool rotateRightHeld;
        private bool hardDropHeld;

        [field: SerializeField] public UdonSharpBehaviour NotifyLineClearsTo { get; set; }
        [field: SerializeField] public PlayArea.PlayArea PlayArea { get; set; }
        [field: SerializeField] public TickDriver TickDriver { get; set; }
        [field: SerializeField] public GameObject LockOutText { get; set; }
        [field: SerializeField] public GameObject TopOutText { get; set; }

        [field: UdonSynced] public GameState CurrentState { get; set; }

        private void Awake()
        {
            if (NotifyLineClearsTo == null) Debug.LogWarning("TetrisGame.Awake: NotifyLineClearsTo is null.");
            if (PlayArea == null) Debug.LogError("TetrisGame.Awake: PlayArea is null.");
            if (TickDriver == null) Debug.LogError("TetrisGame.Awake: TickDriver is null.");
            if (LockOutText == null) Debug.LogError("TetrisGame.Awake: LockOutText is null.");
            if (TopOutText == null) Debug.LogError("TetrisGame.Awake: TopOutText is null.");

            LockOutText.SetActive(false);
            TopOutText.SetActive(false);
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
            var shouldRequest = CurrentState == GameState.NotStarted;
            Debug.Log($"TetrisGame.OnOwnershipRequest: shouldRequest={shouldRequest}");

            // Unfreeze the player if they were the owner and are having ownership reassigned
            if (shouldRequest && Networking.IsOwner(gameObject))
            {
                UnfreezeOwner();
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
            Random.InitState(Networking.GetServerTimeInMilliseconds());

            Debug.Log($"TetrisGame.InitGame: Set owner to {player.displayName}");
            if (player.isLocal)
            {
                Debug.Log("TetrisGame.InitGame: Owner is local player");
                PlayArea.SetOwned(true);
                FreezeOwner(player);
            }

            SetGameState(GameState.Playing);
        }

        public void StopGame()
        {
            if (CurrentState == GameState.Stopped) return;

            Debug.Log("TetrisGame.StopGame: Stopping game");


            SetGameState(GameState.Stopped);
        }

        private static void FreezeOwner()
        {
            FreezeOwner(Networking.LocalPlayer);
        }

        private static void FreezeOwner(VRCPlayerApi player)
        {
            player.Immobilize(true);
            player.SetJumpImpulse(0);
        }

        private void UnfreezeOwner()
        {
            // TODO: Make this not break for the instance owner
            Networking.LocalPlayer.Immobilize(false);
            Networking.LocalPlayer.SetJumpImpulse();
        }

        public void ResetGame()
        {
            Debug.Log("TetrisGame.ResetGame: Resetting game");
            StopGame();
            PlayArea.Clear();
            LockOutText.SetActive(false);
            TopOutText.SetActive(false);
            RequestSerialization();
        }

        private void SetGameState(GameState nextState)
        {
            CurrentState = nextState;
            switch (nextState)
            {
                case GameState.Playing:
                    TickDriver.enabled = true;
                    FreezeOwner();
                    break;
                case GameState.Stopped:
                    TickDriver.enabled = false;
                    UnfreezeOwner();
                    break;
            }
        }

        public void PlayAreaOnClearedLines()
        {
            // Each kind of line clear needs to be sent as a separate event, since Udon events can't have
            // any parameters
            var linesCleared = PlayArea.LastLinesCleared;
            if (NotifyLineClearsTo == null)
            {
                Debug.LogWarning("TetrisGame.PlayAreaOnClearedLines: NotifyLineClearsTo is null.");
            }
            else switch (linesCleared)
            {
                case 2:
                    NotifyDoubleLineClear();
                    break;
                case 3:
                    NotifyTripleLineClear();
                    break;
                case 4:
                    NotifyTetrisLineClear();
                    break;
            }
        }

        private void NotifyDoubleLineClear()
        {
            NotifyLineClearsTo.SendCustomEvent("TetrisGameOnDoubleLineClear");
        }

        private void NotifyTripleLineClear()
        {
            NotifyLineClearsTo.SendCustomEvent("TetrisGameOnTripleLineClear");
        }

        private void NotifyTetrisLineClear()
        {
            NotifyLineClearsTo.SendCustomEvent("TetrisGameOnTetrisLineClear");
        }

        public void PlayAreaOnLockOut()
        {
            SetGameState(GameState.Stopped);
            LockOutText.SetActive(true);
        }

        public void PlayAreaOnTopOut()
        {
            SetGameState(GameState.Stopped);
            TopOutText.SetActive(true);
        }

        public void SendGarbage1()
        {
            SendGarbage(1);
        }

        public void SendGarbage2()
        {
            SendGarbage(2);
        }

        public void SendGarbage4()
        {
            SendGarbage(4);
        }

        private void SendGarbage(int lines)
        {
            Debug.Log($"TetrisGame.SendGarbage: Sending {lines} garbage lines to play area.");
            PlayArea.SendGarbage(lines);
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
            if (!ShouldBeControllable()) return;

            if (args.eventType == UdonInputEventType.AXIS)
            {
                // Quantize axis inputs
                value = args.floatValue >= TriggerDeadZone;
            }

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

        /// <summary>
        /// Exchange with the hold.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputJump(bool value, UdonInputEventArgs args)
        {
            if (!ShouldBeControllable()) return;

            if (!value) return;
            PlayArea.ExchangeHold();
        }

        /// <summary>
        /// Horizontal block movement - supports left/right controls.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveHorizontal(float value, UdonInputEventArgs args)
        {
            if (!ShouldBeControllable()) return;

            // Quantize axis inputs
            if (args.eventType == UdonInputEventType.AXIS && Math.Abs(args.floatValue) < JoystickDeadZone)
            {
                value = 0;
            }

            var sign = Math.Sign(value);
            if (!moveRightHeld && sign == 1)
            {
                PlayArea.MoveControlledGroup(1, 0);
                moveLeftHeld = false;
                moveRightHeld = true;
            }
            else if (!moveLeftHeld && sign == -1)
            {
                PlayArea.MoveControlledGroup(-1, 0);
                moveRightHeld = false;
                moveLeftHeld = true;
            }
            else if (sign == 0)
            {
                moveRightHeld = false;
                moveLeftHeld = false;
                PlayArea.DisableAutoRepeat();
                return;
            }

            PlayArea.PrepareAutoRepeat(sign == -1 ? AutoRepeatDirection.Left : AutoRepeatDirection.Right);
        }

        /// <summary>
        /// Vertical block movement - only supports dropping a block down.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            if (!ShouldBeControllable()) return;

            // Quantize axis inputs
            if (args.eventType == UdonInputEventType.AXIS && Math.Abs(args.floatValue) < JoystickDeadZone)
            {
                value = 0;
            }

            var direction = Math.Sign(value);
            if (direction == -1)
            {
                PlayArea.SoftDrop(true);
            }
            else
            {
                PlayArea.SoftDrop(false);
                if (!hardDropHeld && direction == 1)
                {
                    PlayArea.HardDrop();
                    hardDropHeld = true;
                }
                else if (direction == 0)
                {
                    hardDropHeld = false;
                }
            }
        }

        private bool ShouldBeControllable()
        {
            return CurrentState == GameState.Playing && Networking.IsOwner(gameObject);
        }
    }
}