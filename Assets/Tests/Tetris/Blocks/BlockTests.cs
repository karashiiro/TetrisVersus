using NUnit.Framework;
using Tetris.Blocks;
using Unity.PerformanceTesting;

namespace Tests.Tetris.Blocks
{
    public class BlockTests
    {
        [Test, Performance]
        public void TestEmptySerialization()
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
        public void TestSerialization()
        {
            var buffer = new byte[Block.RequiredNetworkBufferSize];

            var block1 = Helpers.CreateBlock();
            var block2 = Helpers.CreateBlock();
            using var _ = DisposeGameObjects.Of(block1, block2);

            block1.State = BlockState.Controlled;
            block1.ShapeType = ShapeType.Z;
            block2.ShapeType = ShapeType.S;

            Measure.Method(() =>
            {
                block1.SerializeInto(buffer, 0);
                block2.DeserializeFrom(buffer, 0);
            }).Run();

            Helpers.AssertEqual(block1, block2);
        }
    }
}