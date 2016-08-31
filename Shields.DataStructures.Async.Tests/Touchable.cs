using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    public class Touchable
    {
        private int refCount = 0;
        private readonly int maxRefCount;
        private int touchCount = 0;

        public Touchable(int maxRefCount)
        {
            this.maxRefCount = maxRefCount;
        }

        public int TouchCount
        {
            get { return touchCount; }
        }

        public async Task TouchAsync()
        {
            Interlocked.Increment(ref touchCount);
            Assert.IsTrue(Interlocked.Increment(ref refCount) <= maxRefCount);
            await Task.Yield();
            Assert.IsTrue(Interlocked.Decrement(ref refCount) >= 0);
        }
    }
}
