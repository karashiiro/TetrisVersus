using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Tetris
{
    public class TetrisGame : UdonSharpBehaviour
    {
        private VRCPlayerApi Player { get; set; }

        [field: SerializeField] public PlayArea PlayArea { get; set; }
        [field: SerializeField] public BlockFactory BlockFactory { get; set; }

        public void SetOwningPlayer(VRCPlayerApi player)
        {
            Player = player;
        }

        private void Start()
        {
            // Set the owner to the first player in the instance for now
            Player = VRCPlayerApi.GetPlayerById(1);

            // Create a square tetra
            var square = BlockFactory.CreateSquare();

            // Add the square tetra to the play area
            PlayArea.AddControlledBlocks(square);
        }
    }
}