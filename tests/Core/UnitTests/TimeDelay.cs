/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNotStandard.Caching.Core.UnitTests
{
    internal static class TimeDelay
    {

        /// <summary>
        /// Wait for a specified number of milliseconds before returning
        /// </summary>
        /// <remarks>
        /// Thread sleeps in GitHub actions seem to be ignored or behave differently, so this 
        /// method is used to make tests wait for a specified period both when running tests 
        /// locally and when tests run within GitHub actions
        /// </remarks>
        /// <param name="milliseconds">The number of millseconds to wait</param>
        public static void WaitFor(int milliseconds)
        {
            long loopCount = long.MinValue;
            DateTime endAt = DateTime.Now.AddMilliseconds(milliseconds);

            if (milliseconds < 1) return;

            while (DateTime.Now < endAt)
            {
                Thread.Sleep(1);
                if (loopCount == long.MaxValue)
                {
                    loopCount = long.MinValue;
                }
                loopCount++;
            }
        }
    }
}
