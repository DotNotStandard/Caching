using DotNotStandard.Caching.Core.InMemory;
using DotNotStandard.Caching.Core.UnitTests.InMemory.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace DotNotStandard.Caching.Core.UnitTests.InMemory
{

	[TestClass]
	public class InMemoryItemCacheTests
	{

		#region GetItem

		#region Values Types

		[TestMethod]
		public void GetItem_OfIntWhenInitialRetrievalTimesOut_ReturnsDefaultValueOfZero()
		{

			// Arrange
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				null,
				() => { Thread.Sleep(200); return 125; },
				TimeSpan.FromMinutes(2),
				50, 50);
			int actualResult;
			int expectedResult = 0;

			// Act
			Task<int> task1 = new Task<int>(() => { return cache.GetItem(); });
			Task<int> task2 = new Task<int>(() => { Thread.Sleep(50); return cache.GetItem(); });
			// Start the 2 tasks, with task2 likely to have to wait on task1
			task1.Start();
			task2.Start();
			Task.WaitAll(task1, task2);
			actualResult = task2.Result;

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfInt_ReturnsCorrectCachedValue()
		{

			// Arrange
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				null,
				() => { return 125; },
				TimeSpan.FromMinutes(2),
				100, 100);
			int actualResult;
			int expectedResult = 125;

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
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				null,
				() => { return cacheableStruct.GetValue(); },
				TimeSpan.FromMinutes(2));
			int actualResult;
			int expectedResult = 125;

			// Act
			actualResult = cache.GetItem();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public void GetItem_OfCacheableStructRequestedMultipleTimesInSuccession_CallsRetrievalDelegateJustOnce()
		{

			// Arrange
			CacheableStruct cacheableStruct = new CacheableStruct(125);
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				null,
				() => { return cacheableStruct.GetValue(); },
				TimeSpan.FromMinutes(2));
			int actualResult;
			int expectedResult = 1;

			// Act
			_ = cache.GetItem();
			_ = cache.GetItem();
			_ = cache.GetItem();
			_ = cache.GetItem();
			actualResult = cacheableStruct.NumberOfCalls;

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

		#region Reference Types

		[TestMethod]
		public void GetItem_OfCacheableClassWhenInitialRetrievalTimesOut_ReturnsDefaultValueOfNull()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				async () => { await Task.Delay(200); return new CacheableClass(100); },
				() => { Thread.Sleep(200); return new CacheableClass(125); },
				TimeSpan.FromMinutes(2),
				50, 50);
			CacheableClass cacheItem;

			// Act
			Task<CacheableClass> task1 = new Task<CacheableClass>(() => { return cache.GetItem(); });
			Task<CacheableClass> task2 = new Task<CacheableClass>(() => { Thread.Sleep(50); return cache.GetItem(); });
			// Start the 2 tasks, with task2 likely to have to wait on task1
			task1.Start();
			task2.Start();
			Task.WaitAll(task1, task2);
			cacheItem = task2.Result;

			// Assert
			Assert.IsNull(cacheItem);

		}

		[TestMethod]
		public void GetItem_OfCacheableClass_ReturnsCorrectCachedValue()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				async () => { return new CacheableClass(100); },
				() => { return new CacheableClass(125); },
				TimeSpan.FromMinutes(2),
				100, 100);
			CacheableClass cacheItem;
			int actualResult;
			int expectedResult = 125;

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
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				null,
				() => { return new CacheableClass(125); },
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

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
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				null,
				() => { return new CacheableClass(125); },
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

			// Act
			cacheItem = cache.GetItem();
			initialCreatedAt = cacheItem.CreatedAt;
			// Wait for the cache item to expire and then retry
			Thread.Sleep(150);
			cacheItem = cache.GetItem();
			finalCreatedAt = cacheItem.CreatedAt;

			// Assert that the creation values are different
			Assert.AreNotEqual(initialCreatedAt, finalCreatedAt);

		}

		[TestMethod]
		public void GetItem_OfCacheableClassWhenCalledTwice_DoesNotReturnSameInstance()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				null,
				() => { return new CacheableClass(125); },
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

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
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				null,
				() => { return new CacheableClass(125); },
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

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

		#region GetItemAsync

		#region Values Types

		[TestMethod]
		public void GetItemAsync_OfIntWhenInitialRetrievalTimesOut_ReturnsDefaultValueOfZero()
		{

			// Arrange
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				async () => { await Task.Delay(200); return 125; },
				null,
				TimeSpan.FromMinutes(2),
				50, 50);
			int actualResult;
			int expectedResult = 0;

			// Act
			Task<int> task1 = cache.GetItemAsync();
			Task<int> task2 = AwaitDelayAndGetItemFromIntCacheAsync(50, cache);
			// Start the 2 tasks, with task2 likely to have to wait on task1
			Task.WaitAll(task1, task2);
			actualResult = task2.Result;

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		private async Task<int> AwaitDelayAndGetItemFromIntCacheAsync(int delayPeriod, InMemoryItemCache<int> cache)
		{
			await Task.Delay(delayPeriod);
			return await cache.GetItemAsync();
		}

		[TestMethod]
		public async Task GetItemAsync_OfInt_ReturnsCorrectCachedValue()
		{

			// Arrange
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				() => { return Task.FromResult(100); },
				null,
				TimeSpan.FromMinutes(2),
				100, 100);
			int actualResult;
			int expectedResult = 100;

			// Act
			actualResult = await cache.GetItemAsync();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableStruct_ReturnsCorrectCachedValue()
		{

			// Arrange
			CacheableStruct cacheableStruct = new CacheableStruct(100);
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				() => { return Task.FromResult(cacheableStruct.GetValue()); },
				null,
				TimeSpan.FromMinutes(2));
			int actualResult;
			int expectedResult = 100;

			// Act
			actualResult = await cache.GetItemAsync();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableStructRequestedMultipleTimesInSuccession_CallsRetrievalDelegateJustOnce()
		{

			// Arrange
			CacheableStruct cacheableStruct = new CacheableStruct(100);
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				() => { return Task.FromResult(cacheableStruct.GetValue()); },
				null,
				TimeSpan.FromMinutes(2));
			int actualResult;
			int expectedResult = 1;

			// Act
			_ = await cache.GetItemAsync();
			_ = await cache.GetItemAsync();
			_ = await cache.GetItemAsync();
			_ = await cache.GetItemAsync();
			actualResult = cacheableStruct.NumberOfCalls;

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion

		#region Reference Types

		[TestMethod]
		public void GetItemAsync_OfCacheableClassWhenInitialRetrievalTimesOut_ReturnsDefaultValueOfNull()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				async () => { await Task.Delay(200); return new CacheableClass(100); },
				null,
				TimeSpan.FromMinutes(2),
				50, 50);
			CacheableClass cacheItem;

			// Act
			Task<CacheableClass> task1 = cache.GetItemAsync();
			Task<CacheableClass> task2 = AwaitDelayAndGetItemFromCacheableClassCacheAsync(50, cache);
			// Start the 2 tasks, with task2 likely to have to wait on task1
			Task.WaitAll(task1, task2);
			cacheItem = task2.Result;

			// Assert
			Assert.IsNull(cacheItem);

		}

		private async Task<CacheableClass> AwaitDelayAndGetItemFromCacheableClassCacheAsync(int delayPeriod, InMemoryItemCache<CacheableClass> cache)
		{
			await Task.Delay(delayPeriod);
			return await cache.GetItemAsync();
		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableClass_ReturnsCorrectCachedValue()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				() => { return Task.FromResult(new CacheableClass(100)); },
				null,
				TimeSpan.FromMinutes(2),
				100, 100);
			CacheableClass cacheItem;
			int actualResult;
			int expectedResult = 100;

			// Act
			cacheItem = await cache.GetItemAsync();
			actualResult = cacheItem.GetValue();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableClassRequestedMultipleTimesInSuccession_CallsRetrievalDelegateJustOnce()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				() => { return Task.FromResult(new CacheableClass(100)); },
				null,
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

			// Act
			cacheItem = await cache.GetItemAsync();
			initialCreatedAt = cacheItem.CreatedAt;
			_ = cache.GetItemAsync();
			_ = cache.GetItemAsync();
			cacheItem = await cache.GetItemAsync();
			finalCreatedAt = cacheItem.CreatedAt;

			// Assert
			Assert.AreEqual(initialCreatedAt, finalCreatedAt);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableClassWhenCacheExpires_CallsRetrievalDelegateAgain()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				() => { return Task.FromResult(new CacheableClass(100)); },
				null,
				TimeSpan.FromMilliseconds(100));
			DateTime initialCreatedAt;
			DateTime finalCreatedAt;
			CacheableClass cacheItem;

			// Act
			cacheItem = await cache.GetItemAsync();
			initialCreatedAt = cacheItem.CreatedAt;
			// Wait for the cache item to expire and then retry
			Thread.Sleep(150);
			cacheItem = await cache.GetItemAsync();
			finalCreatedAt = cacheItem.CreatedAt;

			// Assert that the creation values are different
			Assert.AreNotEqual(initialCreatedAt, finalCreatedAt);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableClassWhenCalledTwice_DoesNotReturnSameInstance()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				() => { return Task.FromResult(new CacheableClass(100)); },
				null,
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

			// Act
			cacheItem1 = await cache.GetItemAsync();
			cacheItem2 = await cache.GetItemAsync();

			// Assert that full, deep cloning is being performed
			Assert.AreNotEqual(cacheItem1, cacheItem2);
			Assert.AreNotEqual(cacheItem1.Child, cacheItem2.Child);

		}

		[TestMethod]
		public async Task GetItemAsync_OfCacheableClassWhenCalledTwice_CreatesPerfectClone()
		{

			// Arrange
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				() => { return Task.FromResult(new CacheableClass(100)); },
				null,
				TimeSpan.FromMilliseconds(100));
			CacheableClass cacheItem1;
			CacheableClass cacheItem2;

			// Act
			cacheItem1 = await cache.GetItemAsync();
			cacheItem2 = await cache.GetItemAsync();

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
			InMemoryItemCache<int> cache = new InMemoryItemCache<int>(
				null,
				() => { cachedValue += 25; return cachedValue; },
				TimeSpan.FromMinutes(2),
				100, 100);
			int actualResult;
			int expectedResult = 150;

			// Act
			_ = cache.GetItem();
			cache.Invalidate();
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
			InMemoryItemCache<CacheableClass> cache = new InMemoryItemCache<CacheableClass>(
				null,
				() => { cachedValue += 25; return new CacheableClass(cachedValue); },
				TimeSpan.FromMinutes(2));
			CacheableClass cachedClass;
			int actualResult;
			int expectedResult = 150;

			// Act
			_ = cache.GetItem();
			cache.Invalidate();
			cachedClass = cache.GetItem();
			actualResult = cachedClass.GetValue();

			// Assert
			Assert.AreEqual(expectedResult, actualResult);

		}

		#endregion
		
		#endregion

	}
}
