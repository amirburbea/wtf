using System.Configuration;

namespace Beacon.Excel.Objects.Configuration
{
    public interface IAuthenticationElement
    {
        string OneFactor { get; }

        string TwoFactor { get; }
    }

    internal sealed class AuthenticationElement : ConfigurationElement, IAuthenticationElement
    {
        [ConfigurationProperty("oneFactor")]
        public string OneFactor => (string)this["oneFactor"];

        [ConfigurationProperty("twoFactor")]
        public string TwoFactor => (string)this["twoFactor"];
    }
}