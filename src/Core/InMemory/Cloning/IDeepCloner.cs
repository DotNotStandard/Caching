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
