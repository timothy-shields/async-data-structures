using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shields.DataStructures.Async
{
    internal class Disposable : IDisposable
    {
        Action dispose;

        private Disposable(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            dispose();
        }

        public static IDisposable Create(Action dispose)
        {
            return new Disposable(dispose);
        }
    }
}
