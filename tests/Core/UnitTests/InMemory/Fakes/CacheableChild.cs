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
