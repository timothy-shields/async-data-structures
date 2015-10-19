using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async.Tests
{
    public static class TaskAssertionExtensions
    {
        public static void AssertResult<T>(this Task<T> task, T expected)
        {
            task.AssertSuccess();
            Assert.AreEqual(expected, task.Result);
        }

        public static void AssertNotCompleted(this Task task)
        {
            Assert.IsFalse(task.IsCompleted);
        }

        public static void AssertSuccess(this Task task)
        {
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
        }

        public static void AssertCanceled(this Task task)
        {
            Assert.IsTrue(task.IsCanceled);
        }

        public static void AssertFaulted(this Task task)
        {
            Assert.IsTrue(task.IsFaulted);
        }
    }
}
