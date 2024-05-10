using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class LockTimer : UdonSharpBehaviour
    {
        private const float LockDelay = 0.5f;
        private const int MaxResetsWhileLocking = 15;

        private float lockTimer;
        private bool lockTimerEnabled;
        private int lockTimerResets;

        [field: SerializeField] public PlayArea PlayArea { get; set; }

        private void Awake()
        {
            if (PlayArea == null)
            {
                Debug.LogError("LockTimer.Awake: PlayArea is null.");
            }
        }

        private void FixedUpdate()
        {
            if (!lockTimerEnabled) return;
            lockTimer += Time.fixedDeltaTime;

            if (lockTimer >= LockDelay)
            {
                PlayArea.LockControlledGroup();
                ResetTimer();
            }
        }

        public void BeginTimer()
        {
            lockTimerEnabled = true;
        }

        public void ResetTimer()
        {
            lockTimer = 0;
            lockTimerResets = 0;
            lockTimerEnabled = false;
        }

        public void ResetTimerWhileLocking()
        {
            if (lockTimerResets == MaxResetsWhileLocking) return;
            lockTimer = 0;
            lockTimerResets++;
        }
    }
}