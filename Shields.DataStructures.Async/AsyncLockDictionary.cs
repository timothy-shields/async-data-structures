using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    /// <summary>
    /// A dictionary of keyed mutual exclusion locks that are compatible with async.
    /// Note that these locks are not recursive!
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    public class AsyncLockDictionary<TKey>
    {
        private readonly Dictionary<TKey, Entry> dictionary = new Dictionary<TKey, Entry>();

        private class Entry
        {
            public AsyncLock KeyGate = new AsyncLock();
            public int RefCount = 0;
        }

        /// <summary>
        /// Asynchronously acquires the lock. Returns a disposable that releases the
        /// lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> LockAsync(TKey key)
        {
            return LockAsync(key, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously acquires the lock. Returns a disposable that releases the
        /// lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken)
        {
            return new AwaitableDisposable<IDisposable>(ImplLockAsync(key, cancellationToken));
        }

        /// <summary>
        /// Synchronously acquires the lock. Returns a disposable that releases the
        /// lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable Lock(TKey key)
        {
            return Lock(key, CancellationToken.None);
        }

        /// <summary>
        /// Synchronously acquires the lock. Returns a disposable that releases the
        /// lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable Lock(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = entry.KeyGate.Lock(cancellationToken);
                return new Releaser(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        private async Task<IDisposable> ImplLockAsync(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = await entry.KeyGate.LockAsync(cancellationToken).ConfigureAwait(false);
                return new Releaser(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        private object DictionaryGate
        {
            get { return dictionary; }
        }

        private Entry GetEntryRef(TKey key)
        {
            lock (DictionaryGate)
            {
                Entry entry;
                if (!dictionary.TryGetValue(key, out entry))
                {
                    entry = new Entry();
                    dictionary.Add(key, entry);
                }
                entry.RefCount++;
                return entry;
            }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLockDictionary<TKey> dictionary;
            private readonly TKey key;
            private readonly Entry entry;
            private IDisposable handle;

            public Releaser(AsyncLockDictionary<TKey> dictionary, TKey key, Entry entry, IDisposable handle)
            {
                this.dictionary = dictionary;
                this.key = key;
                this.entry = entry;
                this.handle = handle;
            }

            public void Dispose()
            {
                if (handle != null)
                {
                    handle.Dispose();
                    dictionary.ReleaseEntryRef(key, entry);
                    handle = null;
                }
            }
        }

        private void ReleaseEntryRef(TKey key, Entry entry)
        {
            lock (DictionaryGate)
            {
                entry.RefCount--;
                if (entry.RefCount == 0)
                {
                    dictionary.Remove(key);
                }
            }
        }
    }
}
