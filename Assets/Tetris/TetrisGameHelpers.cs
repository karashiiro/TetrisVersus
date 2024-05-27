using UdonSharp;
using UnityEngine;

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
    }
}