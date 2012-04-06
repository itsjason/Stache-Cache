namespace AppFabTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationServer.Caching;

    class RemoteCache : ICache
    {
        private static DataCache _cache;
        private static string _cacheRegion;
        private static int _timeout;

        private static TimeSpan Timespan { get { return new TimeSpan(0, 0, 0, 0, _timeout); } }
        public bool IsRemote { get { return true; } }

        public RemoteCache(IEnumerable<string> cacheServers, int timeoutMilliseconds = 2000, string cacheName = "default", string regionName = "default")
        {
            _timeout = timeoutMilliseconds;
            _cacheRegion = regionName;

            var servers = cacheServers.Select(serverName => new DataCacheServerEndpoint(serverName, 22233)).ToList();

            //Create cache configuration
            //Set the cache host(s)
            //Set default properties for local cache (local cache disabled)
            var factoryConfiguration = new DataCacheFactoryConfiguration
            {
                Servers = servers,
                LocalCacheProperties = new DataCacheLocalCacheProperties(),
                ChannelOpenTimeout = Timespan,
                RequestTimeout = Timespan,
                TransportProperties = new DataCacheTransportProperties()
                {
                    ChannelInitializationTimeout = Timespan,
                    ReceiveTimeout = Timespan
                }
            };

            //Disable tracing to avoid informational/verbose messages on the web page
            DataCacheClientLogManager.ChangeLogLevel(System.Diagnostics.TraceLevel.Off);

            //_cache = ConnectCacheRx(cacheName, factoryConfiguration).Timeout(Timespan).Single();

            ThreadPool.QueueUserWorkItem(_ => ConnectCache(cacheName, factoryConfiguration));

            if(!_cacheConnected.WaitOne(_timeout))
                throw new TimeoutException("Could Not Connect to AppFabric Server at: " + String.Join(", ", cacheServers));

        }

        public object ReadFromCache(string key)
        {
            return _cache.Get(key, _cacheRegion);
        }

        public void AddToCache(object item, string key, int expirationMinutes)
        {
            var span = new TimeSpan(0, expirationMinutes, 0);
            _cache.Put(key, item, span, _cacheRegion);
        }

        public void Delete(string key)
        {
            _cache.Remove(key, _cacheRegion);
        }

        public void Clear()
        {
            _cache.ClearRegion(_cacheRegion);
        }

        private static readonly ManualResetEvent _cacheConnected = new ManualResetEvent(false);

        private static void ConnectCache(string cacheName, DataCacheFactoryConfiguration config)
        {
            try
            {
                var factory = new DataCacheFactory(config);
                _cache = factory.GetCache(cacheName);
                _cache.CreateRegion(_cacheRegion);
                _cacheConnected.Set();
            }
            catch
            {
                ;
            }
        }
    }

}
