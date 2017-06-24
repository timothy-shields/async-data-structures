using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Shields.DataStructures.Async.Tests
{
    public static class TaskAssertionExtensions
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(1);

        public static async Task<TResult> Timeout<TResult>(this Task<TResult> task)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                } else {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static async Task Timeout(this Task task)
        {
            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task) {
                    timeoutCancellationTokenSource.Cancel();
                    await task;  // Very important in order to propagate exceptions
                    return;
                } else {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static void AssertNotCompleted(this Task task)
        {
            Assert.IsFalse(task.IsCompleted);
        }

        public static async Task AssertCanceledAsync(this Task task)
        {
            try
            {
                await task.Timeout();
            }
            catch (TaskCanceledException)
            {
            }
            Assert.IsTrue(task.IsCanceled);
        }
    }
}
