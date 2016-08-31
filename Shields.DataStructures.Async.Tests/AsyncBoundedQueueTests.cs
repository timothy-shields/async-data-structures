using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class AsyncBoundedQueueTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Capacity_must_be_nonnegative()
        {
            new AsyncBoundedQueue<string>(-1);
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
        public void Count_nonempty()
        {
            var queue = new AsyncBoundedQueue<string>(4);
            queue.EnqueueAsync("A").AssertSuccess();
            queue.EnqueueAsync("B").AssertSuccess();
            queue.EnqueueAsync("C").AssertSuccess();
            queue.EnqueueAsync("D").AssertSuccess();
            queue.DequeueAsync().AssertSuccess();
            Assert.AreEqual(3, queue.Count);
        }

        [TestMethod]
        public void DequeueAsync_empty_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                queue.EnqueueAsync("A").AssertSuccess();
                task.AssertCanceled();
            }
        }

        [TestMethod]
        public void DequeueAsync_already_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            queue.EnqueueAsync("A").AssertSuccess();
            var task = queue.DequeueAsync(new CancellationToken(true));
            task.AssertCanceled();
        }

        [TestMethod]
        public void DequeueAsync_canceled_after_completion()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            queue.EnqueueAsync("A").AssertSuccess();
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                task.AssertResult("A");
            }
        }

        [TestMethod]
        public void EnqueueAsync_full_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(2);
            queue.EnqueueAsync("A").AssertSuccess();
            queue.EnqueueAsync("B").AssertSuccess();
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("C", cts.Token);
                cts.Cancel();
                task.AssertCanceled();
            }
        }

        [TestMethod]
        public void EnqueueAsync_not_full_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            queue.EnqueueAsync("A").AssertSuccess();
            queue.EnqueueAsync("B").AssertSuccess();
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("C", cts.Token);
                cts.Cancel();
                task.AssertSuccess();
            }
        }

        [TestMethod]
        public void EnqueueAsync_already_canceled()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var task = queue.EnqueueAsync("A", new CancellationToken(true));
            task.AssertCanceled();
        }

        [TestMethod]
        public void EnqueueAsync_canceled_after_completion()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.EnqueueAsync("A", cts.Token);
                cts.Cancel();
                task.AssertSuccess();
            }
        }

        [TestMethod]
        public void TryDequeue_before_Enqueue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            Assert.IsFalse(queue.TryDequeue());
        }

        [TestMethod]
        public void DequeueAsync_before_Enqueue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var task = queue.DequeueAsync();
            queue.EnqueueAsync("A").AssertSuccess();
            task.AssertResult("A");
        }

        [TestMethod]
        public void DequeueAsync_handled_in_order_of_caller()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C" };
            var tasks = values.Select(_ => queue.DequeueAsync()).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                tasks[i].AssertNotCompleted();
                queue.EnqueueAsync(values[i]).AssertSuccess();
                tasks[i].AssertResult(values[i]);
            }
        }

        [TestMethod]
        public void First_in_first_out()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C" };
            for (int i = 0; i < values.Count; i++)
            {
                queue.EnqueueAsync(values[i]).AssertSuccess();
            }
            for (int i = 0; i < values.Count; i++)
            {
                queue.DequeueAsync().AssertResult(values[i]);
            }
        }

        [TestMethod]
        public void First_in_first_out_exceeding_capacity()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var values = new List<string> { "A", "B", "C", "D", "E", "F" };
            var enqueueTasks = values.Select(queue.EnqueueAsync).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                if (i < queue.Capacity)
                {
                    enqueueTasks[i].AssertSuccess();
                }
                else
                {
                    enqueueTasks[i].AssertNotCompleted();
                }
            }
            for (int i = 0; i < values.Count; i++)
            {
                queue.DequeueAsync().AssertResult(values[i]);
                if (i + queue.Capacity < values.Count)
                {
                    enqueueTasks[i + queue.Capacity].AssertSuccess();
                }
            }
        }

        [TestMethod]
        public void Capacity_zero_DequeueAsync_then_TryEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var dequeueTask = queue.DequeueAsync().AssertNotCompleted();
            Assert.IsTrue(queue.TryEnqueue("A"));
            dequeueTask.AssertResult("A");
        }

        [TestMethod]
        public void Capacity_zero_DequeueAsync_then_EnqueueAsync()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var dequeueTask = queue.DequeueAsync().AssertNotCompleted();
            queue.EnqueueAsync("A").AssertSuccess();
            dequeueTask.AssertResult("A");
        }

        [TestMethod]
        public void Capacity_zero_EnqueueAsync_then_TryDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var enqueueTask = queue.EnqueueAsync("A").AssertNotCompleted();
            string value;
            Assert.IsTrue(queue.TryDequeue(out value));
            Assert.AreEqual("A", value);
            enqueueTask.AssertSuccess();
        }

        [TestMethod]
        public void Capacity_zero_EnqueueAsync_then_DequeueAsync()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var enqueueTask = queue.EnqueueAsync("A").AssertNotCompleted();
            queue.DequeueAsync().AssertResult("A");
            enqueueTask.AssertSuccess();
        }

        [TestMethod]
        public void CompleteAllDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.DequeueAsync()).ToList();
            queue.CompleteAllDequeue("X");
            foreach (var task in tasks)
            {
                task.AssertResult("X");
            }
        }

        [TestMethod]
        public void CancelAllDequeue()
        {
            var queue = new AsyncBoundedQueue<string>(3);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.DequeueAsync()).ToList();
            queue.CancelAllDequeue(CancellationToken.None);
            foreach (var task in tasks)
            {
                task.AssertCanceled();
            }
        }

        [TestMethod]
        public void CompleteAllEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.EnqueueAsync("A")).ToList();
            queue.CompleteAllEnqueue();
            foreach (var task in tasks)
            {
                task.AssertSuccess();
            }
        }

        [TestMethod]
        public void CancelAllEnqueue()
        {
            var queue = new AsyncBoundedQueue<string>(0);
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.EnqueueAsync("A")).ToList();
            queue.CancelAllEnqueue(CancellationToken.None);
            foreach (var task in tasks)
            {
                task.AssertCanceled();
            }
        }
    }
}
