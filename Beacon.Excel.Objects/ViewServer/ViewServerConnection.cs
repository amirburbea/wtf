using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beacon.Excel.Objects.Configuration;
using Beacon.Excel.Objects.Environments;
using Beacon.Excel.Objects.User;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class ViewServerConnection : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly IEnvironmentManager _environmentManager;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IUserManager _userManager;
        private bool _closeRequested;
        private bool _isLoggedIn;
        private Socket? _socket;
        private string? _viewServerUri;

        public ViewServerConnection(IConfiguration configuration, IUserManager userManager, IEnvironmentManager environmentManager, JsonSerializerOptions serializerOptions)
        {
            this._configuration = configuration;
            this._userManager = userManager;
            this._environmentManager = environmentManager;
            this._serializerOptions = serializerOptions;
            this._userManager.UserChanged += this.UserManager_UserChanged;
        }

        public event EventHandler<ResponseEventArgs>? DataResponseReceived;

        public event EventHandler<ResponseEventArgs>? ErrorResponseReceived;

        public event EventHandler? IsLoggedInChanged;

        public event EventHandler<ResponseEventArgs>? MetadataResponseReceived;

        public bool IsLoggedIn
        {
            get => this._isLoggedIn;
            set
            {
                if (value == this._isLoggedIn)
                {
                    return;
                }
                this._isLoggedIn = value;
                this.IsLoggedInChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void Dispose()
        {
            await this.Close().ConfigureAwait(false);
            this._userManager.UserChanged -= this.UserManager_UserChanged;
        }

        public Task Initialize(string viewServer)
        {
            this._viewServerUri = this._configuration.Environments[this._environmentManager.Environment].ViewServers[viewServer].Uri;
            return this.Initialize();
        }

        public async Task Send(Request request)
        {
            if (this._socket != null)
            {
                await this._socket.SendAsync(request).ConfigureAwait(false);
            }
        }

        private async Task Close()
        {
            this._closeRequested = true;
            this.IsLoggedIn = false;
            if (this._socket != null)
            {
                await this._socket.Close().ConfigureAwait(false);
            }
        }

        private async Task Initialize()
        {
            try
            {
                if (this._userManager.User == null)
                {
                    return;
                }
                this.IsLoggedIn = false;
                Socket socket = new Socket(this._viewServerUri!, this._serializerOptions);
                await socket.ConnectAsync().ConfigureAwait(false);
                if (this._closeRequested)
                {
                    await socket.Close().ConfigureAwait(false);
                    return;
                }
                socket.Response += this.Socket_Response;
                socket.Disconnected += this.Socket_Disconnected;
                this._socket = socket;
                await socket.SendAsync(new LoginRequest(this._userManager.User.Token)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (this._closeRequested || this._userManager.User == null)
                {
                    return;
                }
                await Task.Delay(10000).ConfigureAwait(false);
                await this.Initialize().ConfigureAwait(false);
            }
        }

        private async void Socket_Disconnected(object sender, EventArgs e)
        {
            Socket socket = (Socket)sender;
            this.IsLoggedIn = false;
            socket.Response -= this.Socket_Response;
            socket.Disconnected -= this.Socket_Disconnected;
            this._socket = null;
            if (this._userManager.User != null && !this._closeRequested)
            {
                await Task.Delay(10000).ConfigureAwait(false);
                await this.Initialize().ConfigureAwait(false);
            }
        }

        private void Socket_Response(object sender, ResponseEventArgs e)
        {
            if (e.Response.SessionId != null && e.Response.Attributes?.Data != null)
            {
                this.IsLoggedIn = true;
            }
            if (e.Response.Metadata != null)
            {
                this.MetadataResponseReceived?.Invoke(this, e);
            }
            if (e.Response.Data?.Data != null)
            {
                this.DataResponseReceived?.Invoke(this, e);
            }
            if (e.Response.ErrorCode != 0)
            {
                this.ErrorResponseReceived?.Invoke(this, e);
            }
        }

        private async void UserManager_UserChanged(object sender, EventArgs e) => await this.Close().ConfigureAwait(false);

        private static class Constants
        {
            public const int KeepAlive = 20;
            public const int ReceiveChunkSize = 1024;
            public const int SendChunkSize = 1024;
        }

        private sealed class Socket
        {
            private readonly JsonSerializerOptions _serializerOptions;
            private readonly Uri _uri;
            private readonly ClientWebSocket _webSocket = new ClientWebSocket { Options = { KeepAliveInterval = TimeSpan.FromSeconds(Constants.KeepAlive) } };

            public Socket(string uri, JsonSerializerOptions serializerOptions)
            {
                this._uri = new Uri(uri);
                this._serializerOptions = serializerOptions;
            }

            public event EventHandler? Disconnected;

            public event EventHandler<ResponseEventArgs>? Response;

            public async Task Close()
            {
                if (this._webSocket.State == WebSocketState.Open)
                {
                    await this._webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
            }

            public async Task ConnectAsync()
            {
                await this._webSocket.ConnectAsync(this._uri, CancellationToken.None);
                this.StartListen();
            }

            public async Task SendAsync(Request request)
            {
                if (this._webSocket.State != WebSocketState.Open)
                {
                    throw new InvalidOperationException("Connection not open.");
                }
                string json = JsonSerializer.Serialize(request, request.GetType(), this._serializerOptions);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                for (int offset = 0; offset < bytes.Length; offset += Constants.SendChunkSize)
                {
                    int count = Math.Min(Constants.SendChunkSize, bytes.Length - offset);
                    await this._webSocket.SendAsync(new ArraySegment<byte>(bytes, offset, count), WebSocketMessageType.Text, (count + offset) >= bytes.Length, CancellationToken.None).ConfigureAwait(false);
                }
            }

            private void InvokeAsync(EventHandler handler)
            {
                ThreadPool.QueueUserWorkItem(delegate { handler(this, EventArgs.Empty); });
            }

            private void InvokeAsync<T>(EventHandler<T> handler, T eventArgs)
                where T : EventArgs
            {
                ThreadPool.QueueUserWorkItem(delegate { handler(this, eventArgs); });
            }

            private async void StartListen()
            {
                using WebSocket socket = this._webSocket;
                byte[] buffer = new byte[Constants.ReceiveChunkSize];
                try
                {
                    while (socket.State == WebSocketState.Open)
                    {
                        StringBuilder builder = new StringBuilder();
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                            if (result.MessageType != WebSocketMessageType.Close)
                            {
                                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                            }
                            else
                            {
                                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                                EventHandler? disconnectedHandler = this.Disconnected;
                                if (disconnectedHandler != null)
                                {
                                    this.InvokeAsync(disconnectedHandler);
                                }
                                break;
                            }
                        }
                        while (!result.EndOfMessage);
                        EventHandler<ResponseEventArgs>? responseHandler;
                        if (builder.Length != 0 && (responseHandler = this.Response) != null)
                        {
                            string json = builder.ToString();
                            Response response = JsonSerializer.Deserialize<Response>(json, this._serializerOptions);
                            this.InvokeAsync(responseHandler, new ResponseEventArgs(response, json));
                        }
                    }
                }
                catch
                {
                    this.Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
