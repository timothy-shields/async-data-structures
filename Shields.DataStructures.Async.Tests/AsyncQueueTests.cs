using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class AsyncQueueTests
    {
        [TestMethod]
        public void Count_empty()
        {
            var queue = new AsyncQueue<string>();
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Count_nonempty()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("A");
            queue.Enqueue("B");
            queue.Enqueue("C");
            queue.Enqueue("D");
            Assert.IsTrue(queue.TryDequeue());
            Assert.AreEqual(3, queue.Count);
        }

        [TestMethod]
        public void TryPeek_empty()
        {
            var queue = new AsyncQueue<string>();
            string value;
            Assert.IsFalse(queue.TryPeek(out value));
        }

        [TestMethod]
        public void TryDequeue_empty()
        {
            var queue = new AsyncQueue<string>();
            Assert.IsFalse(queue.TryDequeue());
        }

        [TestMethod]
        public void TryPeek_nonempty()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("A");
            string value;
            Assert.IsTrue(queue.TryPeek(out value));
            Assert.AreEqual("A", value);
            Assert.AreEqual(1, queue.Count);
        }

        [TestMethod]
        public void TryDequeue_nonempty()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("A");
            string value;
            Assert.IsTrue(queue.TryDequeue(out value));
            Assert.AreEqual("A", value);
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public async Task DequeueAsync_empty_canceled()
        {
            var queue = new AsyncQueue<string>();
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                queue.Enqueue("A");
                await task.AssertCanceledAsync();
            }
        }

        [TestMethod]
        public async Task DequeueAsync_already_canceled()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("A");
            var task = queue.DequeueAsync(new CancellationToken(true));
            await task.AssertCanceledAsync();
        }

        [TestMethod]
        public async Task DequeueAsync_canceled_after_completion()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("A");
            using (var cts = new CancellationTokenSource())
            {
                var task = queue.DequeueAsync(cts.Token);
                cts.Cancel();
                Assert.AreEqual("A", await task.Timeout());
            }
        }

        [TestMethod]
        public async Task DequeueAsync_before_Enqueue()
        {
            var queue = new AsyncQueue<string>();
            var task = queue.DequeueAsync();
            task.AssertNotCompleted();
            queue.Enqueue("A");
            Assert.AreEqual("A", await task.Timeout());
        }

        [TestMethod]
        public async Task DequeueAsync_handled_in_order_of_caller()
        {
            var queue = new AsyncQueue<string>();
            var values = new List<string> { "A", "B", "C" };
            var tasks = values.Select(_ => queue.DequeueAsync()).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                tasks[i].AssertNotCompleted();
                queue.Enqueue(values[i]);
                Assert.AreEqual(values[i], await tasks[i].Timeout());
            }
        }

        [TestMethod]
        public async Task First_in_first_out()
        {
            var queue = new AsyncQueue<string>();
            var values = new List<string> { "A", "B", "C" };
            for (int i = 0; i < values.Count; i++)
            {
                queue.Enqueue(values[i]);
            }
            for (int i = 0; i < values.Count; i++)
            {
                Assert.AreEqual(values[i], await queue.DequeueAsync().Timeout());
            }
        }

        [TestMethod]
        public async Task CompleteAllDequeue()
        {
            var queue = new AsyncQueue<string>();
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
            var queue = new AsyncQueue<string>();
            var tasks = Enumerable.Range(0, 3).Select(_ => queue.DequeueAsync()).ToList();
            queue.CancelAllDequeue(CancellationToken.None);
            foreach (var task in tasks)
            {
                await task.AssertCanceledAsync();
            }
        }
    }
}
