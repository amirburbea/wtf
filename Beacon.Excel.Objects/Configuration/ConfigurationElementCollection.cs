using System;
using System.Collections.Generic;
using System.Configuration;

namespace Beacon.Excel.Objects.Configuration
{
    public abstract class ConfigurationElementCollection<TElement, TInterface, TKey> : ConfigurationElementCollection, IReadOnlyList<TInterface>
        where TElement : ConfigurationElement, TInterface, new()
        where TInterface : class
    {
        private readonly Func<TElement, TKey> _keyProjection;

        protected ConfigurationElementCollection(Func<TElement, TKey> keyProjection) => this._keyProjection = keyProjection;

        TInterface IReadOnlyList<TInterface>.this[int index] => this[index];

        public TElement this[int index] => (TElement)this.BaseGet(index);

        public TElement this[object key] => (TElement)this.BaseGet(key);

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            for (int index = 0; index < this.Count; index++)
            {
                yield return this[index];
            }
        }

        protected override ConfigurationElement CreateNewElement() => new TElement();

        protected override object? GetElementKey(ConfigurationElement element) => this._keyProjection((TElement)element);
    }
}
