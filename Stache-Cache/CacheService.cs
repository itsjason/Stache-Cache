using System;
using System.Collections.Generic;
using System.Text;
using System.Linq.Expressions;
using System.Security.Cryptography;

namespace StacheCache
{
    public class CacheService : ICacheService
    {
        private readonly CacheFactory _factory;
        private readonly int _defaultCacheTime;
        private readonly SHA1CryptoServiceProvider _cryptoProvider;

        private ICache Cache { get { return _factory.Cache; } }
        public bool UsingRemote { get { return Cache != null && Cache.IsRemote; } }

        public CacheService() : this(Properties.Settings.Default.CacheMinutes)
        {
            var s = Properties.Settings.Default;

            if (!s.UseRemoteCache)
                return;

            var servers = s.CacheServers.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
            _factory = new CacheFactory(servers, s.CacheTimeout, s.CacheName, s.CacheRegionName);
        }

        public CacheService(int cacheMinutes)
        {
            _defaultCacheTime = cacheMinutes;
            _cryptoProvider = new SHA1CryptoServiceProvider();
            _factory = new CacheFactory();
        }

        public CacheService(int cacheMinutes, IEnumerable<string> remoteCacheServers, string remoteCacheName, string remoteCacheRegion, int remoteTimeout, int retrySeconds = 30) : this(cacheMinutes)
        {
            _factory = new CacheFactory(remoteCacheServers, remoteTimeout, remoteCacheName, remoteCacheRegion, retrySeconds);
        }

        public T Get<T>(Expression<Func<T>> expr)
        {
            return Get(_defaultCacheTime, expr);
        }

        public T Get<T>(string key, Expression<Func<T>> expr)
        {
            return Get(key, _defaultCacheTime, expr);
        }

        public T Get<T>(int expirationMinutes, Expression<Func<T>> expr)
        {
            var key = GetKey(expr);
            return Get(key, expirationMinutes, expr);
        }

        public T Get<T>(string key)
        {
            try
            {
                return (T)ReadFromCache(key);
            }
            catch
            {
                _factory.MarkRemoteFailed();
                return default(T);
            }
        }

        public T Get<T>(string key, int expirationMinutes, Expression<Func<T>> expr)
        {
            // Get the delegate to evaluate
            var func = expr.Compile();

            // If cache time is < 1, caching is disabled.
            if (expirationMinutes < 1) return func();

            var item = Get<T>(key);
            if (Equals(item, default(T)))
            {
                item = func();
                Add(item, key, expirationMinutes);
            }
            return item;
        }

        public void Delete<T>(Expression<Func<T>> expr)
        {
            TryWithRemote(() => Delete(GetKey(expr)));
        }

        public void Clear()
        {
            TryWithRemote(() => Cache.Clear());
        }

        public void Add(object item, string key, int expirationMinutes)
        {
            if (expirationMinutes > 0)
                TryWithRemote(() => AddToCache(item, key, expirationMinutes));
        }

        public void Add(object item, string key)
        {
            Add(item, key, _defaultCacheTime);
        }

        public void Delete(String key)
        {
            Cache.Delete(key);
        }

        private void TryWithRemote(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                MarkRemoteFailed();
                action();
            }
        }

        private void MarkRemoteFailed()
        {
            if(_factory != null)
                _factory.MarkRemoteFailed();
        }

        //private static readonly BinaryFormatter Formatter = new BinaryFormatter();
        private void AddToCache(object item, string key, int expirationMinutes)
        {
            TryWithRemote(() => Cache.AddToCache(item, key, expirationMinutes));
        }

        private object ReadFromCache(string key)
        {
            try
            {
                return Cache.ReadFromCache(key);
            } catch
            {
                _factory.MarkRemoteFailed();
                return ReadFromCache(key);
            }            
        }

        private string GetKey<T>(Expression<Func<T>> expr)
        {
            var bodyHash = GetHash(expr.Body.ToString());
            var typeHash = GetHash(typeof(T).FullName);
            return String.Concat(typeHash, "-", bodyHash);
        }

        private string GetHash(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return BitConverter.ToString(_cryptoProvider.ComputeHash(bytes)).Replace("-", "");
        }
    }
}
