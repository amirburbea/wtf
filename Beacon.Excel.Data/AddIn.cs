#nullable enable
using Beacon.Excel.Data.Properties;
using Castle.Windsor.Installer;
using ExcelDna.Integration;
using ExcelDna.IntelliSense;

namespace Beacon.Excel.Data
{
    public sealed class AddIn : IExcelAddIn
    {
        void IExcelAddIn.AutoClose()
        {
            IntelliSenseServer.Uninstall();
            Container.Instance.Dispose();
            Settings.Default.Save();
        }

        void IExcelAddIn.AutoOpen()
        {
            Container.Instance.Install(FromAssembly.InThisApplication());
            DataFunctionsRegistration.RegisterDataFunctions();
            IntelliSenseServer.Install();
        }
    }
}
