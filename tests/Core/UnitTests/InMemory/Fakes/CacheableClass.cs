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

	[Serializable]
	internal class CacheableClass
	{

		private int _value;

		public int NumberOfCalls { get; private set; }

		public DateTime CreatedAt { get; private set; }

		public object Child { get; private set; }

		public CacheableClass(int value)
		{
			_value = value;
			NumberOfCalls = 0;
			CreatedAt = DateTime.Now;
			Child = new object();
		}

		public int GetValue()
		{
			NumberOfCalls++;
			return _value;
		}

	}

}
