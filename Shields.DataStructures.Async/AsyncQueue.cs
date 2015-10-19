using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    public class AsyncQueue<T>
    {
        private Queue<T> queue = new Queue<T>();
        private IAsyncWaitQueue<T> dequeueQueue = new DefaultAsyncWaitQueue<T>();

        private object Gate
        {
            get { return queue; }
        }

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

        public bool TryDequeue()
        {
            T value;
            return TryDequeue(out value);
        }

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

        public Task<T> DequeueAsync()
        {
            return DequeueAsync(CancellationToken.None);
        }

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
                    dequeueQueue.Dequeue(value).Dispose();
                }
            }
        }

        /// <summary>
        /// Returns a disposable that completes all waiting DequeueAsync calls.
        /// </summary>
        /// <param name="value">The value to return to the waiting DequeueAsync callers.</param>
        /// <returns>The disposable that completes all waiting DequeueAsync calls.</returns>
        public IDisposable CompleteAllDequeue(T value)
        {
            return dequeueQueue.DequeueAll(value);
        }
        
        /// <summary>
        /// Returns a disposable that cancels all waiting DequeueAsync calls.
        /// </summary>
        /// <returns>The disposable that cancels all waiting DequeueAsync calls.</returns>
        public IDisposable CancelAllDequeue()
        {
            return dequeueQueue.CancelAll();
        }
    }
}
