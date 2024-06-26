﻿using System;
using Tetris;
using Tetris.VRCExtensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Arcade
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EntryZone : UdonSharpBehaviour
    {
        private readonly DataList players = new DataList();
        private readonly DataDictionary playerIndicators = new DataDictionary();

        private readonly DataDictionary gameStations = new DataDictionary();

        private DataList floors;

        [field: SerializeField] public GameObject InstanceMasterIndicator { get; set; }
        [field: SerializeField] public GameObject PrototypePlayerIndicator { get; set; }
        [field: SerializeField] public Transform PlayerIndicators { get; set; }
        [field: SerializeField] public TetrisVersusController VersusController { get; set; }
        [field: SerializeField] public GameObject PrototypeTetrisGamePrefab { get; set; }
        [field: SerializeField] public Transform GameArea { get; set; }
        [field: SerializeField] public GameObject Floor { get; set; }

        private void Awake()
        {
            if (InstanceMasterIndicator == null) Debug.LogError("EntryZone.Awake: InstanceMasterIndicator is null.");
            if (PrototypePlayerIndicator == null) Debug.LogError("EntryZone.Awake: PrototypePlayerIndicator is null.");
            if (PlayerIndicators == null) Debug.LogError("EntryZone.Awake: PlayerIndicators is null.");
            if (VersusController == null) Debug.LogError("EntryZone.Awake: VersusController is null.");
            if (PrototypeTetrisGamePrefab == null)
                Debug.LogError("EntryZone.Awake: PrototypeTetrisGamePrefab is null.");
            if (GameArea == null) Debug.LogError("EntryZone.Awake: GameArea is null.");
            if (Floor == null) Debug.LogError("EntryZone.Awake: Floor is null.");
        }

        private void Start()
        {
            floors = new DataList();
            floors.Add(new DataToken(Floor));
            UpdateInstanceMasterUI();
        }

        public void StartGame()
        {
            if (!Networking.IsMaster || players.Count == 0)
            {
                Debug.Log("EntryZone.StartGame: Cannot start game");
                return;
            }

            Debug.Log("EntryZone.StartGame: Starting game");

            // 0a. If a game with players is active, return (don't cancel the game)
            // 0b. If a game with no players (left/respawned) is active, stop it and reset the play area
            ResetGameArea();

            // 1. Instantiate prefabs for each player
            var games = CreateGamesForPlayers();

            // 2. Hook up the prefabs to the VS controller
            VersusController.ParticipatingGames = games;

            // 3. Initialize the VS controller and start the game
            VersusController.StartGame();

            // 4. Reassign each game station to its player
            foreach (var playerToken in gameStations.GetKeys().ToArray())
            {
                var player = playerToken.As<VRCPlayerApi>();
                var gameStation = gameStations[playerToken].As<GameObject>();
                var station = TetrisGameHelpers.GetStationFromPrefabInstance(gameStation);
                station.UseStation(player);
            }
        }

        private void ResetGameArea()
        {
            Debug.Log("EntryZone.ResetGameArea: Resetting game area");
            ClearGameInstances();
        }

        private void UpdateInstanceMasterUI()
        {
            InstanceMasterIndicator.SetActive(Networking.IsMaster);
        }

        private TetrisGame[] CreateGamesForPlayers()
        {
            const int gamesPerLayer = 4;
            const int spacing = 25;

            var games = new TetrisGame[players.Count];
            for (var i = 0; i < players.Count; i++)
            {
                var prefabInstance = Instantiate(PrototypeTetrisGamePrefab, GameArea, false);

                var cycle = i % gamesPerLayer;
                var layer = i / gamesPerLayer;
                var angleRadians = cycle * Math.PI / 2;
                var angleDegrees = Convert.ToSingle(angleRadians * 180 / Math.PI);
                var x = spacing * Convert.ToSingle(Math.Cos(angleRadians));
                var y = spacing * layer;
                var z = -spacing * Convert.ToSingle(Math.Sin(angleRadians));
                prefabInstance.transform.SetLocalPositionAndRotation(new Vector3(x, y, z),
                    Quaternion.Euler(0, angleDegrees, 0));

                if (layer >= floors.Count)
                {
                    var nextFloor = Instantiate(Floor);
                    nextFloor.transform.Translate(new Vector3(0, 0, -layer * spacing));
                    floors.Add(nextFloor);
                }

                gameStations.Add(players[i], prefabInstance);
                games[i] = TetrisGameHelpers.GetBehaviorFromPrefabInstance(prefabInstance);
            }

            return games;
        }

        private void ClearGameInstances()
        {
            foreach (var game in gameStations.GetValues().ToArray())
            {
                ObjectHelpers.Destroy(game.As<GameObject>());
            }

            VersusController.ParticipatingGames = new TetrisGame[0];
            gameStations.Clear();
        }

        private VRCPlayerApi[] GetParticipatingPlayers()
        {
            return players.ToReferenceArray<VRCPlayerApi>();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            UpdateInstanceMasterUI();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (players.Contains(new DataToken(player))) return;
            players.Add(new DataToken(player));

            var count = players.Count;
            Debug.Log($"EntryZone.OnPlayerTriggerEnter: Adding player {player.displayName} to game ({count} players)");
            UpdatePlayerIndicators();
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!players.Contains(new DataToken(player))) return;
            players.Remove(new DataToken(player));

            var count = players.Count;
            Debug.Log(
                $"EntryZone.OnPlayerTriggerExit: Removing player {player.displayName} from game ({count} players)");
            UpdatePlayerIndicators();
        }

        private void UpdatePlayerIndicators()
        {
            foreach (var key in playerIndicators.GetKeys().ToArray())
            {
                var indicator = (GameObject)playerIndicators[key].Reference;
                playerIndicators.Remove(key);
                Destroy(indicator);
            }

            const int xLimit = 4;
            for (var i = 0; i < players.Count; i++)
            {
                var nextIndicator = Instantiate(PrototypePlayerIndicator, PlayerIndicators, false);
                nextIndicator.SetActive(true);
                nextIndicator.transform.position = transform.position;

                var xMax = 0.25f * (xLimit - 1);
                var x = xMax - i % xLimit;
                var z = i / xLimit;
                nextIndicator.transform.SetLocalPositionAndRotation(new Vector3(0.25f * x, 0, 0.25f * z),
                    Quaternion.identity);

                var player = (VRCPlayerApi)players[i].Reference;
                playerIndicators[player.playerId] = nextIndicator;
            }
        }
    }
}