using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Beacon.Excel.Objects.Configuration;
using Beacon.Excel.Objects.Factories;
using Castle.Core;

namespace Beacon.Excel.Objects.User
{
    public interface IUserManager
    {
        event EventHandler? UserChanged;

        bool IsOneFactorLoginSupported { get; }

        IUser? User { get; }

        void Logout();

        Task OneFactorLogin(string userName, string password);

        Task TwoFactorLogin(string userName, string password, string authCode);
    }

    internal sealed class UserManager : IUserManager, IInitializable
    {
        private readonly IAuthenticationElement _configuration;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IAddInSettings _settings;
        private IUser? _user;

        public UserManager(IConfiguration configuration, IAddInSettings settings, JsonSerializerOptions serializerOptions)
        {
            this._configuration = configuration.Authentication;
            this._serializerOptions = serializerOptions;
            this._settings = settings;
        }

        public event EventHandler? UserChanged;

        public bool IsOneFactorLoginSupported
        {
            get;
            private set;
        }

        public IUser? User
        {
            get => this._user;
            private set
            {
                if (this._user == value)
                {
                    return;
                }
                this._user = value;
                this.UserChanged?.Invoke(this, EventArgs.Empty);
                if (value == null)
                {
                    return;
                }
                this._settings.UserName = value.UserName;
            }
        }

        async void IInitializable.Initialize()
        {
            async Task<bool> PingOneFactor()
            {
                try
                {
                    HttpWebRequest request = WebRequest.CreateHttp($"{this._configuration.OneFactor}ping");
                    request.Method = "HEAD";
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch
                {
                    return false;
                }
            }

            Task<bool> pingTask = PingOneFactor();
            Task task = await Task.WhenAny(pingTask, Task.Delay(5000)).ConfigureAwait(false);
            this.IsOneFactorLoginSupported = task == pingTask && pingTask.Result;
        }

        public void Logout() => this.User = null;

        public Task OneFactorLogin(string userName, string password)
        {
            return this.IsOneFactorLoginSupported
                ? this.Login($"{this._configuration.OneFactor}user", userName, password)
                : Task.FromException<IUser>(new NotSupportedException());
        }

        public Task TwoFactorLogin(string userName, string password, string authCode)
        {
            return this.Login(this._configuration.TwoFactor, userName, password, authCode);
        }

        private async Task Login(string url, string username, string password, string? usertoken = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            using (Stream stream = request.GetRequestStream())
            {
                await JsonSerializer.SerializeAsync(stream, new { username, password, usertoken }, this._serializerOptions).ConfigureAwait(false);
            }
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"HttpStatusCode:{(int)response.StatusCode}");
            }
            using Stream responseStream = response.GetResponseStream();
            AuthResponse authResponse = await JsonSerializer.DeserializeAsync<AuthResponse>(responseStream, this._serializerOptions).ConfigureAwait(false);
            if (authResponse.ErrorCode != 0 && !string.IsNullOrEmpty(authResponse.ErrorMessage))
            {
                throw new ApplicationException(authResponse.ErrorMessage);
            }
            this.User = new BeaconUser(authResponse);
        }

        private sealed class AuthResponse : IUser
        {
            [JsonPropertyName("ok")]
            public int ErrorCode
            {
                get;
                set;
            }

            [JsonPropertyName("msg")]
            public string? ErrorMessage
            {
                get;
                set;
            }

            string IUser.FirstName => this.FirstName!;

            public string? FirstName
            {
                get;
                set;
            }

            string IUser.LastName => this.LastName!;

            public string? LastName
            {
                get;
                set;
            }

            string IUser.Token => this.Token!;

            public string? Token
            {
                get;
                set;
            }

            string IUser.UserName => this.UserName!;

            public string? UserName
            {
                get;
                set;
            }
        }

        private sealed class BeaconUser : IUser
        {
            public BeaconUser(IUser other)
            {
                this.FirstName = other.FirstName;
                this.LastName = other.LastName;
                this.Token = other.Token;
                this.UserName = other.UserName;
            }

            public string FirstName
            {
                get;
            }

            public string LastName
            {
                get;
            }

            public string Token
            {
                get;
            }

            public string UserName
            {
                get;
            }
        }
    }
}
