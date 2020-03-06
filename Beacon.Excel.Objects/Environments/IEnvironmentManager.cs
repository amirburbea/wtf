using System;
using Beacon.Excel.Objects.User;

namespace Beacon.Excel.Objects.Environments
{
    public interface IEnvironmentManager
    {
        event EventHandler? EnvironmentChanged;

        DataEnvironment Environment
        {
            get;
            set;
        }
    }

    internal sealed class EnvironmentManager : IEnvironmentManager, IDisposable
    {
        private readonly DataEnvironment _initialEnvironment;
        private readonly IAddInSettings _settings;
        private readonly IUserManager _userManager;

        public EnvironmentManager(IUserManager userManager, IAddInSettings settings)
        {
            (this._userManager = userManager).UserChanged += this.UserManager_UserChanged;
            this._settings = settings;
            this._initialEnvironment = settings.Environment;
        }

        public event EventHandler? EnvironmentChanged;

        public DataEnvironment Environment
        {
            get => this._settings.Environment;
            set
            {
                if (value == this._settings.Environment)
                {
                    return;
                }
                this._settings.Environment = value;
                this.EnvironmentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            this._userManager.UserChanged -= this.UserManager_UserChanged;
        }

        private void UserManager_UserChanged(object sender, EventArgs e)
        {
            this.Environment = this._initialEnvironment;
        }
    }
}
