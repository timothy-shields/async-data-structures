using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shields.DataStructures
{
    public class AsyncStack<T>
    {
        private Stack<T> stack = new Stack<T>();
        private IAsyncWaitQueue<T> popQueue = new DefaultAsyncWaitQueue<T>();

        private object Gate
        {
            get { return stack; }
        }

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

        public bool TryPop()
        {
            T value;
            return TryPop(out value);
        }

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

        public Task<T> PopAsync()
        {
            return PopAsync(CancellationToken.None);
        }

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
                    popQueue.Dequeue(value).Dispose();
                }
            }
        }

        public IDisposable CompleteAllPop(T value)
        {
            return popQueue.DequeueAll(value);
        }

        public IDisposable CancelAllPop()
        {
            return popQueue.CancelAll();
        }
    }
}
