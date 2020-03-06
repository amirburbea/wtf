using System.Collections.Generic;
using Beacon.Excel.Objects.Environments;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IEnvironmentElementCollection : IReadOnlyList<IEnvironmentElement>
    {
        IEnvironmentElement this[DataEnvironment environment]
        {
            get;
        }
    }

    internal sealed class EnvironmentElementCollection : ConfigurationElementCollection<EnvironmentElement, IEnvironmentElement, DataEnvironment>, IEnvironmentElementCollection
    {
        public EnvironmentElementCollection()
            : base(element => element.Environment)
        {
        }

        IEnvironmentElement IEnvironmentElementCollection.this[DataEnvironment environment] => this[environment];
    }
}
