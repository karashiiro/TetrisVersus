using UdonSharp;
using UnityEngine;

namespace Tetris.Timers
{
    public class AutoRepeatTimer : UdonSharpBehaviour
    {
        private const int DASTicks = 10;
        private const int AutoRepeatTicks = 2;

        private int dasProgress;
        private bool dasEnabled;

        private AutoRepeatDirection autoRepeatDirection;
        private bool autoRepeatEnabled;
        private int autoRepeatProgress;

        [field: SerializeField] public PlayArea PlayArea { get; set; }

        private void Awake()
        {
            if (PlayArea == null)
            {
                Debug.LogError("AutoRepeatTimer.Awake: PlayArea is null.");
            }
        }

        public void Tick()
        {
            if (dasEnabled && ++dasProgress == DASTicks)
            {
                ResetDAS();
                autoRepeatEnabled = true;
            }

            if (autoRepeatEnabled && ++autoRepeatProgress >= AutoRepeatTicks)
            {
                autoRepeatProgress = 0;
                PlayArea.MoveControlledGroup(autoRepeatDirection.AsTranslation(), 0);
            }
        }

        public void BeginTimerWithDirection(AutoRepeatDirection direction)
        {
            autoRepeatDirection = direction;
            dasEnabled = true;
        }

        public void ResetTimer()
        {
            ResetDAS();
            ResetAutoRepeat();
        }

        private void ResetAutoRepeat()
        {
            autoRepeatProgress = 0;
            autoRepeatEnabled = false;
            autoRepeatDirection = AutoRepeatDirection.None;
        }

        private void ResetDAS()
        {
            dasProgress = 0;
            dasEnabled = false;
        }
    }
}