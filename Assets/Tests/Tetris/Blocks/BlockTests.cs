using NUnit.Framework;
using Tetris.Blocks;
using UdonSharpEditor;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests.Tetris.Blocks
{
    public class BlockTests
    {
        [Test, Performance]
        public void TestBlockEmptySerialization()
        {
            var buffer = new byte[Block.RequiredNetworkBufferSize];

            var didRun = false;
            var result = true;

            Measure.Method(() =>
            {
                Block.SerializeEmpty(buffer, 0);
                result = Block.ShouldDeserialize(buffer, 0);
                didRun = true;
            }).Run();

            Assert.True(didRun);
            Assert.False(result);
        }

        [Test, Performance]
        public void TestBlockSerialization()
        {
            var buffer = new byte[Block.RequiredNetworkBufferSize];

            var block1 = CreateBlock();
            var block2 = CreateBlock();

            block1.State = BlockState.Controlled;
            block1.ShapeType = ShapeType.Z;

            Measure.Method(() =>
            {
                block1.SerializeInto(buffer, 0);
                block2.DeserializeFrom(buffer, 0);
            }).Run();

            Assert.AreEqual(block1.State, block2.State);
            Assert.AreEqual(block1.ShapeType, block2.ShapeType);
        }

        private static Block CreateBlock()
        {
            var gameObject = UnityEditor.ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
            return gameObject.AddUdonSharpComponent<Block>();
        }
    }
}