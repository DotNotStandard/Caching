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
	internal class CacheableClass : ICloneable
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
			Child = new CacheableChild();
		}

		public int GetValue()
		{
			NumberOfCalls++;
			return _value;
		}

		#region ICloneable Interface
		
		public object Clone()
        {
			CacheableClass clone;

			clone = (CacheableClass)this.MemberwiseClone();
			clone.Child = CloneChild(Child);

			return clone;
        }

        #endregion

        #region Private Helper Methods

        private object CloneChild(object child)
        {
			ICloneable cloneableChild;

			if (child is null)
			{
				return null;
			}

			cloneableChild = child as ICloneable;
			if (cloneableChild is null) throw new InvalidOperationException("Child does not implement ICloneable!");
			child = cloneableChild.Clone();

			return child;
		}

		#endregion
	}

}
