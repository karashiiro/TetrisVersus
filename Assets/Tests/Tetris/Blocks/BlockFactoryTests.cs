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

            Measure.Method(() =>
            {
                var group = factory.CreateShape(shapeType, Color.magenta);

                Assert.AreEqual(shapeType, group.Type);
                foreach (var block in group.GetBlocks())
                {
                    Assert.AreEqual(BlockState.AtRest, block.State);
                    Assert.AreEqual(shapeType, block.ShapeType);
                }
            }).Run();
        }
    }
}