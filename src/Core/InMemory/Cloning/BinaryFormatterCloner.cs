using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DotNotStandard.Caching.Core.InMemory.Cloning
{

	/// <summary>
	/// Implementation of IDeepCloner<typeparamref name="T"/> using BinaryFormatter
	/// Due to potential security issues in BinaryFormatter, this is not of use in .NET 5
	/// and above, and should be replaced with a better implementation in future
	/// </summary>
	/// <typeparam name="T">The type of object to be cloned</typeparam>
	internal class BinaryFormatterCloner<T> : IDeepCloner<T>
	{

		/// <summary>
		/// Create a deep clone of a source object, with any children also cloned
		/// throughout the object graph of the source object
		/// </summary>
		/// <param name="source">The object that is to be cloned</param>
		/// <returns>The clone of the original object, or default if the input is null</returns>
		public T DeepClone(T source)
		{
			T clone;
			BinaryFormatter formatter;
			MemoryStream memoryStream;

			// Shortcut if the item is null; needed for reference types, as null does not serialise
			if (source == null) return default(T);

			// Clone the object and return the clone
			formatter = new BinaryFormatter();
			memoryStream = new MemoryStream();
			formatter.Serialize(memoryStream, source);
			memoryStream.Seek(0, SeekOrigin.Begin);
			clone = (T)formatter.Deserialize(memoryStream);

			return clone;
		}

	}
}
