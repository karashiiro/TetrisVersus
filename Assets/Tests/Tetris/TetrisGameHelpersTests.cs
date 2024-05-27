using NUnit.Framework;
using Tetris;
using UnityEngine;

namespace Tests.Tetris
{
    public class TetrisGameHelpersTests
    {
        [Test]
        public void TestGetBehaviorFromPrefabInstance()
        {
            var prefab = Resources.Load<GameObject>("TetrisGame");
            Assert.NotNull(prefab);

            var game = TetrisGameHelpers.GetBehaviorFromPrefabInstance(prefab);
            Assert.NotNull(game);
            Assert.IsInstanceOf<TetrisGame>(game);
        }
    }
}