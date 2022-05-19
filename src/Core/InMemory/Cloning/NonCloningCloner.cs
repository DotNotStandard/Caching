/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DotNotStandard.Caching.Core.InMemory.Cloning
{

	/// <summary>
	/// Implementation of IDeepCloner<typeparamref name="T"/> that does no cloning.
	/// This can be used by consumers when they don't feel a need to clone objects.
	/// This is very fast, but offers no thread safety guarantee.
	/// </summary>
	/// <typeparam name="T">The type of object to be cloned</typeparam>
	internal class NonCloningCloner<T> : IDeepCloner<T>
	{

		/// <summary>
		/// Return the source object, uncloned
		/// </summary>
		/// <param name="source">The object that is to be cloned</param>
		/// <returns>The original object, unchanged</returns>
		public T DeepClone(T source)
		{
			return source;
		}

	}
}
