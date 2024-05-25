using System.Linq;
using NUnit.Framework;
using Tetris;
using Tetris.Blocks;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests.Tetris.Blocks
{
    public class BlockGroupTests
    {
        [Test, Performance]
        public void TestBlockGroupSerialization()
        {
            var boundsMin = new Vector2Int(-2, -2);
            var boundsMax = new Vector2Int(3, 3);
            var blockSlots = (boundsMax.x - boundsMin.x) * (boundsMax.y - boundsMin.y);

            var bufferSize = BlockGroup.RequiredNetworkBufferSizeBase +
                             BlockGroup.RequiredNetworkBufferSizePerBlock * blockSlots;
            var buffer = new byte[bufferSize];

            var factory = Helpers.CreateBlockFactory();
            var palette = PaletteHelpers.DefaultPalette();

            var blockGroup1 = factory.CreateI(Color.cyan);
            var blockGroup2 = factory.CreateZ(Color.red);

            Measure.Method(() =>
            {
                var nWritten = blockGroup1.SerializeInto(buffer, 0, boundsMin, boundsMax);
                Assert.AreEqual(bufferSize, nWritten);

                var nRead = blockGroup2.DeserializeFrom(buffer, 0, boundsMin, boundsMax, factory, palette);
                Assert.AreEqual(bufferSize, nRead);
            }).Run();

            var blocks1 = blockGroup1.GetBlocks();
            var blocks2 = blockGroup2.GetBlocks();
            Assert.AreEqual(blocks1.Length, blocks2.Length);

            foreach (var (b1, b2) in blocks1.Zip(blocks2, (b1, b2) => (b1, b2)))
            {
                Helpers.AssertEqual(b1, b2);
            }
        }
    }
}