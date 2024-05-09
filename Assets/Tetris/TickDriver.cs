using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace Tetris
{
    public class TickDriver : UdonSharpBehaviour
    {
        private const float MinInterval = 1f / 60;

        private float elapsedTime;

        [field: SerializeField] [CanBeNull] public PlayArea PlayArea { get; set; }

        private void FixedUpdate()
        {
            elapsedTime += Time.fixedDeltaTime;

            while (elapsedTime >= MinInterval)
            {
                if (PlayArea != null)
                {
                    PlayArea.Tick();
                }

                elapsedTime -= MinInterval;
            }
        }
    }
}