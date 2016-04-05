using System;
using Microsoft.Azure;

namespace Utils
{
    public static class ConfigurationHelper
    {
        public static string Get(string key)
        {
            return Get<string>(key);
        }

        public static string Get(string key, string defaultValue)
        {
            return Get<string>(key, defaultValue);
        }

        public static T Get<T>(String key)
        {
            return InternalGet<T>(key);
        }

        public static T Get<T>(string key, T defaultValue)
        {
            return InternalGet(key, defaultValue, true);
        }
        
        private static T InternalGet<T>(String key, T defaultValue = default(T), bool defaultProvided = false)
        {
            var stringValue = CloudConfigurationManager.GetSetting(key, false);

            if (String.IsNullOrEmpty(stringValue))
            {
                if (!defaultProvided)
                    throw new ConfigurationException("Key '" + key + "' not found.");

                return defaultValue;
            }

            var returnType = typeof(T);

            if (returnType == typeof(String))
            {
                return (T)(object)stringValue;
            }
            if (returnType == typeof(bool))
            {
                bool value;
                if (bool.TryParse(stringValue, out value))
                    return (T)(object)value;
            }
            else if (returnType == typeof(int))
            {
                int value;
                if (int.TryParse(stringValue, out value))
                    return (T)(object)value;
            }
            else if (returnType == typeof(int?))
            {
                int value;
                if (int.TryParse(stringValue, out value))
                    return (T)(object)value;
                return default(T);
            }

            throw new ConfigurationException("Invalid value for '" + key + "'");
        }
    }

    public class ConfigurationException : Exception { public ConfigurationException(String message) : base(message) { } }
}
