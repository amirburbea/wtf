using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public enum Command
    {
        [Command("LOGIN")]
        Login = 0,

        [Command("METADATA2")]
        Metadata = 1,

        [Command("ON-DEMAND-SELECT")]
        Select = 2
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(string text) => this.Text = text;

        public string Text
        {
            get;
        }

        public sealed class CommandConverter : JsonConverter<Command>
        {
            private static readonly Dictionary<int, string> _dictionary = CommandConverter.GetDictionary();

            public override Command Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string text = reader.GetString();
                foreach (KeyValuePair<int, string> keyValuePair in CommandConverter._dictionary)
                {
                    if (text == keyValuePair.Value)
                    {
                        return (Command)keyValuePair.Key;
                    }
                }
                return default;
            }

            public override void Write(Utf8JsonWriter writer, Command value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(CommandConverter._dictionary[(int)value]);
            }

            private static Dictionary<int, string> GetDictionary()
            {
                Dictionary<int, string> dictionary = new Dictionary<int, string>();
                foreach (FieldInfo field in typeof(Command).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    CommandAttribute? attribute = field.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null)
                    {
                        dictionary.Add((int)(Command)field.GetValue(null), attribute.Text);
                    }
                }
                return dictionary;
            }
        }
    }
}
