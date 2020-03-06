using System.Configuration;
using Beacon.Excel.Objects.Environments;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IEnvironmentElement
    {
        DataEnvironment Environment { get; }

        IViewServerElementCollection ViewServers { get; }
    }

    internal sealed class EnvironmentElement : ConfigurationElement, IEnvironmentElement
    {
        [ConfigurationProperty("environment")]
        public DataEnvironment Environment => (DataEnvironment)this["environment"];

        [ConfigurationProperty("viewServers"), ConfigurationCollection(typeof(ViewServerElement))]
        public ViewServerElementCollection ViewServers => (ViewServerElementCollection)this["viewServers"];

        IViewServerElementCollection IEnvironmentElement.ViewServers => this.ViewServers;
    }
}
