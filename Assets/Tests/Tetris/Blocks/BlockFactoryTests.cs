using NUnit.Framework;
using Tetris.Blocks;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests.Tetris.Blocks
{
    public class BlockFactoryTests
    {
        [Test, Performance]
        public void TestCreateShape(
            [Values(ShapeType.L, ShapeType.J, ShapeType.S, ShapeType.Z, ShapeType.O, ShapeType.I, ShapeType.T)]
            ShapeType shapeType)
        {
            var factory = Helpers.CreateBlockFactory();
            using var disposeAll = DisposeGameObjects.Of(factory);

            BlockGroup group = null;

            // ReSharper disable once AccessToDisposedClosure
            Measure.Method(() => disposeAll.Add(group = factory.CreateShape(shapeType, Color.black))).Run();

            Assert.NotNull(group);
            Assert.AreEqual(shapeType, group.Type);
            foreach (var block in group.GetBlocks())
            {
                Assert.AreEqual(BlockState.AtRest, block.State);
                Assert.AreEqual(shapeType, block.ShapeType);
            }
        }

        [Test, Performance]
        public void TestCreateShapePooled(
            [Values(ShapeType.L, ShapeType.J, ShapeType.S, ShapeType.Z, ShapeType.O, ShapeType.I, ShapeType.T)]
            ShapeType shapeType)
        {
            var factory = Helpers.CreateBlockFactory();
            using var _ = DisposeGameObjects.Of(factory);

            Measure.Method(() =>
            {
                var group = factory.CreateShape(shapeType, Color.black);
                factory.ReturnBlockGroup(group);
            }).Run();
        }

        [Test]
        public void TestBasicPooling()
        {
            var factory = Helpers.CreateBlockFactory();
            using var _ = DisposeGameObjects.Of(factory);

            Assert.AreEqual(0, factory.PoolSize);

            var group1 = factory.CreateShape(ShapeType.O, Color.yellow);
            Assert.AreEqual(0, factory.PoolSize);

            factory.ReturnBlockGroup(group1);
            Assert.AreEqual(4, factory.PoolSize);

            var group2 = factory.CreateShape(ShapeType.T, Color.magenta);
            Assert.AreEqual(0, factory.PoolSize);

            factory.ReturnBlockGroup(group2);
            Assert.AreEqual(4, factory.PoolSize);

            foreach (var block in group2.GetBlocks())
            {
                Assert.AreEqual(BlockState.AtRest, block.State);
                Assert.AreEqual(ShapeType.T, block.ShapeType);
            }
        }
    }
}