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

	internal struct CacheableStruct : ICloneable
	{

		private int _value;

		public int NumberOfGetValueCalls { get; private set; }

		public int NumberOfGetSelfCalls { get; private set; }

		public CacheableStruct(int value)
		{
			_value = value;
			NumberOfGetValueCalls = 0;
			NumberOfGetSelfCalls = 0;
		}

		public int GetValue()
		{
			NumberOfGetValueCalls++;
			return _value;
		}

		public CacheableStruct GetSelf()
		{
			NumberOfGetSelfCalls++;
			return this;
		}

		#region ICloneable Interface

		public object Clone()
		{
			 return this.MemberwiseClone();
		}

        #endregion

    }

}
