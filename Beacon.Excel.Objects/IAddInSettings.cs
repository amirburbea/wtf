using Beacon.Excel.Objects.Environments;

namespace Beacon.Excel.Objects
{
    public interface IAddInSettings
    {
        DataEnvironment Environment { get; set; }

        string? UserName { get; set; }
    }
}
