using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace Tetris
{
    public class TetrisGameHelpers
    {
        public static TetrisGame GetBehaviorFromPrefabInstance(GameObject prefabInstance)
        {
            var controller = prefabInstance.transform.Find("TetrisController");
            var component = (UdonSharpBehaviour)controller.GetComponent(typeof(UdonSharpBehaviour));
            return (TetrisGame)component;
        }

        public static VRCStation GetStationFromPrefabInstance(GameObject prefabInstance)
        {
            var transform = prefabInstance.transform.Find("Station");
            return transform.gameObject.GetComponent<VRCStation>();
        }
    }
}