using UdonSharp;

namespace Tetris
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class GameStation : UdonSharpBehaviour
    {
        private void Start()
        {
            DisableInteractive = true;
        }
    }
}