#nullable enable
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using Beacon.Excel.Objects;
using Beacon.Excel.Objects.User;

namespace Beacon.Excel.Data.Presentation.Login
{
    public sealed class LoginViewModel : NotifierBase
    {
        private static readonly string _domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;

        private readonly IUserManager _userManager;
        private string _authCode;
        private string? _error;
        private string _password;
        private string _userName;
        private bool _isBusy;

        public LoginViewModel(IUserManager userManager, IAddInSettings settings)
        {
            this._userManager = userManager;
            this._authCode = this._password = string.Empty;
            this._userName = settings.UserName != null && settings.UserName.Length != 0
                ? settings.UserName
                : string.IsNullOrEmpty(LoginViewModel._domainName) 
                    ? string.Empty 
                    : Environment.UserName;
            this.LoginCommand = new DelegateCommand(this.Login);
        }

        public string AuthCode
        {
            get => this._authCode;
            set => this.SetValue(ref this._authCode, value);
        }

        public string? Error
        {
            get => this._error;
            private set => this.SetValue(ref this._error, value);
        }

        public bool IsBusy
        {
            get => this._isBusy;
            private set => this.SetValue(ref this._isBusy, value);
        }

        public bool IsOneFactorLoginSupported => this._userManager.IsOneFactorLoginSupported;

        public DelegateCommand LoginCommand
        {
            get;
        }

        public string Password
        {
            get => this._password;
            set => this.SetValue(ref this._password, value);
        }

        public string UserName
        {
            get => this._userName;
            set => this.SetValue(ref this._userName, value);
        }

        private async void Login()
        {
            try
            {
                this.Error = null;
                this.IsBusy = true;
                await (this._userManager.IsOneFactorLoginSupported
                    ? this._userManager.OneFactorLogin(this._userName, this._password)
                    : this._userManager.TwoFactorLogin(this._userName, this._password, this._authCode));
                Application.Current.Windows.OfType<LoginView>().FirstOrDefault()?.Close();
            }
            catch (AggregateException e)
            {
                this.Error = e.InnerException.Message;
            }
            catch (Exception e)
            {
                this.Error = e.Message;
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }
}