using System.Collections.Concurrent;

namespace CosmosPeddler.SDK.Collections;

public class LazyDictionary<KeyT, ValueT> where KeyT : notnull
{
    private readonly ConcurrentDictionary<KeyT, ValueT> dictionary = new();
    private readonly Func<KeyT, ValueT> getValue;

    public ValueT this[KeyT key] => Get(key);

    public LazyDictionary(Func<KeyT, ValueT> getValue)
    {
        this.getValue = getValue;
    }

    public ValueT Get(KeyT key)
    {
        return dictionary.GetOrAdd(key, key => getValue(key));
    }
}