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

	/// <summary>
	/// Implementation of a factory that can be used to return instances of a
	/// cloner that performs NO cloning, bypassing the work it would require.
	/// </summary>
	/// <remarks>
	/// This cloner is only suitable when the cached item does not need to be 
	/// used across multiple threads, or where the item is already thread-safe. 
	/// The same object instances would be returned across all calling threads
	/// </remarks>
	/// <typeparam name="T">The type of object that is in use</typeparam>
	public class NonCloningClonerFactory<T> : IDeepClonerFactory<T>
	{

		/// <summary>
		/// Return an instance of a deep cloner for the supported type
		/// </summary>
		/// <returns>A cloner that can be used to clone the type</returns>
		public IDeepCloner<T> GetCloner()
		{
			return new NonCloningCloner<T>();
		}

	}

}
