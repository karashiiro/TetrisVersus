using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class LockTimer : UdonSharpBehaviour
    {
        private const float LockDelay = 0.5f;

        private float lockTimer;
        private bool lockTimerEnabled;

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
            lockTimerEnabled = false;
        }
    }
}