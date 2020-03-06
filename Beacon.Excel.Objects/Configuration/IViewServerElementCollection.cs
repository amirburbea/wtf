using System.Collections.Generic;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IViewServerElementCollection : IReadOnlyList<IViewServerElement>
    {
        IViewServerElement this[string key]
        {
            get;
        }
    }

    internal sealed class ViewServerElementCollection : ConfigurationElementCollection<ViewServerElement, IViewServerElement, string>, IViewServerElementCollection
    {
        public ViewServerElementCollection()
            : base(element => element.Key)
        {
        }

        IViewServerElement IViewServerElementCollection.this[string key] => this[key];
    }
}
