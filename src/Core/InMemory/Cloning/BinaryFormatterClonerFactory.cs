using System;
using System.Collections.Generic;
using System.Text;

namespace DotNotStandard.Caching.Core.InMemory.Cloning
{

	/// <summary>
	/// Implementation of a factory that can be used to return instances
	/// of deep object graph cloners that use BinaryFormatter for cloning
	/// </summary>
	/// <remarks>
	/// Due to potential security issues with the BinaryFormatter class, this factory
	/// returns instances of an implementation that will no longer work on .NET 5 and above.
	/// This will be replaced with a better implementation in the future.
	/// </remarks>
	/// <typeparam name="T">The type of object that will be cloned</typeparam>
	internal class BinaryFormatterClonerFactory<T> : IDeepClonerFactory<T>
	{

		/// <summary>
		/// Return an instance of a deep cloner for the supported type
		/// </summary>
		/// <returns>A cloner that can be used to clone the type</returns>
		public IDeepCloner<T> GetCloner()
		{
			return new BinaryFormatterCloner<T>();
		}

	}

}
