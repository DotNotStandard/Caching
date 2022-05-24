/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNotStandard.Caching.Core.UnitTests.InMemory.Fakes
{
    internal class CacheableChild : ICloneable
    {
        public int Id { get; set; }

        #region ICloneable Interface

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion

    }
}
