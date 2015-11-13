using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    /// <summary>
    /// A dictionary of keyed reader/writer locks that are compatible with async.
    /// Note that these locks are not recursive!
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    public class AsyncReaderWriterLockDictionary<TKey>
    {
        private readonly Dictionary<TKey, Entry> dictionary = new Dictionary<TKey, Entry>();

        internal class Entry
        {
            public AsyncReaderWriterLock KeyGate = new AsyncReaderWriterLock();
            public int RefCount = 0;
        }

        /// <summary>
        /// Asynchronously acquires the lock as a reader. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> ReaderLockAsync(TKey key)
        {
            return ReaderLockAsync(key, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously acquires the lock as a reader. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> ReaderLockAsync(TKey key, CancellationToken cancellationToken)
        {
            return new AwaitableDisposable<IDisposable>(ImplReaderLockAsync(key, cancellationToken));
        }

        /// <summary>
        /// Synchronously acquires the lock as a reader. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable ReaderLock(TKey key)
        {
            return ReaderLock(key, CancellationToken.None);
        }

        /// <summary>
        /// Synchronously acquires the lock as a reader. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable ReaderLock(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = entry.KeyGate.ReaderLock(cancellationToken);
                return new Releaser(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        private async Task<IDisposable> ImplReaderLockAsync(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = await entry.KeyGate.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
                return new Releaser(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        /// <summary>
        /// The disposable which manages the upgradeable reader lock.
        /// </summary>
        public sealed class UpgradeableReaderKey : IDisposable
        {
            private readonly AsyncReaderWriterLockDictionary<TKey> dictionary;
            private readonly TKey key;
            private readonly Entry entry;
            private AsyncReaderWriterLock.UpgradeableReaderKey handle;
            
            internal UpgradeableReaderKey(AsyncReaderWriterLockDictionary<TKey> dictionary, TKey key, Entry entry, AsyncReaderWriterLock.UpgradeableReaderKey handle)
            {
                this.dictionary = dictionary;
                this.key = key;
                this.entry = entry;
                this.handle = handle;
            }

            /// <summary>
            /// Gets a value indicating whether this lock has been upgraded to a write lock.
            /// </summary>
            public bool Upgraded
            {
                get { return handle.Upgraded; }
            }

            /// <summary>
            /// Release the lock.
            /// </summary>
            public void Dispose()
            {
                if (handle != null)
                {
                    handle.Dispose();
                    dictionary.ReleaseEntryRef(key, entry);
                    handle = null;
                }
            }

            /// <summary>
            /// Synchronously upgrades the reader lock to a writer lock. Returns a disposable
            /// that downgrades the writer lock to a reader lock when disposed. This method
            /// may block the calling thread.
            /// </summary>
            public IDisposable Upgrade()
            {
                return handle.Upgrade();
            }

            /// <summary>
            /// Synchronously upgrades the reader lock to a writer lock. Returns a disposable
            /// that downgrades the writer lock to a reader lock when disposed. This method
            /// may block the calling thread.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the upgrade. If this is already set,
            /// then this method will attempt to upgrade immediately (succeeding if the lock
            /// is currently available).
            /// </param>
            /// <returns></returns>
            public IDisposable Upgrade(CancellationToken cancellationToken)
            {
                return handle.Upgrade(cancellationToken);
            }

            /// <summary>
            /// Upgrades the reader lock to a writer lock. Returns a disposable that downgrades
            /// the writer lock to a reader lock when disposed.
            /// </summary>
            public AwaitableDisposable<IDisposable> UpgradeAsync()
            {
                return handle.UpgradeAsync();
            }

            /// <summary>
            /// Upgrades the reader lock to a writer lock. Returns a disposable that downgrades
            /// the writer lock to a reader lock when disposed.
            /// </summary>
            /// <param name="cancellationToken">
            /// The cancellation token used to cancel the upgrade. If this is already set,
            /// then this method will attempt to upgrade immediately (succeeding if the lock
            /// is currently available).
            /// </param>
            /// <returns></returns>
            public AwaitableDisposable<IDisposable> UpgradeAsync(CancellationToken cancellationToken)
            {
                return handle.UpgradeAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Asynchronously acquires the lock as a reader with the option to upgrade.
        /// Returns a key that can be used to upgrade and downgrade the lock, and releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>
        /// A key that can be used to upgrade and downgrade this lock, and releases the
        /// lock when disposed.
        /// </returns>
        public AwaitableDisposable<UpgradeableReaderKey> UpgradeableReaderLockAsync(TKey key)
        {
            return UpgradeableReaderLockAsync(key, CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously acquires the lock as a reader with the option to upgrade.
        /// Returns a key that can be used to upgrade and downgrade the lock, and releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>
        /// A key that can be used to upgrade and downgrade this lock, and releases the
        /// lock when disposed.
        /// </returns>
        public AwaitableDisposable<UpgradeableReaderKey> UpgradeableReaderLockAsync(TKey key, CancellationToken cancellationToken)
        {
            return new AwaitableDisposable<UpgradeableReaderKey>(ImplUpgradeableReaderLockAsync(key, cancellationToken));
        }

        /// <summary>
        /// Synchronously acquires the lock as a reader with the option to upgrade.
        /// Returns a key that can be used to upgrade and downgrade the lock, and releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>
        /// A key that can be used to upgrade and downgrade this lock, and releases the
        /// lock when disposed.
        /// </returns>
        public UpgradeableReaderKey UpgradeableReaderLock(TKey key)
        {
            return UpgradeableReaderLock(key, CancellationToken.None);
        }

        /// <summary>
        /// Synchronously acquires the lock as a reader with the option to upgrade.
        /// Returns a key that can be used to upgrade and downgrade the lock, and releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>
        /// A key that can be used to upgrade and downgrade this lock, and releases the
        /// lock when disposed.
        /// </returns>
        public UpgradeableReaderKey UpgradeableReaderLock(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = entry.KeyGate.UpgradeableReaderLock(cancellationToken);
                return new UpgradeableReaderKey(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        private async Task<UpgradeableReaderKey> ImplUpgradeableReaderLockAsync(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = await entry.KeyGate.UpgradeableReaderLockAsync(cancellationToken).ConfigureAwait(false);
                return new UpgradeableReaderKey(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously acquires the lock as a writer. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> WriterLockAsync(TKey key)
        {
            return WriterLockAsync(key, CancellationToken.None);
        }    

        /// <summary>
        /// Asynchronously acquires the lock as a writer. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> WriterLockAsync(TKey key, CancellationToken cancellationToken)
        {
            return new AwaitableDisposable<IDisposable>(ImplWriterLockAsync(key, cancellationToken));
        }

        /// <summary>
        /// Synchronously acquires the lock as a writer. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable WriterLock(TKey key)
        {
            return WriterLock(key, CancellationToken.None);
        }

        /// <summary>
        /// Synchronously acquires the lock as a writer. Returns a disposable that releases
        /// the lock when disposed.
        /// </summary>
        /// <param name="key">The key to lock.</param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the lock. If this is already set, then
        /// this method will attempt to take the lock immediately (succeeding if the
        /// lock is currently available).
        /// </param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public IDisposable WriterLock(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = entry.KeyGate.WriterLock(cancellationToken);
                return new Releaser(this, key, entry, handle);
            }
            catch
            {
                ReleaseEntryRef(key, entry);
                throw;
            }
        }

        private async Task<IDisposable> ImplWriterLockAsync(TKey key, CancellationToken cancellationToken)
        {
            var entry = GetEntryRef(key);
            try
            {
                var handle = await entry.KeyGate.WriterLockAsync(cancellationToken).ConfigureAwait(false);
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
            private readonly AsyncReaderWriterLockDictionary<TKey> dictionary;
            private readonly TKey key;
            private readonly Entry entry;
            private IDisposable handle;

            public Releaser(AsyncReaderWriterLockDictionary<TKey> dictionary, TKey key, Entry entry, IDisposable handle)
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
