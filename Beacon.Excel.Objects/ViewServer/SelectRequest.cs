using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class SelectRequest : Request
    {
        public SelectRequest()
            : base(Command.Select)
        {            
        }

        [JsonPropertyName("params")]
        public SelectRequestParameters? Parameters
        {
            get;
            set;
        }

        public string? QueryId
        {
            get;
            set;
        }
    }

    public sealed class SelectRequestParameters
    {
        [JsonPropertyName("FIELDS")]
        public string? Fields
        {
            get;
            set;
        }

        [JsonPropertyName("GROUPBY")]
        public string? GroupBy
        {
            get;
            set;
        }

        [JsonPropertyName("LIMIT")]
        public int? Limit
        {
            get;
            set;
        }

        [JsonPropertyName("OFFSET")]
        public int? Offset
        {
            get;
            set;
        }

        [JsonPropertyName("ORDERBY")]
        public string? OrderBy
        {
            get;
            set;
        }

        [JsonPropertyName("VIEW")]
        public string? View
        {
            get;
            set;
        }

        [JsonPropertyName("WHERE")]
        public string? Where
        {
            get;
            set;
        }
    }
}
