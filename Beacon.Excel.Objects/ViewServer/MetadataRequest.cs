using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class MetadataRequest : Request
    {
        public MetadataRequest(string queryId, string view)
            : base(Command.Metadata)
        {
            this.QueryId = queryId;
            this.Parameters = new MetadataRequestParameters(view);
        }

        [JsonPropertyName("params")]
        public MetadataRequestParameters Parameters
        {
            get;
        }

        public string QueryId
        {
            get;
        }
    }

    public sealed class MetadataRequestParameters
    {
        internal MetadataRequestParameters(string view)
        {
            this.View = view;
        }

        [JsonPropertyName("VIEW")]
        public string View
        {
            get;
        }
    }
}
