using Tetris.VRCExtensions;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace Tetris.PlayArea
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VersusMiniViewArray : UdonSharpBehaviour
    {
        private readonly DataList views = new DataList();

        [field: SerializeField] public GameObject PrototypeMiniView { get; set; }

        private void Awake()
        {
            if (PrototypeMiniView == null) Debug.LogError("VersusMiniViewArray.Awake: PrototypeMiniView is null.");
        }

        public void AddGame(TetrisGame game)
        {
            var nextViewObject = Instantiate(PrototypeMiniView, transform, false);
            var component = (UdonSharpBehaviour)nextViewObject.GetComponent(typeof(UdonSharpBehaviour));
            var miniView = (VersusMiniView)component;
            miniView.ReplicateRenderer = game.PlayArea.MiniViewRenderer;
            miniView.transform.position = transform.position;

            var x = views.Count % 4;
            var y = views.Count / 4;
            miniView.transform.SetLocalPositionAndRotation(new Vector3(1.2f * x, 2.3f * y), Quaternion.identity);

            views.Add(new DataToken(miniView));
        }

        public void ReplicateAll()
        {
            Debug.Log($"VersusMiniViewArray.ReplicateAll: Replicating {views.Count} views");
            foreach (var token in views.ToArray())
            {
                var view = token.As<VersusMiniView>();
                view.Replicate();
            }
        }
    }
}