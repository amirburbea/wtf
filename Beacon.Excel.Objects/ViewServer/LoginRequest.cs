using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class LoginRequest : Request
    {
        public LoginRequest(string userToken)
            : base(Command.Login)
        {
            this.Parameters = new LoginRequestParameters(userToken);
        }

        [JsonPropertyName("params")]
        public LoginRequestParameters Parameters
        {
            get;
        }
    }

    public sealed class LoginRequestParameters
    {
        internal LoginRequestParameters(string userToken)
        {
            this.UserToken = userToken;
        }

        [JsonPropertyName("APPLICATION-TOKEN")]
        public string ApplicationToken => "22652d8397b4bb8052b478f60b78694a98badb21bf7375af1803c4aad4818306674bd091a71fc60243c7af9e";

        [JsonPropertyName("USER-TOKEN")]
        public string UserToken
        {
            get;
        }
    }
}
