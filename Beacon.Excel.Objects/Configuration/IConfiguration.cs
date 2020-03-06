using System.Configuration;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IConfiguration
    {
        IAuthenticationElement Authentication { get; }

        IEnvironmentElementCollection Environments { get; }
    }

    internal sealed class AddInConfiguration : ConfigurationSection, IConfiguration
    {
        [ConfigurationProperty("authentication")]
        public AuthenticationElement Authentication => (AuthenticationElement)this["authentication"];

        IAuthenticationElement IConfiguration.Authentication => this.Authentication;

        [ConfigurationProperty("environments"), ConfigurationCollection(typeof(EnvironmentElement))]
        public EnvironmentElementCollection Environments => (EnvironmentElementCollection)this["environments"];

        IEnvironmentElementCollection IConfiguration.Environments => this.Environments;
    }
}
