using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Beacon.Excel.Objects
{
    public sealed class AddInSettingsProvider : SettingsProvider
    {
        private readonly Dictionary<string, object?> _localValues = AddInSettingsProvider.GetValues(roaming: false);
        private readonly Dictionary<string, object?> _roamingValues = AddInSettingsProvider.GetValues(roaming: true);

        public override string ApplicationName
        {
            get => Constants.ApplicationName;
            set { /*Ignore*/ }
        }

        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection)
        {
            SettingsPropertyValueCollection output = new SettingsPropertyValueCollection();
            foreach (SettingsProperty property in collection)
            {
                Dictionary<string, object?> values = this.GetValues(property);
                output.Add(new SettingsPropertyValue(property)
                {
                    IsDirty = false,
                    SerializedValue = values.TryGetValue(property.Name, out object? value) ? value : property.DefaultValue
                });
            }
            return output;
        }

        public override void Initialize(string name, NameValueCollection config) => base.Initialize(this.ApplicationName, config);

        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection)
        {
            foreach (SettingsPropertyValue value in collection)
            {
                Dictionary<string, object?> values = this.GetValues(value.Property);
                values[value.Property.Name] = value.SerializedValue;
            }
            File.WriteAllText(AddInSettingsProvider.GetFileName(roaming: false), JsonSerializer.Serialize(this._localValues));
            File.WriteAllText(AddInSettingsProvider.GetFileName(roaming: true), JsonSerializer.Serialize(this._roamingValues));
        }

        private static string GetFileName(bool roaming)
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(roaming ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.LocalApplicationData),
                Constants.ApplicationName
            );
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, "settings.json");
        }

        private static Dictionary<string, object?> GetValues(bool roaming)
        {
            string fileName = AddInSettingsProvider.GetFileName(roaming);
            return File.Exists(fileName)
                ? JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(fileName))
                : new Dictionary<string, object?>();
        }

        private Dictionary<string, object?> GetValues(SettingsProperty property)
        {
            bool roaming = property.Attributes.OfType<SettingsManageabilityAttribute>().Any();
            return roaming ? this._roamingValues : this._localValues;
        }

        private static class Constants
        {
            public const string ApplicationName = "Beacon.Excel.Data";
        }
    }
}
