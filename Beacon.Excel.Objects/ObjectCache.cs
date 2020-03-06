using System.Collections.Concurrent;

namespace Beacon.Excel.Objects
{
    public interface IObjectCache
    {
        int Count { get; }

        object? Extract(string key);

        bool Insert(string key, object? value);
    }

    internal sealed class ObjectCache : IObjectCache
    {
        private readonly ConcurrentDictionary<string, object?> _dictionary = new ConcurrentDictionary<string, object?>();

        public int Count => this._dictionary.Count;

        public object? Extract(string key)
        {
            return this._dictionary.TryRemove(key, out object? value) ? value : default;
        }

        public bool Insert(string key, object? value)
        {
            return this._dictionary.TryAdd(key, value);
        }
    }
}
