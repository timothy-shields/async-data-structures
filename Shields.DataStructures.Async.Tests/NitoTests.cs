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
        public void TestIAsyncWaitQueue1()
        {
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            Task<string> task = queue.Enqueue().AssertNotCompleted();
            queue.Dequeue("A");
            task.AssertResult("A");
        }

        [TestMethod]
        public void TestIAsyncWaitQueue2()
        {
            var gate = new object();
            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            Task<string> task = queue.Enqueue(gate, cancellationToken).AssertNotCompleted();
            queue.Dequeue("A");
            task.AssertResult("A");
        }

        [TestMethod]
        public void TestIAsyncWaitQueue3()
        {
            IAsyncWaitQueue<string> queue = new DefaultAsyncWaitQueue<string>();
            Task<string> task = queue.Enqueue().AssertNotCompleted();
            queue.Dequeue("A");
            var value = await task;
            Assert.AreEqual("A", value);
        }
    }
}