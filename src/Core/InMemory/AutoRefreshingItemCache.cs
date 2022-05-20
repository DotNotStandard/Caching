/*
 * Copyright © 2022 DotNotStandard. All rights reserved.
 * 
 * See the LICENSE file in the root of the repo for licensing details.
 * 
 */
using DotNotStandard.Caching.Core.InMemory.Cloning;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNotStandard.Caching.Core.InMemory
{
    /// <summary>
    /// In-memory cache that automatically refreshes the cache data to a schedule in the background
    /// </summary>
    /// <typeparam name="T">The type that is to be cached</typeparam>
    internal class AutoRefreshingItemCache<T> : IDisposable
    {
        private bool _isInitialised = false;
        private readonly ILogger _logger;
        private readonly IDeepClonerFactory<T> _clonerFactory;
        private readonly Func<CancellationToken, Task<T>> _asyncLoadDelegate;
        private CancellationTokenSource _refreshDelayCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _refreshingCancellationTokenSource = new CancellationTokenSource();
        private readonly TimeSpan _loadTimeout;
        private readonly TimeSpan _refreshPeriod;
        private CacheItem<T> _cacheItem;
        private Task _loadItemTask;

        #region Constructors

        /// <summary>
        /// Create a new instance for use in caching some data
        /// </summary>
        /// <param name="logger">A logger to use for reporting issues with loading</param>
        /// <param name="clonerFactory">The factory for the cloner to in cloning the cached item prior to return</param>
        /// <param name="asyncLoadDelegate">The delegate that is used to load data</param>
        /// <param name="initialValue">The initial value to place into the cache, before loading is complete</param>
        /// <param name="refreshPeriod">The period between cache refreshes - the maxmimum data staleness</param>
        /// <param name="loadTimeout">The timeout for the load operation - defaults to TimeSpan.MaxValue</param>
        /// <exception cref="ArgumentException">One of the parameters was invalid</exception>
        public AutoRefreshingItemCache(ILogger logger, IDeepClonerFactory<T> clonerFactory, 
            Func<CancellationToken, Task<T>> asyncLoadDelegate, T initialValue, 
            TimeSpan refreshPeriod, TimeSpan? loadTimeout = null)
        {
            if (loadTimeout.HasValue && loadTimeout.Value.TotalMilliseconds < 0) throw new ArgumentException(nameof(loadTimeout));
            if (refreshPeriod.TotalMilliseconds < 10) throw new ArgumentException(nameof(refreshPeriod));

            _logger = logger;
            _clonerFactory = clonerFactory;
            _asyncLoadDelegate = asyncLoadDelegate;
            if (loadTimeout is null) loadTimeout = TimeSpan.MaxValue;
            _loadTimeout = loadTimeout.Value;
            _refreshPeriod = refreshPeriod;
            _cacheItem = new CacheItem<T>(initialValue);
            _loadItemTask = LoadCacheItem();
        }

        #endregion

        #region Exposed Properties and Methods

        /// <summary>
        /// Get the value currently held in the cache
        /// </summary>
        /// <returns></returns>
        public T GetItem()
        {
            return DeepCloneOf(_cacheItem.Value);
        }

        /// <summary>
        /// Invalidate the current cache item, so that it is reloaded as soon as possible
        /// </summary>
        public void Invalidate()
        {
            _refreshDelayCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Await completion of the initial cache load with no timeout
        /// </summary>
        public void CompleteInitialisation()
        {
            TryCompleteInitialisation(TimeSpan.MaxValue);
        }

        /// <summary>
        /// Await completion of the initial cache load up to a defined maximum timeout
        /// </summary>
        /// <param name="timeout">The maximum allowed timeout before waiting is terminated</param>
        /// <returns>Boolean indicating whether initialisation completed prior to the timeout elapsing</returns>
        public bool TryCompleteInitialisation(TimeSpan timeout)
        {
            if (_isInitialised) return true;
            return _loadItemTask.Wait(timeout);
        }

        #region IDisposable Interface

        /// <summary>
        /// Disposal method, used to clean up background processing
        /// </summary>
        public void Dispose()
        {
            _refreshingCancellationTokenSource.Cancel();
            _refreshDelayCancellationTokenSource.Cancel();
        }

        #endregion

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Background method forming the task that loads data to the required schedule
        /// </summary>
        private async Task LoadCacheItem()
        {
            CancellationTokenSource cts;
            CancellationToken cancellationToken;
            
            while (!_refreshingCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Load the data using the provided delegate, timing out if needs be
                    cts = new CancellationTokenSource(_loadTimeout);
                    cancellationToken = cts.Token;
                    T itemToCache = await _asyncLoadDelegate(cancellationToken);

                    // Assign the loaded data into the cache (an atomic assignment operation)
                    _cacheItem = new CacheItem<T>(itemToCache);
                    _isInitialised = true;

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure to load cache from source!");
                }

                // Await the refresh period before starting again
                try
                {
                    await Task.Delay(_refreshPeriod, _refreshDelayCancellationTokenSource.Token);
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                }
                _refreshDelayCancellationTokenSource = new CancellationTokenSource();
            }
        }

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

        #region Private Helper Types

        /// <summary>
        /// Type to contain the cached value; use of this class ensures that 
        /// update of the cache through an assignment is atomic for all types
        /// </summary>
        /// <typeparam name="T"></typeparam>
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
        private class CacheItem<T>
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
        {
            public CacheItem(T value)
            {
                Value = value;
            }

            public T Value { get; set; }
        }

        #endregion
    }
}
