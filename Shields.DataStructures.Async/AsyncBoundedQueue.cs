using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    /// <summary>
    /// An asynchronous queue with limited capacity.
    /// </summary>
    /// <typeparam name="T">The value type of the queue.</typeparam>
    public class AsyncBoundedQueue<T>
    {
        private readonly int capacity;
        private Queue<T> queue;
        private IAsyncWaitQueue<T> dequeueQueue = new DefaultAsyncWaitQueue<T>();
        private IAsyncWaitQueue<object> enqueueQueue = new DefaultAsyncWaitQueue<object>();

        private object Gate
        {
            get { return queue; }
        }

        /// <summary>
        /// Constructs an empty queue with the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the queue.</param>
        public AsyncBoundedQueue(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", "The capacity must be nonnegative.");
            }
            this.capacity = capacity;
            queue = new Queue<T>(capacity);
        }

        /// <summary>
        /// Gets the number of values in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                lock (Gate)
                {
                    return queue.Count;
                }
            }
        }

        /// <summary>
        /// Gets the capacity of the queue.
        /// </summary>
        public int Capacity
        {
            get
            {
                return capacity;
            }
        }

        //public bool TryPeek(out T value)
        //{
        //    lock (Gate)
        //    {
        //        if (queue.Count > 0)
        //        {
        //            value = queue.Peek();
        //            return true;
        //        }
        //        else
        //        {
        //            value = default(T);
        //            return false;
        //        }
        //    }
        //}

        //public bool TryDequeue(out T value)
        //{
        //    lock (Gate)
        //    {
        //        if (0 < queue.Count && queue.Count < capacity)
        //        {
        //            value = queue.Dequeue();
        //            return true;
        //        }
        //        else if (queue.Count == capacity)
        //        {
        //            using (enqueueQueue.Dequeue())
        //            {
        //                value = queue.Dequeue();
        //                return true;
        //            }
        //        }
        //        else // if (queue.Count == 0)
        //        {
        //            value = default(T);
        //            return false;
        //        }
        //    }
        //}

        /// <summary>
        /// Dequeues a value from the queue. If the queue is not empty,
        /// the returned task will already be completed. If the queue is
        /// empty, the returned task will complete when a value becomes
        /// available.
        /// </summary>
        /// <returns>A task whose result will be the dequeued value.</returns>
        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }

        /// <summary>
        /// Dequeues a value from the queue. If the queue is not empty,
        /// the returned task will already be completed. If the queue is
        /// empty, the returned task will complete when a value becomes
        /// available.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result will be the dequeued value.</returns>
        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskConstants<T>.Canceled;
            }
            lock (Gate)
            {
                if (0 < queue.Count && queue.Count < capacity)
                {
                    return Task.FromResult(queue.Dequeue());
                }
                else if (queue.Count == 0)
                {
                    if (enqueueQueue.IsEmpty)
                    {
                        return dequeueQueue.Enqueue(Gate, cancellationToken);
                    }
                    else
                    {
                        using (enqueueQueue.Dequeue())
                        {
                            return dequeueQueue.Enqueue(Gate, cancellationToken);
                        }
                    }
                }
                else // if (queue.Count == capacity)
                {
                    if (enqueueQueue.IsEmpty)
                    {
                        return Task.FromResult(queue.Dequeue());
                    }
                    else
                    {
                        using (enqueueQueue.Dequeue())
                        {
                            return Task.FromResult(queue.Dequeue());
                        }
                    }
                }
            }
        }

        //public bool TryEnqueue(T value)
        //{
        //    lock (Gate)
        //    {
        //        if (queue.Count < capacity)
        //        {
        //            queue.Enqueue(value);
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}

        /// <summary>
        /// Enqueues a value into the queue. If the queue is not at full,
        /// capacity the returned task will already be completed. If the
        /// queue is at full capacity, the returned task will complete
        /// when the value is enqueued.
        /// </summary>
        /// <param name="value">The value to enqueue.</param>
        /// <returns>A task that will complete when the value has been enqueued.</returns>
        public Task EnqueueAsync(T value)
        {
            return EnqueueAsync(value, CancellationToken.None);
        }

        /// <summary>
        /// Enqueues a value into the queue. If the queue is not at full,
        /// capacity the returned task will already be completed. If the
        /// queue is at full capacity, the returned task will complete
        /// when the value is enqueued.
        /// </summary>
        /// <param name="value">The value to enqueue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that will complete when the value has been enqueued.</returns>
        public Task EnqueueAsync(T value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskConstants.Canceled;
            }
            lock (Gate)
            {
                if (dequeueQueue.IsEmpty && queue.Count < capacity)
                {
                    queue.Enqueue(value);
                    return TaskConstants.Completed;
                }
                else if (!dequeueQueue.IsEmpty)
                {
                    dequeueQueue.Dequeue(value).Dispose();
                    return TaskConstants.Completed;
                }
                else // if (queue.Count == capacity)
                {
                    return enqueueQueue.Enqueue(Gate, cancellationToken).ContinueWith(
                        (task, state) =>
                        {
                            var tuple = (Tuple<AsyncBoundedQueue<T>, T>)state;
                            var captured_this = tuple.Item1;
                            var captured_value = tuple.Item2;
                            if (!dequeueQueue.IsEmpty)
                            {
                                captured_this.dequeueQueue.Dequeue(captured_value).Dispose();
                            }
                            else
                            {
                                captured_this.queue.Enqueue(value);
                            }
                        },
                        Tuple.Create(this, value),
                        cancellationToken,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled,
                        TaskScheduler.Current);
                }
            }
        }

        /// <summary>
        /// Returns a disposable that completes all waiting dequeue operations with the specified value.
        /// </summary>
        /// <param name="value">The value to return from all waiting dequeue operations.</param>
        /// <returns>The disposable that completes all waiting dequeue operations.</returns>
        public IDisposable CompleteAllDequeue(T value)
        {
            return dequeueQueue.DequeueAll(value);
        }

        /// <summary>
        /// Returns a disposable that cancels all waiting dequeue operations.
        /// </summary>
        /// <returns>The disposable that cancels all waiting dequeue operations.</returns>
        public IDisposable CancelAllDequeue()
        {
            return dequeueQueue.CancelAll();
        }

        /// <summary>
        /// Returns a disposable that completes all waiting enqueue operations.
        /// </summary>
        /// <returns>The disposable that completes all waiting enqueue operations.</returns>
        public IDisposable CompleteAllEnqueue()
        {
            return enqueueQueue.DequeueAll();
        }

        /// <summary>
        /// Returns a disposable that cancels all waiting enqueue operations.
        /// </summary>
        /// <returns>The disposable that cancels all waiting enqueue operations.</returns>
        public IDisposable CancelAllEnqueue()
        {
            return enqueueQueue.CancelAll();
        }
    }
}
