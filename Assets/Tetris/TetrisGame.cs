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
            var square1 = BlockFactory.CreateSquare(Color.red);

            // Add the square tetra to the play area
            PlayArea.AddControlledBlocks(square1);

            // Do a few ticks so we know things are working
            for (var i = 0; i < 22; i++)
            {
                PlayArea.Tick();
            }

            // Create another square tetra
            var square2 = BlockFactory.CreateSquare(Color.blue);

            // Add the square tetra to the play area
            PlayArea.AddControlledBlocks(square2);

            // Do a few ticks so we know things are working
            for (var i = 0; i < 22; i++)
            {
                PlayArea.Tick();
            }
        }
    }
}