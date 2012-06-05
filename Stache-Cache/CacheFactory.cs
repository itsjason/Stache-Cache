using System;
using System.Collections.Generic;

namespace StacheCache
{
    internal class CacheFactory
    {
        private readonly bool _remoteEnabled;
        private readonly IEnumerable<string> _remoteCacheServers;
        private readonly int _remoteTimeout;
        private readonly string _remoteCacheName;
        private readonly string _remoteCacheRegion;
        private bool _remoteFailed;
        private DateTime _remoteFailedAt;
        private ICache _remoteCache, _localCache;
        private readonly int _retrySeconds;

        internal ICache Cache { get { return CanTryRemote() ? TryGetRemoteCache() : GetLocalCache(); } }

        public CacheFactory()
        {
            _remoteEnabled = false;
        }

        public CacheFactory(IEnumerable<string> remoteCacheServers, int remoteTimeout, string remoteCacheName, string remoteCacheRegion, int retrySeconds = 30)
        {
            _remoteEnabled = Properties.Settings.Default.UseRemoteCache;
            _remoteCacheServers = remoteCacheServers;
            _remoteTimeout = remoteTimeout;
            _remoteCacheName = remoteCacheName;
            _remoteCacheRegion = remoteCacheRegion;
            _retrySeconds = retrySeconds;
        }

        private bool CanTryRemote()
        {
            if (!_remoteEnabled)
                return false;

            if (!_remoteFailed)
                return true;

            return DateTime.Now.Subtract(_remoteFailedAt).Seconds > _retrySeconds;
        }

        private ICache TryGetRemoteCache()
        {
            _remoteFailed = false;
            try
            {
                return _remoteCache ??
                       (_remoteCache =
                        new RemoteCache(_remoteCacheServers, _remoteTimeout, _remoteCacheName, _remoteCacheRegion));
            }
            catch
            {
                MarkRemoteFailed();
                return GetLocalCache();
            }
        }

        private ICache GetLocalCache()
        {
            return _localCache ?? (_localCache = new LocalCache());
        }

        internal void MarkRemoteFailed()
        {
            _remoteFailed = true;
            _remoteCache = null;
            _remoteFailedAt = DateTime.Now;
        }
    }
}
