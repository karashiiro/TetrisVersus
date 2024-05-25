using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tetris.Blocks;
using Tetris.PlayArea;

namespace Tests.Tetris.PlayArea
{
    public class QueueTests
    {
        [Test]
        public void TestPush()
        {
            var queue = Helpers.CreateQueue();
            var group = Helpers.CreateBlockGroup();
            using var _ = DisposeGameObjects.Of(queue);

            Assert.AreEqual(0, queue.Count);

            Assert.True(queue.Push(group));
            Assert.AreEqual(1, queue.Count);
        }

        [Test]
        public void TestPushFull()
        {
            var queue = Helpers.CreateQueue();
            using var disposeAll = DisposeGameObjects.Of(queue);

            Assert.AreEqual(0, queue.Count);
            for (var n = 1; n <= Queue.Capacity; n++)
            {
                Assert.True(queue.Push(Helpers.CreateBlockGroup()));
                Assert.AreEqual(n, queue.Count);
            }

            var nextGroup = Helpers.CreateBlockGroup();
            disposeAll.Add(nextGroup);

            Assert.False(queue.Push(nextGroup));
            Assert.AreEqual(Queue.Capacity, queue.Count);
        }

        [Test]
        public void TestPop()
        {
            var queue = Helpers.CreateQueue();
            var inGroup = Helpers.CreateBlockGroup();
            using var disposeAll = DisposeGameObjects.Of(queue, inGroup);

            queue.Push(inGroup);

            Assert.AreEqual(1, queue.Count);

            var parent = Helpers.CreateObject();
            disposeAll.Add(parent);

            var outGroup = queue.Pop(parent.transform);
            Assert.AreEqual(0, queue.Count);
            Assert.NotNull(outGroup);
            Assert.AreEqual(inGroup, outGroup);
        }

        [Test]
        public void TestPopFull()
        {
            var queue = Helpers.CreateQueue();
            using var disposeAll = DisposeGameObjects.Of(queue);

            var groups = new List<BlockGroup>();
            for (var n = 1; n <= Queue.Capacity; n++)
            {
                var inGroup = Helpers.CreateBlockGroup();
                queue.Push(inGroup);
                groups.Add(inGroup);
                disposeAll.Add(inGroup);
            }

            Assert.AreEqual(Queue.Capacity, queue.Count);

            var parent = Helpers.CreateObject();
            disposeAll.Add(parent);

            var outGroup = queue.Pop(parent.transform);
            Assert.AreEqual(Queue.Capacity - 1, queue.Count);
            Assert.NotNull(outGroup);
            Assert.AreEqual(groups.First(), outGroup);
        }
    }
}