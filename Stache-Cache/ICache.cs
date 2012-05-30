using System;

namespace StacheCache
{
    public interface ICache
    {
        object ReadFromCache(String key);
        void AddToCache(Object item, String key, int expirationMinutes);
        void Delete(String key);
        void Clear();
        bool IsRemote { get; }
    }
}