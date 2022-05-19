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
	/// Implementation of IDeepCloner<typeparamref name="T"/> using ICloneable
	/// Note that complex objects MUST override the Clone method to perform a deep clone 
	/// in order to be compatible with this cloner
	/// </summary>
	/// <typeparam name="T">The type of object to be cloned</typeparam>
	internal class CloneableCloner<T> : IDeepCloner<T>
	{

		/// <summary>
		/// Create a deep clone of a source object, with any children also cloned
		/// throughout the object graph of the source object
		/// </summary>
		/// <param name="source">The object that is to be cloned</param>
		/// <returns>The clone of the original object, or default if the input is null</returns>
		public T DeepClone(T source)
		{
			ICloneable cloneable;
			T clone;

			if (source == null) return default(T);

			cloneable = source as ICloneable;
			if (cloneable is null) throw new InvalidOperationException("Object does not implement ICloneable!");

			clone = (T)cloneable.Clone();

			return clone;
		}

	}
}
