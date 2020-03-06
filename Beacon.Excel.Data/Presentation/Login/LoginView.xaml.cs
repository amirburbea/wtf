#nullable enable
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Beacon.Excel.Data.Presentation.Login
{
    partial class LoginView
    {
        static LoginView()
        {
            CommandManager.RegisterClassCommandBinding(typeof(LoginView), new CommandBinding(ApplicationCommands.Close, (sender, e) => ((LoginView)sender).Close()));
        }

        public LoginView(LoginViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
            PropertyChangedEventManager.AddHandler(viewModel, this.ViewModel_ErrorChanged, nameof(viewModel.Error));
            this.Loaded += this.LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= this.LoginView_Loaded;
            LoginViewModel viewModel = (LoginViewModel)this.DataContext;
            (string.IsNullOrEmpty(viewModel.UserName) ? this.PART_UserName : (UIElement)this.PART_Password).Focus();
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ((LoginViewModel)this.DataContext).Password = ((PasswordBox)sender).Password;
        }

        private void ViewModel_ErrorChanged(object sender, PropertyChangedEventArgs e)
        {
            LoginViewModel viewModel = (LoginViewModel)sender;
            if (viewModel.Error == null)
            {
                return;
            }
            this.PART_Password.SelectAll();
            this.PART_Password.Focus();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            LoginViewModel viewModel = (LoginViewModel)this.DataContext;
            PropertyChangedEventManager.RemoveHandler(viewModel, this.ViewModel_ErrorChanged, nameof(viewModel.Error));
            base.OnClosing(e);
        }
    }
}