using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Tetris
{
    public class TetrisGame : UdonSharpBehaviour
    {
        private VRCPlayerApi Player { get; set; }

        [field: SerializeField] public PlayArea PlayArea { get; set; }

        public void SetOwningPlayer(VRCPlayerApi player)
        {
            Player = player;
        }

        private void Start()
        {
            // Set the owner to the first player in the instance for now
            Player = VRCPlayerApi.GetPlayerById(1);

            // Do a few ticks so we know things are working
            for (var i = 0; i < 70; i++)
            {
                Debug.Log($"Current tick: {i}");
                PlayArea.Tick();
            }
        }
    }
}