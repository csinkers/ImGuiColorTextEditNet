namespace ImGuiColorTextEditNet;

class SimpleCache<TKey, TValue> where TKey : notnull
{
    readonly string _name;
    readonly Func<TKey, TValue> _valueBuilder;
    const int CyclePeriod = 500;
    Dictionary<TKey, TValue> _cache = new();
    Dictionary<TKey, TValue> _lastCache = new();
    int _cycleCounter;

    public SimpleCache(string name, Func<TKey, TValue> valueBuilder)
    {
        _name = name;
        _valueBuilder = valueBuilder ?? throw new ArgumentNullException(nameof(valueBuilder));
    }

    public TValue Get(TKey value)
    {
        if (_cycleCounter++ >= CyclePeriod)
        {
            _cycleCounter = 0;
            if (_cache.Count > 2 * _lastCache.Count || _lastCache.Count > 2 * _cache.Count)
            {
                // Console.WriteLine($"Cache {_name} cycling ({_lastCache.Count} -> {_cache.Count})");
                _lastCache = _cache;
                _cache = new Dictionary<TKey, TValue>();
            }
        }

        if (_cache.TryGetValue(value, out var label))
            return label;

        if (_lastCache.TryGetValue(value, out label))
        {
            _cache[value] = label;
            return label;
        }

        label = _valueBuilder(value);
        _cache[value] = label;
        return label;
    }
}