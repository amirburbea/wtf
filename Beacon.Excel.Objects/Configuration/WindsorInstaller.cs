using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Beacon.Excel.Objects.Configuration
{
    public sealed class WindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<IConfiguration>().Instance((AddInConfiguration)ConfigurationManager.GetSection("beacon.excel")));
        }
    }
}