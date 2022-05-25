/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using DotNotStandard.Caching.Core.InMemory;
using DotNotStandard.Caching.Core.InMemory.Cloning;
using DotNotStandard.Caching.Core.UnitTests.InMemory.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace DotNotStandard.Caching.Core.UnitTests.InMemory
{

	[TestClass]
	public class AutoRefreshingItemCacheTests
	{

		#region GetItem

		#region Values Types

		[TestMethod]
		public void GetItem_OfIntWhenInitialialisationTimesOut_ReturnsInitialValueOfZero()
		{

			// Arrange
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				async (ct) => { await TimeDelay.WaitForAsync(500); return 125; },
				0,
				TimeSpan.FromMinutes(2)
				);
			int actualResult;
			int expectedResult = 0;

			cache.StartInitialisation();
			cache.TryCompleteInitialisation(TimeSpan.FromMilliseconds(50));

			// Act
			actualResult = cache.GetItem();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfInt_ReturnsCorrectCachedValue()
		{

			// Arrange
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				(ct) => { return Task.FromResult(125); },
				0,
				TimeSpan.FromMinutes(2)
				);
			int actualResult;
			int expectedResult = 125;

			cache.Initialise();

			// Act
			actualResult = cache.GetItem();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfCacheableStruct_ReturnsCorrectCachedValue()
		{

			// Arrange
			CacheableStruct cacheableStruct = new CacheableStruct(125);
			AutoRefreshingItemCache<CacheableStruct> cache = new AutoRefreshingItemCache<CacheableStruct>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(cacheableStruct); },
				new CacheableStruct(1000),
				TimeSpan.FromMinutes(2)
				);
			int actualResult;
			int expectedResult = 125;

			cache.Initialise();

			// Act
			actualResult = cache.GetItem().GetValue();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfCacheableStructRequestedMultipleTimesInSuccession_CallsRetrievalDelegateJustOnce()
		{

			// Arrange
			CacheableStruct cacheableStruct = new CacheableStruct(125);
			AutoRefreshingItemCache<CacheableStruct> cache = new AutoRefreshingItemCache<CacheableStruct>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(cacheableStruct.GetSelf()); },
				new CacheableStruct(1000),
				TimeSpan.FromMinutes(2)
				);
			int actualResult;
			int expectedResult = 1;

			cache.Initialise();

			// Act
			_ = cache.GetItem();
			_ = cache.GetItem();
			_ = cache.GetItem();
			_ = cache.GetItem();
			actualResult = cacheableStruct.NumberOfGetSelfCalls;

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

		#region Reference Types

		[TestMethod]
		public void GetItem_OfCacheableClassWhenInitialisationTimesOut_ReturnsInitialValueOfNull()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				async (ct) => { await TimeDelay.WaitForAsync(500); return new CacheableClass(100); },
				null,
				TimeSpan.FromMinutes(2)
				);
			CacheableClass actualResult;

			cache.StartInitialisation();
			cache.TryCompleteInitialisation(TimeSpan.FromMilliseconds(50));

			// Act
			actualResult = cache.GetItem();

			// Assert
			Assert.IsNull(actualResult);

		}

		[TestMethod]
		public void GetItem_OfCacheableClass_ReturnsCorrectCachedValue()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(new CacheableClass(125)); },
				new CacheableClass(100),
				TimeSpan.FromMinutes(2)
				);
			CacheableClass cacheItem;
			int actualResult;
			int expectedResult = 125;

			cache.Initialise();

			// Act
			cacheItem = cache.GetItem();
			actualResult = cacheItem.GetValue();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfCacheableClassRequestedMultipleTimesInSuccession_CallsRetrievalDelegateJustOnce()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(new CacheableClass(125)); },
				null,
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

			cache.Initialise();

			// Act
			cacheItem = cache.GetItem();
			initialCreatedAt = cacheItem.CreatedAt;
			_ = cache.GetItem();
			_ = cache.GetItem();
			cacheItem = cache.GetItem();
			finalCreatedAt = cacheItem.CreatedAt;

			// Assert
			Assert.AreEqual(initialCreatedAt, finalCreatedAt);

		}

		[TestMethod]
		public void GetItem_OfCacheableClassWhenCacheExpires_CallsRetrievalDelegateAgain()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(new CacheableClass(125)); },
				null,
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

			cache.Initialise();

			// Act
			cacheItem = cache.GetItem();
			initialCreatedAt = cacheItem.CreatedAt;
			// Wait for the cache item to expire and then retry
			TimeDelay.WaitFor(150);
			cacheItem = cache.GetItem();
			finalCreatedAt = cacheItem.CreatedAt;

			// Assert that the creation values are different
			Assert.AreNotEqual(initialCreatedAt, finalCreatedAt);

		}

		[TestMethod]
		public void GetItem_OfCacheableClassWhenCalledTwice_DoesNotReturnSameInstance()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(new CacheableClass(125)); },
				null,
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

			cache.Initialise();

			// Act
			cacheItem1 = cache.GetItem();
			cacheItem2 = cache.GetItem();

			// Assert that full, deep cloning is being performed
			Assert.AreNotEqual(cacheItem1, cacheItem2);
			Assert.AreNotEqual(cacheItem1.Child, cacheItem2.Child);

		}

		[TestMethod]
		public void GetItem_OfCacheableClassWhenCalledTwice_CreatesPerfectClone()
		{

			// Arrange
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { return Task.FromResult(new CacheableClass(125)); },
				null,
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

			cache.Initialise();

			// Act
			cacheItem1 = cache.GetItem();
			cacheItem2 = cache.GetItem();

			// Assert that the clone worked correctly
			Assert.AreEqual(cacheItem1.GetValue(), cacheItem2.GetValue());
			Assert.AreEqual(cacheItem1.NumberOfCalls, cacheItem2.NumberOfCalls);
			Assert.AreEqual(cacheItem1.CreatedAt, cacheItem2.CreatedAt);

		}

		#endregion

		#endregion

		#region Invalidate

		#region Value Types

		[TestMethod]
		public void Invalidate_OfIntWhenCalledInvalidatedAndCalledAgain_ReturnsSecondCachedValue()
		{

			// Arrange
			int cachedValue = 100;
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				(ct) => { cachedValue += 25; return Task.FromResult(cachedValue); },
				0,
				TimeSpan.FromMinutes(2)
				);
			int actualResult;
			int expectedResult = 150;

			cache.Initialise();

			// Act
			_ = cache.GetItem();
			cache.Invalidate();
			TimeDelay.WaitFor(50);
			actualResult = cache.GetItem();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

		#region Reference Types

		[TestMethod]
		public void Invalidate_OfCacheableClassWhenCalledInvalidatedAndCalledAgain_ReturnsSecondCachedValue()
		{

			// Arrange
			int cachedValue = 100;
			AutoRefreshingItemCache<CacheableClass> cache = new AutoRefreshingItemCache<CacheableClass>(
				new FakeLogger(),
				(ct) => { cachedValue += 25; return Task.FromResult(new CacheableClass(cachedValue)); },
				null,
				TimeSpan.FromMinutes(2)
				);
			CacheableClass cachedClass;
			int actualResult;
			int expectedResult = 150;

			cache.Initialise();

			// Act
			_ = cache.GetItem();
			cache.Invalidate();
			TimeDelay.WaitFor(50);
			cachedClass = cache.GetItem();
			actualResult = cachedClass.GetValue();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

		#endregion

		#region Initialisation

		[TestMethod]
		public void Initialise_CacheLoadThrowsException_TimesOut()
		{
			// Arrange
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				(ct) => { throw new Exception("Test exception raised during cache load tests"); },
				0,
				TimeSpan.FromMinutes(30)
				);
			bool actualResult;
			bool expectedResult = false;

			cache.StartInitialisation();

			// Act
			actualResult = cache.TryCompleteInitialisation(TimeSpan.FromMilliseconds(50));

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void TryCompleteInitialisation_ImmediateCacheLoad_DoesNotTimeOut()
		{
			// Arrange
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				(ct) => { return Task.FromResult(125); },
				0,
				TimeSpan.FromMinutes(30)
				);
			bool actualResult;
			bool expectedResult = true;

			cache.StartInitialisation();

			// Act
			actualResult = cache.TryCompleteInitialisation(TimeSpan.FromMilliseconds(50));

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void TryCompleteInitialisation_VerySlowCacheLoad_TimesOut()
        {
			// Arrange
			AutoRefreshingItemCache<int> cache = new AutoRefreshingItemCache<int>(
				new FakeLogger(),
				new NonCloningClonerFactory<int>(),
				async (ct) => { await TimeDelay.WaitForAsync(30000); return 125; },
				0,
				TimeSpan.FromMinutes(30)
				);
			bool actualResult;
			bool expectedResult = false;

			cache.StartInitialisation();

			// Act
			actualResult = cache.TryCompleteInitialisation(TimeSpan.FromMilliseconds(50));

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

	}
}
