using UdonSharp;
using UnityEngine;

namespace Tetris.Timers
{
    public class LockTimer : UdonSharpBehaviour
    {
        private const int LockTicks = 30;
        private const int MaxResetsWhileLocking = 15;

        private int lockProgress;
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

        public void Tick()
        {
            if (!lockTimerEnabled) return;

            if (++lockProgress >= LockTicks)
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
            lockProgress = 0;
            lockTimerResets = 0;
            lockTimerEnabled = false;
        }

        public void ResetTimerWhileLocking()
        {
            if (lockTimerResets == MaxResetsWhileLocking) return;
            lockProgress = 0;
            lockTimerResets++;
            lockTimerEnabled = false;
        }
    }
}