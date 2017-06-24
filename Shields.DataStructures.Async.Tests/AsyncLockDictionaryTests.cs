using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;

namespace Shields.DataStructures.Async.Tests
{
    [TestClass]
    public class AsyncLockDictionaryTests
    {
        Task SpamAsync(int count, Func<Task> actionAsync)
        {
            return Task.WhenAll(Enumerable.Range(0, count).Select(_ => Task.Run(actionAsync)));
        }

        [TestMethod]
        public async Task Can_lock_once()
        {
            var dict = new AsyncLockDictionary<string>();
            await dict.LockAsync("A").AsTask().Timeout();
        }
        
        [TestMethod]
        public async Task Cannot_lock_twice_concurrently()
        {
            var dict = new AsyncLockDictionary<string>();
            await dict.LockAsync("A").AsTask().Timeout();
            dict.LockAsync("A").AsTask().AssertNotCompleted();
        }

        [TestMethod]
        public async Task Cannot_lock_twice_in_parallel()
        {
            var dict = new AsyncLockDictionary<string>();
            var touchable = new Touchable(1);
            await SpamAsync(10000, async () =>
            {
                using (await dict.LockAsync("A"))
                {
                    await touchable.TouchAsync();
                }
            });
            Assert.AreEqual(10000, touchable.TouchCount);
        }

        [TestMethod]
        public async Task Second_lock_completes_when_first_lock_is_released()
        {
            var dict = new AsyncLockDictionary<string>();
            var handle = await dict.LockAsync("A").AsTask().Timeout();
            var task = dict.LockAsync("A").AsTask();
            task.AssertNotCompleted();
            handle.Dispose();
            await task;
        }

        [TestMethod]
        public async Task Can_lock_different_keys_concurrently()
        {
            var dict = new AsyncLockDictionary<string>();
            await dict.LockAsync("A").AsTask().Timeout();
            await dict.LockAsync("B").AsTask().Timeout();
        }

        [TestMethod]
        public async Task Can_lock_different_keys_in_parallel()
        {
            var dict = new AsyncLockDictionary<int>();
            var touchable = new Touchable(10);
            await Task.WhenAll(Enumerable.Range(0, 10)
                .Select(i => SpamAsync(1000, async () =>
                {
                    using (await dict.LockAsync(i))
                    {
                        await touchable.TouchAsync();
                    }
                })));
            Assert.AreEqual(10000, touchable.TouchCount);
        }
    }
}
