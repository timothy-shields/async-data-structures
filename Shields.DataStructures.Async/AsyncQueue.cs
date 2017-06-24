using Nito.AsyncEx;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    /// <summary>
    /// An asynchronous queue with unlimited capacity.
    /// </summary>
    /// <typeparam name="T">The value type of the queue.</typeparam>
    public class AsyncQueue<T>
    {
        private Queue<T> queue = new Queue<T>();
        private IAsyncWaitQueue<T> dequeueQueue = new DefaultAsyncWaitQueue<T>();

        private object Gate
        {
            get { return queue; }
        }

        /// <summary>
        /// Constructs an empty queue.
        /// </summary>
        public AsyncQueue()
        {
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
        /// Tries to read the value at the front of the queue.
        /// </summary>
        /// <param name="value">The value at the front of the queue.</param>
        /// <returns>True if and only if the queue was not empty.</returns>
        public bool TryPeek(out T value)
        {
            lock (Gate)
            {
                if (queue.Count > 0)
                {
                    value = queue.Peek();
                    return true;
                }
                else
                {
                    value = default(T);
                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to remove the value at the front of the queue.
        /// </summary>
        /// <returns>True if and only if the queue was not empty.</returns>
        public bool TryDequeue()
        {
            T value;
            return TryDequeue(out value);
        }

        /// <summary>
        /// Tries to remove the value at the front of the queue.
        /// </summary>
        /// <param name="value">The value at the front of the queue.</param>
        /// <returns>True if and only if the queue was not empty.</returns>
        public bool TryDequeue(out T value)
        {
            lock (Gate)
            {
                if (queue.Count > 0)
                {
                    value = queue.Dequeue();
                    return true;
                }
                else
                {
                    value = default(T);
                    return false;
                }
            }
        }
        
        /// <summary>
        /// Asynchronously removes the value at the front of the queue.
        /// If the queue is currently empty, the caller enters a queue of waiters.
        /// </summary>
        /// <returns>The value at the front of the queue.</returns>
        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously removes the value at the front of the queue.
        /// If the queue is currently empty, the caller enters a queue of waiters.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value at the front of the queue.</returns>
        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskConstants<T>.Canceled;
            }
            lock (Gate)
            {
                if (queue.Count > 0)
                {
                    return Task.FromResult(queue.Dequeue());
                }
                else
                {
                    return dequeueQueue.Enqueue(Gate, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Adds a value to the back of the queue.
        /// </summary>
        /// <param name="value">The value to add at the back of the queue.</param>
        public void Enqueue(T value)
        {
            lock (Gate)
            {
                if (dequeueQueue.IsEmpty)
                {
                    queue.Enqueue(value);
                }
                else
                {
                    dequeueQueue.Dequeue(value);
                }
            }
        }

        /// <summary>
        /// Completes all waiting DequeueAsync calls.
        /// </summary>
        /// <param name="value">The value to return to the waiting DequeueAsync callers.</param>
        public void CompleteAllDequeue(T value)
        {
            dequeueQueue.DequeueAll(value);
        }

        /// <summary>
        /// Cancels all waiting DequeueAsync calls.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void CancelAllDequeue(CancellationToken cancellationToken)
        {
            dequeueQueue.CancelAll(cancellationToken);
        }
    }
}
