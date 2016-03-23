using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Utils
{
    public static class Configuration
    {
        private static readonly string SettingsFileLocation = ConfigurationManager.AppSettings["RootDirectory"]  + "settings.json";

        /// <summary>
        /// Configuration is loaded once at startup and cached
        /// </summary>
        static Configuration()
        {
            if (!File.Exists(SettingsFileLocation))
                throw new Exception($"Settings file does not exist: {Path.GetFullPath(SettingsFileLocation)}");

            var data = File.ReadAllText(SettingsFileLocation);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            var props = typeof(Configuration).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in settings)
            {
                if (!props.ContainsKey(kvp.Key))
                    continue;

                var prop = props[kvp.Key];
                prop.SetValue(null, kvp.Value);
            }
        }

#if DEBUG
        public static string DefaultConfigurationFile => JsonConvert.SerializeObject(typeof(Configuration).GetProperties().Where(p => p.Name != nameof(DefaultConfigurationFile)).ToDictionary(p => p.Name, p => p.GetValue(null)), Formatting.Indented);
#endif

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public static string UserName { get; private set; }
        public static string Password { get; private set; }

    }
}
