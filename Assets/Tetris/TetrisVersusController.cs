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

        public void TetrisGameOnDoubleLineClear()
        {
            SendGarbageRandomly(1);
        }

        public void TetrisGameOnTripleLineClear()
        {
            SendGarbageRandomly(2);
        }

        public void TetrisGameOnTetrisLineClear()
        {
            SendGarbageRandomly(4);
        }

        private void SendGarbageRandomly(int garbageLines)
        {
            var activeGames = GetActiveGames();
            if (activeGames.Length < 2) return;

            if (!TryFindLocalGame(activeGames, out var from, out var fromIdx))
            {
                Debug.LogWarning("TetrisVersusController.TetrisGameOnDoubleLineClear: Could not find event sender.");
                return;
            }

            var toIdx = Random.Range(0, activeGames.Length);
            if (toIdx == fromIdx)
            {
                // Don't send yourself garbage
                toIdx = (toIdx + 1) % activeGames.Length;
            }

            Debug.Log(
                $"TetrisVersusController.TetrisGameOnDoubleLineClear: Sending {garbageLines} garbage lines from game {fromIdx} to {toIdx}");

            // TODO: Use OnDeserialization instead of risking desync with the network event
            var to = ParticipatingGames[toIdx];
            switch (garbageLines)
            {
                case 1:
                    to.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TetrisGame.SendGarbage1));
                    break;
                case 2:
                    to.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TetrisGame.SendGarbage2));
                    break;
                case 4:
                    to.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(TetrisGame.SendGarbage4));
                    break;
            }
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