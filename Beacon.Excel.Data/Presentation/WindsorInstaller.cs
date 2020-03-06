using System.Windows;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace Beacon.Excel.Data.Presentation
{
    public sealed class WindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container
                .Register(Component.For<Application>().ImplementedBy<App>())
                .Register(Component.For<IPresentationService>().ImplementedBy<PresentationService>())
                .Register(Classes.FromThisAssembly().BasedOn<Window>().LifestyleTransient());
        }
    }
}