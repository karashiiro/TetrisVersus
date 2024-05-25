using NUnit.Framework;
using Tetris.Blocks;
using Unity.PerformanceTesting;

namespace Tests.Tetris.PlayArea
{
    public class HoldTests
    {
        [Test]
        public void TestExchange()
        {
            var hold = Helpers.CreateHold();
            var group1 = Helpers.CreateBlockGroup();
            var group2 = Helpers.CreateBlockGroup();
            var parent = Helpers.CreateObject();
            var originalGroup1 = group1;
            var originalGroup2 = group2;

            // Store group1 inside the hold, expecting it to be null afterward
            hold.Exchange(ref group1, parent.transform, BlockState.AtRest);
            Assert.Null(group1);

            // Store group2 inside the hold, expecting group2 to be the original group1 afterward
            hold.Exchange(ref group2, parent.transform, BlockState.AtRest);
            Assert.NotNull(group2);
            Assert.AreEqual(originalGroup1, group2);

            // Store group1 (null) inside the hold, expecting group1 to be the original group2 afterward
            hold.Exchange(ref group1, parent.transform, BlockState.AtRest);
            Assert.NotNull(group1);
            Assert.AreEqual(originalGroup2, group1);

            // Store group2 (original group1) inside the hold, expecting group2 to be null afterward
            hold.Exchange(ref group2, parent.transform, BlockState.AtRest);
            Assert.Null(group2);

            // Store group1 (original group2) inside the hold, expecting group1 to be the original group1 afterward
            hold.Exchange(ref group1, parent.transform, BlockState.AtRest);
            Assert.NotNull(group1);
            Assert.AreEqual(originalGroup1, group1);

            // Store group2 (null) inside the hold, expecting group2 to be the original group2 afterward
            hold.Exchange(ref group2, parent.transform, BlockState.AtRest);
            Assert.NotNull(group2);
            Assert.AreEqual(originalGroup2, group2);
        }

        [Test]
        public void TestExchangeEmpty()
        {
            var hold = Helpers.CreateHold();
            var group = Helpers.CreateBlockGroup();
            var parent = Helpers.CreateObject();
            var originalGroup = group;

            // Store a non-null group inside the hold, expecting group to be null afterward
            hold.Exchange(ref group, parent.transform, BlockState.AtRest);
            Assert.Null(group);

            // Store the now-null group inside the hold, expecting group to be the original value afterward
            hold.Exchange(ref group, parent.transform, BlockState.AtRest);
            Assert.NotNull(group);

            Assert.AreEqual(originalGroup, group);
        }

        [Test, Performance]
        public void BenchExchange()
        {
            var hold = Helpers.CreateHold();
            var group1 = Helpers.CreateBlockGroup();
            var group2 = Helpers.CreateBlockGroup();
            var parent = Helpers.CreateObject();
            hold.Exchange(ref group1, parent.transform, BlockState.AtRest);

            Measure.Method(() => hold.Exchange(ref group2, parent.transform, BlockState.AtRest)).Run();
        }

        [Test, Performance]
        public void BenchExchangeEmpty()
        {
            var hold = Helpers.CreateHold();
            var group = Helpers.CreateBlockGroup();
            var parent = Helpers.CreateObject();

            Measure.Method(() => hold.Exchange(ref group, parent.transform, BlockState.AtRest)).Run();
        }
    }
}