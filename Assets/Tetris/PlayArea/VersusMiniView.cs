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
            if (TargetRenderer == null) Debug.LogError("VersusMiniView.Awake: TargetRenderer is null.");
            if (ReplicateRenderer == null) Debug.LogError("VersusMiniView.Awake: ReplicateRenderer is null.");
        }

        public void ReplicateMaterial(Material material)
        {
            if (TargetRenderer == null) return;
            TargetRenderer.material = material;
        }

        public void Replicate()
        {
            if (ReplicateRenderer == null) return;
            ReplicateMaterial(ReplicateRenderer.material);
        }
    }
}