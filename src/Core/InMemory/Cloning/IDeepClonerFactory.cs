using System;
using System.Collections.Generic;
using System.Text;

namespace DotNotStandard.Caching.Core.InMemory.Cloning
{
	public interface IDeepClonerFactory<T>
	{

		IDeepCloner<T> GetCloner();

	}
}
