using UdonSharp;
using UnityEngine;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OwnershipIndicator : UdonSharpBehaviour
    {
        [field: SerializeField] public TetrisGame Game { get; set; }

        public void ReplicateOwnership()
        {
            var r = GetComponent<Renderer>();
            r.material.color = Game.ShouldBeControllable() ? Color.cyan : Color.black;
        }
    }
}