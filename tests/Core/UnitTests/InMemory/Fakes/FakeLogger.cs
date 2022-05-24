/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNotStandard.Caching.Core.UnitTests.InMemory.Fakes
{
    internal class FakeLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return new FakeScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine(logLevel);
            Debug.WriteLine(state);
            if (exception is not null)
            {
                Debug.WriteLine(exception.ToString());
            }
        }

        internal class FakeScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
