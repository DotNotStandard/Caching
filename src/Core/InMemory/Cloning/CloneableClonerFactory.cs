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
	/// Implementation of a factory that can be used to return instances of deep
	/// object graph cloners that support the ICloneable interface for cloning
	/// </summary>
	/// <remarks>
	/// This cloner is only suitable for types that correctly implement the 
	/// ICloneable interface to create deep clones - a full copy of themselves
	/// and all of their children, throughout the object graph.
	/// If only a shallow copy is created then the cached item will not be 
	/// truly thread-safe, as the same object instances can be returned
	/// across multiple threads
	/// </remarks>
	/// <typeparam name="T">The type of object that will be cloned</typeparam>
	internal class CloneableClonerFactory<T> : IDeepClonerFactory<T>
	{

		/// <summary>
		/// Return an instance of a deep cloner for the supported type
		/// </summary>
		/// <returns>A cloner that can be used to clone the type</returns>
		public IDeepCloner<T> GetCloner()
		{
			if (!typeof(ICloneable).IsAssignableFrom(typeof(T)))
			{
				throw new InvalidOperationException("This type does not support ICloneable!");
			}
			return new CloneableCloner<T>();
		}

	}

}
