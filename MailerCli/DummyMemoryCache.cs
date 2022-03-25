using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace MailerCli
{
    public class DummyMemoryCache : IMemoryCache
    {
        Dictionary<string,object> _cache = new Dictionary<string,object>();
        public ICacheEntry CreateEntry(object key)
        {
            var item = new MemCachItem() {Key = key};
            _cache[item.Key.ToString()] = item;
            return item;
        }

        public void Dispose()
        {
        }

        public void Remove(object key)
        {
            _cache.Remove(key.ToString());
        }

        public bool TryGetValue(object key, out object value)
        {
            return _cache.TryGetValue(key.ToString(), out value);
        }
    }

    public class MemCachItem : ICacheEntry
    {
        public object Key { get; set; }

        public object Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }

        public IList<IChangeToken> ExpirationTokens => throw new NotImplementedException();

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; set; }

        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose()
        {
        }
    }
}
