using UdonSharp;
using UnityEngine;

namespace Tetris.PlayArea
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VersusMiniView : UdonSharpBehaviour
    {
        [field: SerializeField] public Renderer TargetRenderer { get; set; }
        [field: SerializeField] public Renderer ReplicateRenderer { get; set; }

        private void Awake()
        {
            if (TargetRenderer == null) Debug.LogError("VersusHUD.Awake: TargetRenderer is null.");
            if (ReplicateRenderer == null) Debug.LogError("VersusHUD.Awake: ReplicateRenderer is null.");
        }

        public void Replicate()
        {
            TargetRenderer.material = ReplicateRenderer.material;
        }
    }
}