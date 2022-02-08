/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using DotNotStandard.Caching.Core.InMemory.Cloning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNotStandard.Caching.Core.InMemory
{
	/// <summary>
	/// In-memory cache for reusable objects (especially read-only objects)
	/// The return is disconnected from any other instances, to avoid threading issues
	/// </summary>
	/// <typeparam name="T">The type of object that is to be cached</typeparam>
	/// <remarks>This is not intended for parameterised objects. Instead, it's for parameterless lookup tables and similar</remarks>
	public class InMemoryItemCache<T>
	{

		private CacheEntry<T> _cacheEntry;
		private readonly TimeSpan _cachingPeriod;
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
		private readonly Func<Task<T>> _asyncRetrievalDelegate;
		private readonly Func<T> _syncRetrievalDelegate;
		private readonly int _repeatRetrievalTimeout;
		private readonly IDeepClonerFactory<T> _clonerFactory;

		#region Constructors

		private InMemoryItemCache()
		{
			// Hide the default constructor; it's not to be used
		}

		/// <summary>
		/// Initialise the in-memory cache using the delegates and caching period provided
		/// </summary>
		/// <param name="asyncRetrievalDelegate">The async delegate to await for retrieval of the object, e.g. from a database</param>
		/// <param name="syncRetrievalDelegate">The syncnronous delegate for retrieval of the object, e.g. from a database</param>
		/// <param name="cachingPeriod">The period for which caching is to be performed</param>
		/// <param name="initialRetrievalTimeout">The number of milliseconds to wait before timing out the first retrieval before returning the default</param>
		/// <param name="repeatRetrievalTimeout">The number of milliseconds to wait before timing out on repeat retrievals before returning stale data</param>
		/// <remarks>Either async or sync delegate may be null, but not both</remarks>
		public InMemoryItemCache(Func<Task<T>> asyncRetrievalDelegate, Func<T> syncRetrievalDelegate, TimeSpan cachingPeriod,
			int initialRetrievalTimeout = int.MaxValue, int repeatRetrievalTimeout = 100)
		{
			if (asyncRetrievalDelegate is null && syncRetrievalDelegate is null)
				throw new ArgumentException("Either a synchronous or asynchronous delegate must be provided as a minimum!", "Delegates");

			_asyncRetrievalDelegate = asyncRetrievalDelegate;
			_syncRetrievalDelegate = syncRetrievalDelegate;
			_cachingPeriod = cachingPeriod;
			_repeatRetrievalTimeout = repeatRetrievalTimeout;

			// Create an instance of the default object cloner, for use when returning objects
			_clonerFactory = new BinaryFormatterClonerFactory<T>();

			// Create a default cache entry so that no null reference exceptions are encountered on first use
			// This item is marked as immediately expiring, so that it always triggers a request to the source
			_cacheEntry = new CacheEntry<T>(default(T), DateTime.MinValue, initialRetrievalTimeout);
		}

		#endregion

		#region Exposed Properties and Methods

		/// <summary>
		/// Retrieve a unique clone of the object from the cache, or from the source as required
		/// </summary>
		/// <returns>An item of the correct type, from the cache if the caching rules have been met</returns>
		public async Task<T> GetItemAsync()
		{
			bool lockAttained;
			T itemToCache;
			CacheEntry<T> cacheEntry;

			if (_asyncRetrievalDelegate is null) throw new InvalidOperationException("Cannot use the asynchronous GetItemAsync method unless an asynchronous delegate is provided!");

			// Capture the cached entry reference and then check if it is currently valid
			cacheEntry = _cacheEntry;
			if (DateTime.Now < cacheEntry.ExpiresAt)
			{
				// Return a clone of the cached item
				return DeepCloneOf(cacheEntry.CachedItem);
			}

			// Item has expired; need to retrieve the item and then cache it
			lockAttained = await _semaphore.WaitAsync(cacheEntry.RetrievalTimeout).ConfigureAwait(false);
			if (lockAttained)
			{
				try
				{
					// Recheck that the item was not retrieved by another thread while we waited
					cacheEntry = _cacheEntry;
					if (DateTime.Now < cacheEntry.ExpiresAt)
					{
						// Return a clone of the newly cached item
						return DeepCloneOf(_cacheEntry.CachedItem);
					}

					// Still expired; retrieve the item and update the cache to signal reuse
					itemToCache = await _asyncRetrievalDelegate().ConfigureAwait(false);
					cacheEntry = new CacheEntry<T>(itemToCache, DateTime.Now.Add(_cachingPeriod), _repeatRetrievalTimeout);
					_cacheEntry = cacheEntry;
				}
				finally
				{
					// Release the semaphore to allow other threads to retrieve in the future
					_semaphore.Release();
				}
			}

			// The result is a clone of the newly cached item (or stale data, if locking timed out)
			return DeepCloneOf(cacheEntry.CachedItem);
		}

		/// <summary>
		/// Retrieve a unique clone of the object from the cache, or from the source as required
		/// </summary>
		/// <returns>An item of the correct type, from the cache if the caching rules have been met</returns>
		public T GetItem()
		{
			bool lockAttained;
			T itemToCache;
			CacheEntry<T> cacheEntry;

			if (_syncRetrievalDelegate is null) throw new InvalidOperationException("Cannot use the synchronous GetItem method unless a synchronous delegate is provided!");

			// Capture the cached entry reference and then check if it is currently valid
			cacheEntry = _cacheEntry;
			if (DateTime.Now < cacheEntry.ExpiresAt)
			{
				// Return a clone of the cached item
				return DeepCloneOf(cacheEntry.CachedItem);
			}

			// Item has expired; need to retrieve the item and then cache it
			lockAttained = _semaphore.Wait(cacheEntry.RetrievalTimeout);
			if (lockAttained)
			{
				try
				{
					// Recheck that the item was not retrieved by another thread while we waited
					cacheEntry = _cacheEntry;
					if (DateTime.Now < cacheEntry.ExpiresAt)
					{
						// Return a clone of the newly cached item
						return DeepCloneOf(_cacheEntry.CachedItem);
					}

					// Still expired; retrieve the item and update the cache to signal reuse
					itemToCache = _syncRetrievalDelegate();
					cacheEntry = new CacheEntry<T>(itemToCache, DateTime.Now.Add(_cachingPeriod), _repeatRetrievalTimeout);
					// NOTE: assignment of a reference type is an atomic operation; no lock required here
					_cacheEntry = cacheEntry;
				}
				finally
				{
					// Release the semaphore to allow other threads to retrieve in the future
					_semaphore.Release();
				}
			}

			// The result is a clone of the newly cached item (or stale data if locking timed out)
			return DeepCloneOf(cacheEntry.CachedItem);
		}

		/// <summary>
		/// Invalidate the cache, so that data is refreshed when next required
		/// </summary>
		public void Invalidate()
		{
			_cacheEntry.Invalidate();
		}

		#endregion

		#region Private Helper Methods

		/// <summary>
		/// Create a full clone of an object, thereby ensuring that the type
		/// is not impacted by use across multiple threads, or multiple separate methods
		/// </summary>
		/// <param name="item">The item that is to be cloned</param>
		/// <returns>A deep/full clone of the object</returns>
		private T DeepCloneOf(T item)
		{
			T clone;
			IDeepCloner<T> cloner;

			cloner = _clonerFactory.GetCloner();
			clone = cloner.DeepClone(item);

			return clone;
		}

		#endregion

		#region Private Helper Class

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
		private class CacheEntry<T>
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
		{
			public T CachedItem { get; private set; }

			public DateTime ExpiresAt { get; private set; }

			public int RetrievalTimeout { get; private set; }

			public void Invalidate()
			{
				ExpiresAt = DateTime.Now.AddDays(-1);
			}

			#region Constructors

			public CacheEntry(T itemToCache, DateTime expiresAt, int retrievalTimeout)
			{
				CachedItem = itemToCache;
				ExpiresAt = expiresAt;
				RetrievalTimeout = retrievalTimeout;
			}

			#endregion

		}

		#endregion

	}
}
