using System;
using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

internal class SimpleCache<TKey, TValue> where TKey : notnull
{
    const int CyclePeriod = 500;

    readonly string _name;
    readonly Func<TKey, TValue> _valueBuilder;

    Dictionary<TKey, TValue> _cache = new();
    Dictionary<TKey, TValue> _lastCache = new();
    int _cycleCounter;

#if DEBUG
    int _hits;
    int _requests;

    public override string ToString()
        => $"Cache for {_name}. Hit rate {100.0f * _hits / _requests:F1}%, size {_cache.Count} + {_lastCache.Count}";
#else
    public override string ToString() => $"Cache for {_name}";
#endif

    public SimpleCache(string name, Func<TKey, TValue> valueBuilder)
    {
        _name = name;
        _valueBuilder = valueBuilder ?? throw new ArgumentNullException(nameof(valueBuilder));
    }

    public TValue Get(TKey value)
    {
#if DEBUG
        _requests++;
#endif
        if (_cycleCounter++ >= CyclePeriod)
        {
            _cycleCounter = 0;
            if (_cache.Count > 2 * _lastCache.Count || _lastCache.Count > 2 * _cache.Count)
            {
                // Console.WriteLine($"Cache {_name} cycling ({_lastCache.Count} -> {_cache.Count})");
                _lastCache = _cache;
                _cache = new();
            }
        }

        if (_cache.TryGetValue(value, out var label))
        {
#if DEBUG
            _hits++;
#endif
            return label;
        }

        if (_lastCache.TryGetValue(value, out label))
        {
#if DEBUG
            _hits++;
#endif
            _cache[value] = label;
            return label;
        }

        label = _valueBuilder(value);
        _cache[value] = label;
        return label;
    }
}