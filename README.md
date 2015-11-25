# `Shields.DataStructures.Async` [![Build status](https://ci.appveyor.com/api/projects/status/tlisbau4su4tt2sl/branch/master?svg=true)](https://ci.appveyor.com/project/timothy-shields/async-data-structures/branch/master)

`Shields.DataStructures.Async` is a C#/.NET library that provides the following async data structures. It is [available via NuGet](https://www.nuget.org/packages/Shields.DataStructures.Async/).

# Async Stack

```C#
class AsyncStack<T>
{
    AsyncStack()
    int Count { get; }
    bool TryPeek(out T value)
    bool TryPop()
    bool TryPop(out T value)
    Task<T> PopAsync()
    Task<T> PopAsync(CancellationToken cancellationToken)
    void Push(T value)
    IDisposable CompleteAllPop(T value)
    IDisposable CancelAllPop()
}
```

# Async Queue

```C#
class AsyncQueue<T>
{
    AsyncQueue()
    int Count { get; }
    bool TryPeek(out T value)
    bool TryDequeue()
    bool TryDequeue(out T value)
    Task<T> DequeueAsync()
    Task<T> DequeueAsync(CancellationToken cancellationToken)
    void Enqueue(T value)
    IDisposable CompleteAllDequeue(T value)
    IDisposable CancelAllDequeue()
}
```

# Async Bounded Queue

```C#
class AsyncBoundedQueue<T>
{
    AsyncBoundedQueue(int capacity)
    int Count { get; }
    int Capacity { get; }
    bool TryPeek(out T value)
    bool TryDequeue()
    bool TryDequeue(out T value)
    Task<T> DequeueAsync()
    Task<T> DequeueAsync(CancellationToken cancellationToken)
    bool TryEnqueue(T value)
    Task EnqueueAsync(T value)
    Task EnqueueAsync(T value, CancellationToken cancellationToken)
    IDisposable CompleteAllDequeue(T value)
    IDisposable CancelAllDequeue()
    IDisposable CompleteAllEnqueue()
    IDisposable CancelAllEnqueue()
}
```

# Async Lock Dictionary

```C#
class AsyncLockDictionary<TKey>
{
    AsyncLockDictionary()
    AsyncLockDictionary(IEqualityComparer<TKey> comparer)
    IEqualityComparer<TKey> Comparer { get; }
    AwaitableDisposable<IDisposable> LockAsync(TKey key)
    AwaitableDisposable<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken)
    IDisposable Lock(TKey key)
    IDisposable Lock(TKey key, CancellationToken cancellationToken)
}
```

# Async Reader-Writer Lock Dictionary

```C#
class AsyncReaderWriterLockDictionary<TKey>
{
    AsyncReaderWriterLockDictionary()
    AsyncReaderWriterLockDictionary(IEqualityComparer<TKey> comparer)
    IEqualityComparer<TKey> Comparer { get; }
    AwaitableDisposable<IDisposable> ReaderLockAsync(TKey key)
    AwaitableDisposable<IDisposable> ReaderLockAsync(TKey key, CancellationToken cancellationToken)
    IDisposable ReaderLock(TKey key)
    IDisposable ReaderLock(TKey key, CancellationToken cancellationToken)
    class UpgradeableReaderKey : IDisposable
    {
        bool Upgraded { get; }
        void Dispose()
        IDisposable Upgrade()
        IDisposable Upgrade(CancellationToken cancellationToken)
        AwaitableDisposable<IDisposable> UpgradeAsync()
        AwaitableDisposable<IDisposable> UpgradeAsync(CancellationToken cancellationToken)
    }
    AwaitableDisposable<UpgradeableReaderKey> UpgradeableReaderLockAsync(TKey key)
    AwaitableDisposable<UpgradeableReaderKey> UpgradeableReaderLockAsync(TKey key, CancellationToken cancellationToken)
    UpgradeableReaderKey UpgradeableReaderLock(TKey key)
    UpgradeableReaderKey UpgradeableReaderLock(TKey key, CancellationToken cancellationToken)
    AwaitableDisposable<IDisposable> WriterLockAsync(TKey key)
    AwaitableDisposable<IDisposable> WriterLockAsync(TKey key, CancellationToken cancellationToken)
    IDisposable WriterLock(TKey key)
    IDisposable WriterLock(TKey key, CancellationToken cancellationToken)
}
```
