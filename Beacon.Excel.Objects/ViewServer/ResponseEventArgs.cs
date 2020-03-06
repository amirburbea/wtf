using System;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class ResponseEventArgs : EventArgs
    {
        public ResponseEventArgs(Response response, string json) => (this.Response, this.Json) = (response, json);

        public string Json { get; }

        public Response Response { get; }
    }
}
