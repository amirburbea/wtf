using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.Json
{
    /// <summary>
    /// Deserializes arbitrary JSON objects into lists, dictionaries and basic values.
    /// </summary>
    [JsonConverter(typeof(DataContainerConverter))]
    public sealed class DataContainer
    {
        public object? Data { get; set; }

        private static IReadOnlyList<object?> ReadArray(ref Utf8JsonReader reader)
        {
            List<object?> list = new List<object?>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Comment:
                        continue;
                    case JsonTokenType.EndArray:
                        return list;
                    default:
                        list.Add(DataContainer.ReadValue(ref reader));
                        break;
                }
            }
            throw new JsonException("Unexpected end.");
        }

        private static IReadOnlyDictionary<string, object?> ReadObject(ref Utf8JsonReader reader)
        {
            Dictionary<string, object?> dictionary = new Dictionary<string, object?>();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Comment:
                        continue;
                    case JsonTokenType.EndObject:
                        return dictionary;
                    case JsonTokenType.PropertyName:
                        string key = reader.GetString();
                        reader.Read();
                        dictionary.Add(key, DataContainer.ReadValue(ref reader));
                        break;
                }
            }
            throw new JsonException("Unexpected end.");
        }

        private static object? ReadValue(ref Utf8JsonReader reader)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Null => null,
                JsonTokenType.Comment => reader.Read() ? DataContainer.ReadValue(ref reader) : throw new JsonException("Unexpected token."),
                JsonTokenType.Number => reader.TryGetInt32(out int integer) ? integer : (object)reader.GetDouble(),
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartArray => DataContainer.ReadArray(ref reader),
                JsonTokenType.StartObject => DataContainer.ReadObject(ref reader),
                _ => throw new JsonException($"Unexpected token: {reader.TokenType}."),
            };
        }

        private static void WriteArray(Utf8JsonWriter writer, IReadOnlyList<object?> list)
        {
            writer.WriteStartArray();
            foreach (object? value in list)
            {
                DataContainer.WriteValue(writer, value);
            }
            writer.WriteEndArray();
        }

        private static void WriteObject(Utf8JsonWriter writer, IReadOnlyDictionary<string, object?> dictionary)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, object?> keyValuePair in dictionary)
            {
                writer.WritePropertyName(keyValuePair.Key);
                DataContainer.WriteValue(writer, keyValuePair.Value);
            }
            writer.WriteEndObject();
        }

        private static void WriteValue(Utf8JsonWriter writer, object? data)
        {
            if (data == null)
            {
                writer.WriteNullValue();
                return;
            }
            if (data is IReadOnlyDictionary<string, object?> dictionary)
            {
                DataContainer.WriteObject(writer, dictionary);
                return;
            }
            if (data is IReadOnlyList<object?> list)
            {
                DataContainer.WriteArray(writer, list);
                return;
            }
            switch (Type.GetTypeCode(data.GetType()))
            {
                case TypeCode.Boolean:
                    writer.WriteBooleanValue((bool)data);
                    return;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    writer.WriteNumberValue(Convert.ToInt32(data));
                    return;
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    writer.WriteNumberValue(Convert.ToDouble(data));
                    return;
            }
            writer.WriteStringValue(data.ToString());
        }

        private sealed class DataContainerConverter : JsonConverter<DataContainer>
        {
            public override DataContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new DataContainer { Data = DataContainer.ReadValue(ref reader) };
            }

            public override void Write(Utf8JsonWriter writer, DataContainer value, JsonSerializerOptions options)
            {
                DataContainer.WriteValue(writer, value.Data);
            }
        }
    }
}