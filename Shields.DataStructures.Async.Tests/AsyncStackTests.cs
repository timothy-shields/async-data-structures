using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class AsyncStackTests
    {
        [TestMethod]
        public void Count_empty()
        {
            var stack = new AsyncStack<string>();
            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void Count_nonempty()
        {
            var stack = new AsyncStack<string>();
            stack.Push("A");
            stack.Push("B");
            stack.Push("C");
            stack.Push("D");
            Assert.IsTrue(stack.TryPop());
            Assert.AreEqual(3, stack.Count);
        }

        [TestMethod]
        public void TryPeek_empty()
        {
            var stack = new AsyncStack<string>();
            string value;
            Assert.IsFalse(stack.TryPeek(out value));
        }

        [TestMethod]
        public void TryPop_empty()
        {
            var stack = new AsyncStack<string>();
            Assert.IsFalse(stack.TryPop());
        }

        [TestMethod]
        public void TryPeek_nonempty()
        {
            var stack = new AsyncStack<string>();
            stack.Push("A");
            string value;
            Assert.IsTrue(stack.TryPeek(out value));
            Assert.AreEqual("A", value);
            Assert.AreEqual(1, stack.Count);
        }

        [TestMethod]
        public void TryPop_nonempty()
        {
            var stack = new AsyncStack<string>();
            stack.Push("A");
            string value;
            Assert.IsTrue(stack.TryPop(out value));
            Assert.AreEqual("A", value);
            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public async Task PopAsync_empty_canceled()
        {
            var stack = new AsyncStack<string>();
            using (var cts = new CancellationTokenSource())
            {
                var task = stack.PopAsync(cts.Token);
                cts.Cancel();
                stack.Push("A");
                await task.AssertCanceledAsync();
            }
        }

        [TestMethod]
        public async Task PopAsync_already_canceled()
        {
            var stack = new AsyncStack<string>();
            stack.Push("A");
            var task = stack.PopAsync(new CancellationToken(true));
            await task.AssertCanceledAsync();
        }

        [TestMethod]
        public async Task PopAsync_canceled_after_completion()
        {
            var stack = new AsyncStack<string>();
            stack.Push("A");
            using (var cts = new CancellationTokenSource())
            {
                var task = stack.PopAsync(cts.Token);
                cts.Cancel();
                Assert.AreEqual("A", await task.Timeout());
            }
        }

        [TestMethod]
        public async Task PopAsync_before_Push()
        {
            var stack = new AsyncStack<string>();
            var task = stack.PopAsync();
            stack.Push("A");
            Assert.AreEqual("A", await task.Timeout());
        }

        [TestMethod]
        public async Task PopAsync_handled_in_order_of_caller()
        {
            var stack = new AsyncStack<string>();
            var values = new List<string> { "A", "B", "C" };
            var tasks = values.Select(_ => stack.PopAsync()).ToList();
            for (int i = 0; i < values.Count; i++)
            {
                tasks[i].AssertNotCompleted();
                stack.Push(values[i]);
                Assert.AreEqual(values[i], await tasks[i].Timeout());
            }
        }

        [TestMethod]
        public async Task Last_in_first_out()
        {
            var stack = new AsyncStack<string>();
            var values = new List<string> { "A", "B", "C" };
            for (int i = 0; i < values.Count; i++)
            {
                stack.Push(values[i]);
            }
            for (int i = values.Count - 1; i >= 0; i--)
            {
                Assert.AreEqual(values[i], await stack.PopAsync().Timeout());
            }
        }

        [TestMethod]
        public async Task CompleteAllPop()
        {
            var stack = new AsyncStack<string>();
            var tasks = Enumerable.Range(0, 3).Select(_ => stack.PopAsync()).ToList();
            stack.CompleteAllPop("X");
            foreach (var task in tasks)
            {
                Assert.AreEqual("X", await task.Timeout());
            }
        }

        [TestMethod]
        public async Task CancelAllPop()
        {
            var stack = new AsyncStack<string>();
            var tasks = Enumerable.Range(0, 3).Select(_ => stack.PopAsync()).ToList();
            stack.CancelAllPop(CancellationToken.None);
            foreach (var task in tasks)
            {
                await task.AssertCanceledAsync();
            }
        }
    }
}
