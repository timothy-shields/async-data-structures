using Nito.AsyncEx;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures
{
    /// <summary>
    /// An asynchronous stack with unlimited capacity.
    /// </summary>
    /// <typeparam name="T">The value type of the stack.</typeparam>
    public class AsyncStack<T>
    {
        private Stack<T> stack = new Stack<T>();
        private IAsyncWaitQueue<T> popQueue = new DefaultAsyncWaitQueue<T>();

        private object Gate
        {
            get { return stack; }
        }

        /// <summary>
        /// Constructs an empty stack.
        /// </summary>
        public AsyncStack()
        {
        }

        /// <summary>
        /// Gets the number of values in the stack.
        /// </summary>
        public int Count
        {
            get
            {
                lock (Gate)
                {
                    return stack.Count;
                }
            }
        }

        /// <summary>
        /// Tries to read the value at the top of the stack.
        /// </summary>
        /// <param name="value">The value at the top of the stack.</param>
        /// <returns>True if and only if the stack was not empty.</returns>
        public bool TryPeek(out T value)
        {
            lock (Gate)
            {
                if (stack.Count > 0)
                {
                    value = stack.Peek();
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
        /// Tries to remove the value at the top of the stack.
        /// </summary>
        /// <returns>True if and only if the stack was not empty.</returns>
        public bool TryPop()
        {
            T value;
            return TryPop(out value);
        }

        /// <summary>
        /// Tries to remove the value at the top of the stack.
        /// </summary>
        /// <param name="value">The value at the top of the stack.</param>
        /// <returns>True if and only if the stack was not empty.</returns>
        public bool TryPop(out T value)
        {
            lock (Gate)
            {
                if (stack.Count > 0)
                {
                    value = stack.Pop();
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
        /// Asynchronously removes the value at the top of the stack.
        /// If the stack is currently empty, the caller enters a queue of waiters.
        /// </summary>
        /// <returns>The value at the top of the stack.</returns>
        public Task<T> PopAsync()
        {
            return PopAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously removes the value at the top of the stack.
        /// If the stack is currently empty, the caller enters a queue of waiters.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value at the top of the stack.</returns>
        public Task<T> PopAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskConstants<T>.Canceled;
            }
            lock (Gate)
            {
                if (stack.Count > 0)
                {
                    return Task.FromResult(stack.Pop());
                }
                else
                {
                    return popQueue.Enqueue(Gate, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Adds a value to the top of the stack.
        /// </summary>
        /// <param name="value">The value to add at the top of the stack.</param>
        public void Push(T value)
        {
            lock (Gate)
            {
                if (popQueue.IsEmpty)
                {
                    stack.Push(value);
                }
                else
                {
                    popQueue.Dequeue(value);
                }
            }
        }

        /// <summary>
        /// Completes all waiting PopAsync calls.
        /// </summary>
        /// <param name="value">The value to return to the waiting PopAsync callers.</param>
        public void CompleteAllPop(T value)
        {
            popQueue.DequeueAll(value);
        }

        /// <summary>
        /// Cancels all waiting PopAsync calls.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void CancelAllPop(CancellationToken cancellationToken)
        {
            popQueue.CancelAll(cancellationToken);
        }
    }
}
