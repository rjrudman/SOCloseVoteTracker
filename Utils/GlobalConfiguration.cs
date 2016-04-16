using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Utils
{
    public static class GlobalConfiguration
    {
        private static readonly string SettingsFileLocation = ConfigurationManager.AppSettings["SettingsFileLocation"];

        /// <summary>
        /// GlobalConfiguration is loaded once at startup and cached
        /// </summary>
        static GlobalConfiguration()
        {
            if (File.Exists(SettingsFileLocation))
            {
                var data = File.ReadAllText(SettingsFileLocation);
                var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

                var props = typeof (GlobalConfiguration).GetProperties()
                    .ToDictionary(p => p.Name, p => p, StringComparer.InvariantCultureIgnoreCase);
                foreach (var kvp in settings)
                {
                    if (!props.ContainsKey(kvp.Key))
                        continue;

                    var prop = props[kvp.Key];
                    prop.SetValue(null, kvp.Value);
                }
            }
            else
            {
                UserName = ConfigurationHelper.Get("SO_UserName");
                Password = ConfigurationHelper.Get("SO_Password");
                ChatRoomID = ConfigurationHelper.Get<int>("ChatRoomID");

                DisablePolling = ConfigurationHelper.Get("DisablePolling", false);
                ProxyUrl = ConfigurationHelper.Get("ProxyUrl", string.Empty);
                ProxyUsername = ConfigurationHelper.Get("ProxyUsername", string.Empty);
                ProxyPassword = ConfigurationHelper.Get("ProxyPassword", string.Empty);
            }
        }

#if DEBUG
        public static string DefaultConfigurationFile => JsonConvert.SerializeObject(typeof(GlobalConfiguration).GetProperties().Where(p => p.Name != nameof(DefaultConfigurationFile)).ToDictionary(p => p.Name, p => p.GetValue(null)), Formatting.Indented);
#endif

        public static string UserName { get; private set; }
        public static string Password { get; private set; }

        public static long ChatRoomID { get; private set; }

        public static bool EnableHangfire { get; private set; }
        public static bool DisablePolling { get; private set; }

        public static string ProxyUrl { get; private set; }
        public static string ProxyUsername { get; private set; }
        public static string ProxyPassword { get; private set; }

    }
}
