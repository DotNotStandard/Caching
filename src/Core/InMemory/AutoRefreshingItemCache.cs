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
    public class AutoRefreshingItemCache<T> : IDisposable
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
        private Task _initialisationTask;
        private Task _refreshingTask;
        private readonly object _clonerLock = new object();

        #region Constructors

        /// <summary>
        /// Create a new instance for use in caching some data
        /// </summary>
        /// <param name="logger">A logger to use for reporting issues with loading</param>
        /// <param name="clonerFactory">The factory for the cloner to in cloning the cached item prior to return</param>
        /// <param name="asyncLoadDelegate">The delegate that is used to load data</param>
        /// <param name="initialValue">The initial value to place into the cache, before loading is complete</param>
        /// <param name="refreshPeriod">The period between cache refreshes - the maxmimum data staleness</param>
        /// <param name="loadTimeout">The timeout for the load operation - defaults to Timeout.InfiniteTimeSpan</param>
        /// <exception cref="ArgumentException">One of the parameters was invalid</exception>
        public AutoRefreshingItemCache(ILogger logger,
            Func<CancellationToken, Task<T>> asyncLoadDelegate, T initialValue,
            TimeSpan refreshPeriod, TimeSpan? loadTimeout = null)
        {
            if (loadTimeout.HasValue && loadTimeout.Value.TotalMilliseconds < 0) throw new ArgumentException(nameof(loadTimeout));
            if (refreshPeriod.TotalMilliseconds < 10) throw new ArgumentException(nameof(refreshPeriod));

            _logger = logger;
            _clonerFactory = new CloneableClonerFactory<T>();
            _asyncLoadDelegate = asyncLoadDelegate;
            if (loadTimeout is null) loadTimeout = Timeout.InfiniteTimeSpan;
            _loadTimeout = loadTimeout.Value;
            _refreshPeriod = refreshPeriod;
            _cacheItem = new CacheItem<T>(initialValue);
        }

        /// <summary>
        /// Create a new instance for use in caching some data
        /// </summary>
        /// <param name="logger">A logger to use for reporting issues with loading</param>
        /// <param name="clonerFactory">The factory for the cloner to in cloning the cached item prior to return</param>
        /// <param name="asyncLoadDelegate">The delegate that is used to load data</param>
        /// <param name="initialValue">The initial value to place into the cache, before loading is complete</param>
        /// <param name="refreshPeriod">The period between cache refreshes - the maxmimum data staleness</param>
        /// <param name="loadTimeout">The timeout for the load operation - defaults to Timeout.InfiniteTimeSpan</param>
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
            if (loadTimeout is null) loadTimeout = Timeout.InfiniteTimeSpan;
            _loadTimeout = loadTimeout.Value;
            _refreshPeriod = refreshPeriod;
            _cacheItem = new CacheItem<T>(initialValue);
        }

        #endregion

        #region Exposed Properties and Methods

        #region Initialisation

        /// <summary>
        /// Synchronously perform initialisation, returning when the cache is fully
        /// initialised.
        /// </summary>
        /// <remarks>
        /// If data fails to load then this method will wait indefinitely
        /// </remarks>
        public void Initialise()
        {
            if (_initialisationTask is null)
            {
                StartInitialisation();
            }
            CompleteInitialisation();
        }

        /// <summary>
        /// Asynchronously await initialisation. The method does not return until the cache is 
        /// fully initialised. If data fails to load then this method will wait indefinitely
        /// </summary>
        /// <returns></returns>
        public async Task InitialiseAsync()
        {
            if (_initialisationTask is null)
            {
                StartInitialisation();
            }
            await _initialisationTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Start initialisation on a background thread without waiting for completion
        /// </summary>
        /// <remarks>
        /// This method initiates a background operation and then immediately returns.
        /// Use this method where you want to start initialisation but not wait for
        /// it to complete immediately. Use in combination with either the CompleteInitialisation
        /// or TryCompleteInitialisation methods
        /// </remarks>
        public void StartInitialisation()
        {
            _initialisationTask = DoInitialisationAsync();
        }

        /// <summary>
        /// Await completion of the initial cache load with no timeout
        /// </summary>
        /// <remarks>
        /// The method is for use in combination with the StartInitialisation method. 
        /// The method does not return until the cache is fully initialised. If data fails to 
        /// load then this method will wait indefinitely
        /// </remarks>
        public void CompleteInitialisation()
        {
            bool isInitialised = false;

            while (!isInitialised)
            {
                isInitialised = TryCompleteInitialisation(Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Await completion of the initial cache load up to a defined maximum timeout
        /// </summary>
        /// <remarks>
        /// The method is for use in combination with the StartInitialisation method. 
        /// The method does not return until the cache is fully initialised, or the timeout is reached.
        /// The boolean return value indicates if initialisation was successfully completed.
        /// </remarks>
        /// <param name="timeout">The maximum allowed timeout before waiting is terminated</param>
        /// <returns>Boolean indicating whether initialisation completed prior to the timeout elapsing</returns>
        public bool TryCompleteInitialisation(TimeSpan timeout)
        {
            if (_isInitialised) return true;
            return _initialisationTask.Wait(timeout);
        }

        #endregion

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
        /// Perform initialisation of the cache
        /// </summary>
        /// <returns></returns>
        private async Task DoInitialisationAsync()
        {
            bool successfullyInitialised = false;

            while (!successfullyInitialised)
            {
                // Perform the initial data load into the cache
                successfullyInitialised = await LoadCacheAsync().ConfigureAwait(false);
                if (!successfullyInitialised) await Task.Delay(2000).ConfigureAwait(false);
            }
            _isInitialised = true;

            // Start the scheduled refreshing cycle
            _refreshingTask = RefreshCacheAsync();
        }

        /// <summary>
        /// Background method forming the task that loads data to the required schedule
        /// </summary>
        private async Task RefreshCacheAsync()
        {
            while (!_refreshingCancellationTokenSource.IsCancellationRequested)
            {
                // Await the refresh period before the next cache load attempt
                try
                {
                    await Task.Delay(_refreshPeriod, _refreshDelayCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                }
                _refreshDelayCancellationTokenSource = new CancellationTokenSource();

                await LoadCacheAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Load data from the data source into the cache
        /// </summary>
        /// <returns>Boolean True if the load succeeds, otherwise false</returns>
        private async Task<bool> LoadCacheAsync()
        {
            bool successfullyLoaded = false;
            CancellationTokenSource cts;
            CancellationToken cancellationToken;

            try
            {
                // Load the data using the provided delegate, timing out if needs be
                cts = new CancellationTokenSource(_loadTimeout);
                cancellationToken = cts.Token;
                T itemToCache = await _asyncLoadDelegate(cancellationToken).ConfigureAwait(false);

                // Assign the loaded data into the cache (an atomic assignment operation)
                _cacheItem = new CacheItem<T>(itemToCache);
                successfullyLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure to load cache from source!");
            }

            return successfullyLoaded;
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
            lock (_clonerLock)
            {
                clone = cloner.DeepClone(item);
            }

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
