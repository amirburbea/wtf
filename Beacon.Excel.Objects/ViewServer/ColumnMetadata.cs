using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public sealed class ColumnMetadata
    {
        public string? Category
        {
            get;
            set;
        }

        [JsonPropertyName("enum")]
        public string? Enumeration
        {
            get;
            set;
        }

        public string? Format
        {
            get;
            set;
        }

        public string? Header
        {
            get;
            set;
        }

        public string? JavaType
        {
            get;
            set;
        }

        public ColumnDataType Type
        {
            get;
            set;
        }

        internal sealed class DictionaryConverter : JsonConverter<Dictionary<string, ColumnMetadata>>
        {
            public override Dictionary<string, ColumnMetadata> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Dictionary<string, ColumnMetadata> dictionary = new Dictionary<string, ColumnMetadata>();
                while (reader.Read() && reader.TokenType == JsonTokenType.EndObject)
                {
                    string property = reader.GetString();
                    reader.Read();
                    dictionary.Add(property, JsonSerializer.Deserialize<ColumnMetadata>(ref reader, options));
                }
                return dictionary;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, ColumnMetadata> value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
