﻿using System;
using Tetris.PlayArea;
using Tetris.Timers;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using Random = UnityEngine.Random;
using VRCStation = VRC.SDK3.Components.VRCStation;

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

        [UdonSynced]
        private readonly byte[] networkState = new byte[Tetris.PlayArea.PlayArea.RequiredNetworkBufferSize];

        private bool moveLeftHeld;
        private bool moveRightHeld;
        private bool rotateLeftHeld;
        private bool rotateRightHeld;
        private bool hardDropHeld;

        [field: SerializeField] public UdonSharpBehaviour NotifyEventsTo { get; set; }
        [field: SerializeField] public PlayArea.PlayArea PlayArea { get; set; }
        [field: SerializeField] public TickDriver TickDriver { get; set; }
        [field: SerializeField] public GameObject LockOutText { get; set; }
        [field: SerializeField] public GameObject TopOutText { get; set; }
        [field: SerializeField] public VRCStation Station { get; set; }
        [field: SerializeField] public VersusMiniViewArray VersusMiniViews { get; set; }
        [field: SerializeField] public OwnershipIndicator OwnershipIndicatorLine { get; set; }

        [field: UdonSynced] public GameState CurrentState { get; set; }

        private void Awake()
        {
            if (NotifyEventsTo == null) Debug.LogWarning("TetrisGame.Awake: NotifyLineClearsTo is null.");
            if (PlayArea == null) Debug.LogError("TetrisGame.Awake: PlayArea is null.");
            if (TickDriver == null) Debug.LogError("TetrisGame.Awake: TickDriver is null.");
            if (LockOutText == null) Debug.LogError("TetrisGame.Awake: LockOutText is null.");
            if (TopOutText == null) Debug.LogError("TetrisGame.Awake: TopOutText is null.");
            if (Station == null) Debug.LogError("TetrisGame.Awake: Station is null.");
            if (VersusMiniViews == null) Debug.LogError("TetrisGame.Awake: VersusMiniViews is null.");
            if (OwnershipIndicatorLine == null) Debug.LogError("TetrisGame.Awake: OwnershipIndicatorLine is null.");
        }

        private void Start()
        {
            LockOutText.SetActive(false);
            TopOutText.SetActive(false);
            VersusMiniViews.ReplicateAll();
        }

        public override void Interact()
        {
            TakeOwnershipAndStart();
        }

        private void TakeOwnershipAndStart()
        {
            if (Networking.IsOwner(gameObject))
            {
                Debug.Log("TetrisGame.TakeOwnershipAndStart: Already owner, skipping ownership request");
                InitGame();
                return;
            }

            // TakeOwnershipAndStart is always triggered by the local player
            Debug.Log("TetrisGame.TakeOwnershipAndStart: Requesting ownership of game");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            // OnOwnershipRequest is triggered by the requesting player (who should always be the interact player?)
            // The current owner still needs to approve the request after this event resolves.
            var shouldRequest = CurrentState == GameState.NotStarted;
            Debug.Log($"TetrisGame.OnOwnershipRequest: shouldRequest={shouldRequest}");

            // Unfreeze the player if they were the owner and are having ownership reassigned
            if (shouldRequest && LocalPlayerIsOwner())
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
            if (player.isLocal)
            {
                InitGame();
            }
        }

        private void InitGame()
        {
            Random.InitState(Networking.GetServerTimeInMilliseconds());

            Debug.Log("TetrisGame.InitGame: Starting game as local player");
            PlayArea.SetOwned(true);

            SetGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Stopped || !LocalPlayerIsOwner()) return;
            Debug.Log("TetrisGame.PauseGame: Stopping game");
            SetGameState(GameState.Stopped);
        }

        public void ResetGame()
        {
            if (CurrentState == GameState.NotStarted || !LocalPlayerIsOwner()) return;
            Debug.Log("TetrisGame.ResetGame: Resetting game");
            VersusMiniViews.Clear();
            PlayArea.Clear();
            LockOutText.SetActive(false);
            TopOutText.SetActive(false);
            SetGameState(GameState.NotStarted);
        }

        private void FreezeOwner()
        {
            if (!LocalPlayerIsOwner()) return;
            Station.UseStation(Networking.LocalPlayer);
        }

        private void UnfreezeOwner()
        {
            if (!LocalPlayerIsOwner()) return;
            Station.ExitStation(Networking.LocalPlayer);
        }

        private void SetGameState(GameState nextState)
        {
            var last = CurrentState;
            CurrentState = nextState;
            if (CurrentState != last)
            {
                RequestSerialization();
            }

            switch (nextState)
            {
                case GameState.Playing:
                    TickDriver.enabled = true;
                    FreezeOwner();
                    break;
                case GameState.NotStarted:
                case GameState.Stopped:
                    TickDriver.enabled = false;
                    UnfreezeOwner();
                    break;
            }

            OwnershipIndicatorLine.ReplicateOwnership();
        }

        public void PlayAreaOnClearedLines()
        {
            // Each kind of line clear needs to be sent as a separate event, since Udon events can't have
            // any parameters
            var linesCleared = PlayArea.LastLinesCleared;
            if (NotifyEventsTo == null)
            {
                Debug.LogWarning("TetrisGame.PlayAreaOnClearedLines: NotifyEventsTo is null.");
            }
            else
                switch (linesCleared)
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
            NotifyEventsTo.SendCustomEvent("TetrisGameOnDoubleLineClear");
        }

        private void NotifyTripleLineClear()
        {
            NotifyEventsTo.SendCustomEvent("TetrisGameOnTripleLineClear");
        }

        private void NotifyTetrisLineClear()
        {
            NotifyEventsTo.SendCustomEvent("TetrisGameOnTetrisLineClear");
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
            if (LocalPlayerIsOwner() && PlayArea.ShouldSerialize())
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
            if (LocalPlayerIsOwner()) return;
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

        public bool ShouldBeControllable()
        {
            return CurrentState == GameState.Playing && LocalPlayerIsOwner();
        }

        private bool LocalPlayerIsOwner()
        {
            return Networking.IsOwner(gameObject);
        }
    }
}