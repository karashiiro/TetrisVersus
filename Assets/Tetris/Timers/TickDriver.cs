using UdonSharp;
using UnityEngine;

namespace Tetris.Timers
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TickDriver : UdonSharpBehaviour
    {
        // Emulate 60Hz
        private const float MinInterval = 1f / 60;

        private float elapsedTime;

        [field: SerializeField] public PlayArea PlayArea { get; set; }
        [field: SerializeField] public LockTimer LockTimer { get; set; }
        [field: SerializeField] public AutoRepeatTimer AutoRepeatTimer { get; set; }
        [field: SerializeField] public EntryDelayTimer EntryDelayTimer { get; set; }

        private void Awake()
        {
            if (PlayArea == null) Debug.LogError("TickDriver.Awake: PlayArea is null.");
            if (LockTimer == null) Debug.LogError("TickDriver.Awake: LockTimer is null.");
            if (AutoRepeatTimer == null) Debug.LogError("TickDriver.Awake: AutoRepeatTimer is null.");
            if (EntryDelayTimer == null) Debug.LogError("TickDriver.Awake: EntryDelayTimer is null.");
        }

        private void FixedUpdate()
        {
            elapsedTime += Time.fixedDeltaTime;

            while (elapsedTime >= MinInterval)
            {
                EntryDelayTimer.Tick();
                AutoRepeatTimer.Tick();
                LockTimer.Tick();
                PlayArea.Tick();

                elapsedTime -= MinInterval;
            }
        }
    }
}