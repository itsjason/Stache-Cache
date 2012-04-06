using System;
using System.Collections.Generic;

namespace AppFabTest
{
    using System.Web;
    using System.Web.Caching;

    class LocalCache : ICache
    {
        private static Cache Cache { get { return HttpRuntime.Cache; } }
        public bool IsRemote { get { return false; } }

        public object ReadFromCache(string key)
        {
            return Cache.Get(key);
        }

        public void AddToCache(object item, string key, int expirationMinutes)
        {
            Cache.Insert(key, item, null, DateTime.Now.AddMinutes(expirationMinutes), Cache.NoSlidingExpiration);
        }

        public void Delete(string key)
        {
            Cache.Remove(key);
        }

        public void Clear()
        {
            var en = Cache.GetEnumerator();
            var keys = new List<string>();
            while (en.MoveNext())
            {
                keys.Add(en.Key.ToString());
            }

            foreach (var key in keys)
            {
                Delete(key);
            }
        }
    }
}
