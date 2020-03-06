using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    [JsonConverter(typeof(ColumnDataTypeConverter))]
    public enum ColumnDataType
    {
        String = 0,
        Number = 1,
        Date = 2,
        Boolean = 3
    }

    internal sealed class ColumnDataTypeConverter : JsonConverter<ColumnDataType>
    {
        public override ColumnDataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Enum.TryParse<ColumnDataType>(reader.GetString(), true, out ColumnDataType dataType) ? dataType : default;
        }

        public override void Write(Utf8JsonWriter writer, ColumnDataType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(Enum.GetName(typeof(ColumnDataType), value).ToLowerInvariant());
        }
    }
}
