using System.Collections.Generic;
using System.Text.Json.Serialization;
using Beacon.Excel.Objects.Json;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class Response
    {
        public DataContainer? Attributes
        {
            get;
            set;
        }

        public DataContainer? Data
        {
            get;
            set;
        }

        public string? Error
        {
            get;
            set;
        }

        [JsonPropertyName("OK")]
        public int ErrorCode
        {
            get;
            set;
        }

        [JsonConverter(typeof(ColumnMetadata.DictionaryConverter))]
        public Dictionary<string, ColumnMetadata>? Metadata
        {
            get;
            set;
        }

        public string? QueryId
        {
            get;
            set;
        }

        public string? SessionId
        {
            get;
            set;
        }
    }
}
