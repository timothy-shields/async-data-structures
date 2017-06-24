using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class AsyncBoundedQueueTests
    {
        [TestMethod]
        public void Capacity_must_be_nonnegative()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new AsyncBoundedQueue<string>(-1));
        }

        [TestMethod]
        public void Capacity_may_be_zero()
        {
            new AsyncBoundedQueue<string>(0);
        }

        [TestMethod]
        public void Count_zero_capacity()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Count_empty()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public async Task Count_nonempty()
        {
            var queue = new AsyncBoundedQueue<string>(4);
            await queue.EnqueueAsync("A").Timeout();
            await queue.EnqueueAsync("B").Timeout();
            await queue.EnqueueAsync("C").Timeout();
            await queue.EnqueueAsync("D").Timeout();
            await queue.DequeueAsync().Timeout();
            Assert.AreEqual(3, queue.Count);
        }

        [TestMethod]
        public async Task DequeueAsync_empty_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                await queue.EnqueueAsync("A").Timeout();
                await task.AssertCanceledAsync();
            }
        }

        [TestMethod]
        public async Task DequeueAsync_already_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            await queue.EnqueueAsync("A").Timeout();
            await queue.DequeueAsync(new CancellationToken(true)).AssertCanceledAsync();
        }

        [TestMethod]
        public async Task DequeueAsync_canceled_after_completion()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            await queue.EnqueueAsync("A");
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                Assert.AreEqual("A", await task.Timeout());
            }
        }

        [TestMethod]
        public async Task EnqueueAsync_full_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(2);
            await queue.EnqueueAsync("A");
            await queue.EnqueueAsync("B");
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("C", cts.Token);
                cts.Cancel();
                await task.AssertCanceledAsync();
            }
        }

        [TestMethod]
        public async Task EnqueueAsync_not_full_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            await queue.EnqueueAsync("A").Timeout();
            await queue.EnqueueAsync("B").Timeout();
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("C", cts.Token);
                cts.Cancel();
                await task.Timeout();
            }
        }

        [TestMethod]
        public async Task EnqueueAsync_already_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            await queue.EnqueueAsync("A", new CancellationToken(true)).AssertCanceledAsync();
        }

        [TestMethod]
        public async Task EnqueueAsync_canceled_after_completion()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("A", cts.Token);
                cts.Cancel();
                await task.Timeout();
            }
        }

        [TestMethod]
        public void TryDequeue_before_Enqueue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            Assert.IsFalse(queue.TryDequeue());
        }

        [TestMethod]
        public async Task DequeueAsync_before_Enqueue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var task = queue.DequeueAsync();
            await queue.EnqueueAsync("A").Timeout();
            Assert.AreEqual("A", await task.Timeout());
        }

        [TestMethod]
        public async Task DequeueAsync_handled_in_order_of_caller()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C" };
            var tasks = values.Select(_ => queue.DequeueAsync()).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                tasks[i].AssertNotCompleted();
                await queue.EnqueueAsync(values[i]).Timeout();
                Assert.AreEqual(values[i], await tasks[i].Timeout());
            }
        }

        [TestMethod]
        public async Task First_in_first_out()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C" };
            for (int i = 0; i < values.Count; i++)
            {
                await queue.EnqueueAsync(values[i]).Timeout();
            }
            for (int i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i], await queue.DequeueAsync().Timeout());
            }
        }

        [TestMethod]
        public async Task First_in_first_out_exceeding_capacity()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C", "D", "E", "F" };
            var enqueueTasks = values.Select(queue.EnqueueAsync).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                if (i < queue.Capacity)
                {
                    await enqueueTasks[i].Timeout();
                }
                else
                {
                    enqueueTasks[i].AssertNotCompleted();
                }
            }
            for (int i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i], await queue.DequeueAsync().Timeout());
                if (i + queue.Capacity < values.Count)
                {
                    await enqueueTasks[i + queue.Capacity].Timeout();
                }
            }
        }

        [TestMethod]
        public async Task Capacity_zero_DequeueAsync_then_TryEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var dequeueTask = queue.DequeueAsync();
            dequeueTask.AssertNotCompleted();
            Assert.IsTrue(queue.TryEnqueue("A"));
            Assert.AreEqual("A", await dequeueTask.Timeout());
        }

        [TestMethod]
        public async Task Capacity_zero_DequeueAsync_then_EnqueueAsync()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var dequeueTask = queue.DequeueAsync();
            dequeueTask.AssertNotCompleted();
            await queue.EnqueueAsync("A").Timeout();
            Assert.AreEqual("A", await dequeueTask.Timeout());
        }

        [TestMethod]
        public async Task Capacity_zero_EnqueueAsync_then_TryDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var enqueueTask = queue.EnqueueAsync("A");
            enqueueTask.AssertNotCompleted();
            Assert.IsTrue(queue.TryDequeue(out var value));
            Assert.AreEqual("A", value);
            await enqueueTask.Timeout();
        }

        [TestMethod]
        public async Task Capacity_zero_EnqueueAsync_then_DequeueAsync()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var enqueueTask = queue.EnqueueAsync("A");
            enqueueTask.AssertNotCompleted();
            Assert.AreEqual("A", await queue.DequeueAsync().Timeout());
            await enqueueTask.Timeout();
        }

        [TestMethod]
        public async Task CompleteAllDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.DequeueAsync()).ToList();
            queue.CompleteAllDequeue("X");
            foreach (var task in tasks)
            {
                Assert.AreEqual("X", await task.Timeout());
            }
        }

        [TestMethod]
        public async Task CancelAllDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.DequeueAsync()).ToList();
            queue.CancelAllDequeue(CancellationToken.None);
            foreach (var task in tasks)
            {
                await task.AssertCanceledAsync();
            }
        }

        [TestMethod]
        public async Task CompleteAllEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.EnqueueAsync("A")).ToList();
            queue.CompleteAllEnqueue();
            foreach (var task in tasks)
            {
                await task.Timeout();
            }
        }

        [TestMethod]
        public async Task CancelAllEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.EnqueueAsync("A")).ToList();
            queue.CancelAllEnqueue(CancellationToken.None);
            foreach (var task in tasks)
            {
                await task.AssertCanceledAsync().Timeout();
            }
        }
    }
}
