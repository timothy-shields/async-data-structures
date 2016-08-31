using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    public static class TaskAssertionExtensions
    {
        public static Task<T> AssertResult<T>(this Task<T> task, T expected)
        {
            task.AssertSuccess();
            Assert.AreEqual(expected, task.Result);
            return task;
        }

        public static Task AssertNotCompleted(this Task task)
        {
            Assert.IsFalse(task.IsCompleted);
            return task;
        }

        public static Task<T> AssertNotCompleted<T>(this Task<T> task)
        {
            Assert.IsFalse(task.IsCompleted);
            return task;
        }

        public static Task AssertSuccess(this Task task)
        {
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            return task;
        }

        public static Task<T> AssertSuccess<T>(this Task<T> task)
        {
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            return task;
        }

        public static Task AssertCanceled(this Task task)
        {
            Assert.IsTrue(task.IsCanceled);
            return task;
        }

        public static Task AssertFaulted(this Task task)
        {
            Assert.IsTrue(task.IsFaulted);
            return task;
        }
    }
}
