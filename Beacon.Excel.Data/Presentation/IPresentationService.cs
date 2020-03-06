#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Beacon.Excel.Data.Presentation.Login;
using Beacon.Excel.Objects.Factories;
using Castle.Core;

namespace Beacon.Excel.Data.Presentation
{
    public interface IPresentationService
    {
        void ShowLoginDialog();
    }

    public sealed class PresentationService : IPresentationService, IInitializable, IDisposable
    {
        private readonly IFactory<Application> _appFactory;
        private readonly IFactory<LoginView> _loginViewFactory;
        private readonly Thread _uiThread;
        private Application? _app;

        public PresentationService(IFactory<Application> appFactory, IFactory<LoginView> loginViewFactory)
        {
            this._appFactory = appFactory;
            this._loginViewFactory = loginViewFactory;
            this._uiThread = new Thread(this.Run);
            this._uiThread.SetApartmentState(ApartmentState.STA);
        }

        public void Dispose()
        {
            if (this._app == null)
            {
                return;
            }
            this._app.Shutdown();
            this._appFactory.Release(this._app);
            this._app = null;
            this._uiThread.Join();
        }

        public void Initialize() => this._uiThread.Start();

        public void ShowLoginDialog()
        {
            void ShowLoginWindow()
            {
                LoginView? existing = this._app?.Windows.OfType<LoginView>().FirstOrDefault();
                if (existing != null)
                {
                    return;
                }
                LoginView loginView = this._loginViewFactory.Resolve();

                void OnLoginWindowClosed(object? sender, EventArgs e)
                {
                    loginView.Closed -= OnLoginWindowClosed;
                    this._loginViewFactory.Release(loginView);
                }

                loginView.Closed += OnLoginWindowClosed;
                loginView.Show();
                loginView.Dispatcher.InvokeAsync(loginView.Activate);
            }

            this._app?.Dispatcher.InvokeAsync(ShowLoginWindow);
        }

        private void Run()
        {
            (this._app = this._appFactory.Resolve()).Run();
        }
    }
}