using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using Random = UnityEngine.Random;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TetrisVersusController : UdonSharpBehaviour
    {
        [field: SerializeField] public TetrisGame[] ParticipatingGames { get; set; }

        private void Awake()
        {
            if (ParticipatingGames == null) Debug.LogError("TetrisVersusController.Awake: ParticipatingGames is null.");
        }

        public void TetrisGameOnClearedLines()
        {
            if (!TryFindLocalGame(out var from, out var fromIdx))
            {
                Debug.LogError("TetrisVersusController.FindSender: Could not find event sender!");
                return;
            }

            var toIdx = Random.Range(0, ParticipatingGames.Length);
            if (toIdx == fromIdx)
            {
                // Don't send yourself garbage
                toIdx = (toIdx + 1) % ParticipatingGames.Length;
            }

            var lines = from.PlayArea.LastLinesCleared;

            Debug.Log(
                $"TetrisVersusController.TetrisGameOnClearedLines: Sending {lines} garbage lines from game {fromIdx} to {toIdx}");

            // TODO: Use OnDeserialization instead of risking desync with the network event
            var to = ParticipatingGames[toIdx];
            to.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TetrisGame.SendGarbage));
        }

        private bool TryFindLocalGame(out TetrisGame game, out int idx)
        {
            game = null;
            idx = -1;
            
            for (var i = 0; i < ParticipatingGames.Length; i++)
            {
                if (Networking.IsOwner(ParticipatingGames[i].gameObject) &&
                    ParticipatingGames[i].CurrentState == GameState.Playing)
                {
                    game = ParticipatingGames[i];
                    idx = i;
                    return true;
                }
            }

            return false;
        }
    }
}