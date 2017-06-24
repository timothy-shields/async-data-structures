using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class NitoTests
    {
        [TestMethod]
        public async Task TestIAsyncWaitQueue1()
        {
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            var task = queue.Enqueue();
            task.AssertNotCompleted();
            queue.Dequeue("A");
            Assert.AreEqual("A", await task.Timeout());
        }

        [TestMethod]
        public async Task TestIAsyncWaitQueue2()
        {
            var gate = new object();
            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            var task = queue.Enqueue(gate, cancellationToken);
            task.AssertNotCompleted();
            queue.Dequeue("A");
            Assert.AreEqual("A", await task.Timeout());
        }

        [TestMethod]
        public async Task TestIAsyncWaitQueue3()
        {
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            var task = queue.Enqueue();
            task.AssertNotCompleted();
            queue.Dequeue("A");
            Assert.AreEqual("A", await task.Timeout());
        }
    }
}