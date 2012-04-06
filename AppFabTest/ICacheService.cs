namespace AppFabTest
{
    using System;
    using System.Linq.Expressions;

    public interface ICacheService
    {
        T Get<T>(Expression<Func<T>> expr);
        T Get<T>(string key);
        T Get<T>(string key, Expression<Func<T>> expr);
        T Get<T>(int expirationMinutes, Expression<Func<T>> expr);
        T Get<T>(string key, int expirationMinutes, Expression<Func<T>> expr);
        void Add(object item, string key);
        void Add(object item, string key, int expirationMinutes);
        void Delete(String key);
        void Delete<T>(Expression<Func<T>> expr);
        void Clear();
        bool UsingRemote { get; }
    }
}