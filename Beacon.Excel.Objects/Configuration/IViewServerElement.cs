using System.Configuration;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IViewServerElement
    {
        string Key { get; }

        string Uri { get; }
    }

    internal sealed class ViewServerElement : ConfigurationElement, IViewServerElement
    {
        [ConfigurationProperty("key")]
        public string Key => (string)this["key"];

        [ConfigurationProperty("uri")]
        public string Uri => (string)this["uri"];
    }
}