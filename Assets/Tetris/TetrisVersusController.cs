using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
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
            var activeGames = GetActiveGames();
            if (activeGames.Length < 2) return;

            if (!TryFindLocalGame(activeGames, out var from, out var fromIdx))
            {
                Debug.LogWarning("TetrisVersusController.FindSender: Could not find event sender.");
                return;
            }

            var toIdx = Random.Range(0, activeGames.Length);
            if (toIdx == fromIdx)
            {
                // Don't send yourself garbage
                toIdx = (toIdx + 1) % activeGames.Length;
            }

            var lines = from.PlayArea.LastLinesCleared;

            Debug.Log(
                $"TetrisVersusController.TetrisGameOnClearedLines: Sending {lines} garbage lines from game {fromIdx} to {toIdx}");

            // TODO: Use OnDeserialization instead of risking desync with the network event
            var to = ParticipatingGames[toIdx];
            to.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TetrisGame.SendGarbage));
        }

        private TetrisGame[] GetActiveGames()
        {
            var activeGameIndexes = new DataList();
            for (var i = 0; i < ParticipatingGames.Length; i++)
            {
                if (ParticipatingGames[i].CurrentState == GameState.Playing)
                {
                    activeGameIndexes.Add(i);
                }
            }

            var games = new TetrisGame[activeGameIndexes.Count];
            for (var i = 0; i < games.Length; i++)
            {
                games[i] = ParticipatingGames[activeGameIndexes[i].Int];
            }

            return games;
        }

        private bool TryFindLocalGame(TetrisGame[] games, out TetrisGame game, out int idx)
        {
            game = null;
            idx = -1;

            for (var i = 0; i < games.Length; i++)
            {
                if (Networking.IsOwner(games[i].gameObject) &&
                    games[i].CurrentState == GameState.Playing)
                {
                    game = games[i];
                    idx = i;
                    return true;
                }
            }

            return false;
        }
    }
}