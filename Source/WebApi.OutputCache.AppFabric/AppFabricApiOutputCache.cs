using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationServer.Caching;
using WebApi.OutputCache.Core.Cache;

namespace WebApi.OutputCache.AppFabric
{
    public class AppFabricApiOutputCache : IApiOutputCache, IDisposable
    {
        // TODO: make this configurable...
        private const int DefaultMaxRetryCount = 3;

        /* 
         * Example of backoff in action
         * BackOff = TimeSpan.FromSeconds(3);
         * MinBackOff = TimeSpan.FromSeconds(3);
         * MaxBackOff = TimeSpan.FromSeconds(15);
         * 
         * 3000
         * 6072
         * 10320
         * 15000
         * 15000
         * then always 15000
         */
        private static readonly TimeSpan BackOff = TimeSpan.FromSeconds(1); // TODO: make this configurable...
        private static readonly TimeSpan MinBackOff = TimeSpan.FromSeconds(0); // TODO: make this configurable...
        private static readonly TimeSpan MaxBackOff = TimeSpan.FromSeconds(15); // TODO: make this configurable...

        private static readonly int[] TransientErrorCodes =
        {
            DataCacheErrorCode.ConnectionTerminated,
            DataCacheErrorCode.RetryLater,
            DataCacheErrorCode.Timeout,
            DataCacheErrorCode.ServiceAccessError
        };

        private readonly DataCacheFactory _dataCacheFactory;
        private DataCache _dataCache;
        private bool _disposed;

        public AppFabricApiOutputCache()
        {
            _dataCacheFactory = new DataCacheFactory();
            Connect();
        }

        public AppFabricApiOutputCache(DataCacheFactoryConfiguration configuration)
        {
            _dataCacheFactory = new DataCacheFactory(configuration);
            Connect();
        }

        public IEnumerable<string> AllKeys
        {
            get
            {
                // not supported
                return Enumerable.Empty<string>();
            }
        }

        public void RemoveStartsWith(string key)
        {
            // TODO: this should be implemented differently (see: https://github.com/filipw/AspNetWebApi-OutputCache#server-side-caching)
            RunRetry(() => { _dataCache.Remove(key); });
        }

        public T Get<T>(string key) where T : class
        {
            return RunRetry(() => (T)_dataCache.Get(key));
        }

        public object Get(string key)
        {
            return RunRetry(() => _dataCache.Get(key));
        }

        public void Remove(string key)
        {
            RunRetry(() => { _dataCache.Remove(key); });
        }

        public bool Contains(string key)
        {
            var value = RunRetry(() => _dataCache.Get(key));
            return value != null;
        }

        public void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            RunRetry(() => { _dataCache.Add(key, o, expiration.Subtract(DateTimeOffset.UtcNow)); });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Connect()
        {
            RunRetry(() => { _dataCache = _dataCacheFactory.GetDefaultCache(); });

            if (_dataCache == null)
                throw new Exception("AppFabric cache provider could not be initialized. Please check that the configuration is set properly and that the server is up and running.");
        }

        private static T RunRetry<T>(Func<T> retryFunc, int maxRetries = DefaultMaxRetryCount)
        {
            int currentRetry = 0;
            Random random = null;

            while (currentRetry < maxRetries)
            {
                try
                {
                    return retryFunc();
                }
                catch (DataCacheException ex)
                {
                    if (!IsTransientError(ex))
                    {
                        throw;
                    }
                }

                Trace.TraceWarning("Retrying action...");

                currentRetry++;

                if (random == null)
                    random = new Random();

                PerformBackoff(currentRetry, random);
            }

            return default(T);
        }

        private static void RunRetry(Action retryAction, int maxRetries = DefaultMaxRetryCount)
        {
            RunRetry(() =>
            {
                retryAction();
                return true;
            }, maxRetries);
        }

        private static void PerformBackoff(int retryCount, Random random)
        {
            int increment = (int)((Math.Pow(2, retryCount - 1) - 1) *
                                  random.Next((int)(BackOff.TotalMilliseconds * 0.8), (int)(BackOff.TotalMilliseconds * 1.2)));
            int sleepMsec = (int)Math.Min(MinBackOff.TotalMilliseconds + increment, MaxBackOff.TotalMilliseconds);
            if (sleepMsec < 0) sleepMsec = (int)MaxBackOff.TotalMilliseconds;

            Task.Delay(sleepMsec).Wait();
        }

        private static bool IsTransientError(DataCacheException ex)
        {
            return TransientErrorCodes.Contains(ex.ErrorCode);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dataCacheFactory?.Dispose();
                }
                _disposed = true;
            }
        }

        ~AppFabricApiOutputCache()
        {
            Dispose(false);
        }
    }
}