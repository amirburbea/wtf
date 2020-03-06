using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Beacon.Excel.Objects.Factories
{
    public sealed class WindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .AddFacility<TypedFactoryFacility>()
                .Register(Component.For(typeof(IFactory<>)).AsFactory());
        }
    }
}