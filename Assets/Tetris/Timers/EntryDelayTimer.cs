using UdonSharp;
using UnityEngine;

namespace Tetris.Timers
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EntryDelayTimer : UdonSharpBehaviour
    {
        private const int EntryDelayTicks = 6;
        
        private int entryDelayProgress;
        private bool entryDelayEnabled;

        [field: SerializeField] public PlayArea.PlayArea PlayArea { get; set; }

        private void Awake()
        {
            if (PlayArea == null)
            {
                Debug.LogError("EntryDelayTimer.Awake: PlayArea is null.");
            }
        }

        public void Tick()
        {
            if (entryDelayEnabled && ++entryDelayProgress >= EntryDelayTicks)
            {
                PlayArea.LoadNextShape();
                ResetTimer();
            }
        }

        public void BeginTimer()
        {
            if (entryDelayEnabled) return;
            Debug.Log("EntryDelayTimer.BeginTimer: Beginning entry delay timer");
            entryDelayEnabled = true;
        }

        public void ResetTimer()
        {
            entryDelayProgress = 0;
            entryDelayEnabled = false;
        }
    }
}