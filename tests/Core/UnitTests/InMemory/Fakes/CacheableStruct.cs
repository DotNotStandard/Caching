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

	internal struct CacheableStruct
	{

		private int _value;

		public int NumberOfCalls { get; private set; }

		public CacheableStruct(int value)
		{
			_value = value;
			NumberOfCalls = 0;
		}

		public int GetValue()
		{
			NumberOfCalls++;
			return _value;
		}

	}

}
