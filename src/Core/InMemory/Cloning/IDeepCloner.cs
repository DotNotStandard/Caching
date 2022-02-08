/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNotStandard.Caching.Core.InMemory.Cloning
{
	public interface IDeepCloner<T>
	{

		T DeepClone(T source);

	}
}
