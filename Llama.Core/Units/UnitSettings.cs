using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Llama.Core.Units
{
    /// <summary>
    /// Persists the current <see cref="UnitSystem"/> selections to a JSON file
    /// stored next to the executing assembly.
    /// </summary>
    public static class UnitSettings
    {
        private static readonly string SettingsPath;

        static UnitSettings()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            SettingsPath = Path.Combine(assemblyDir ?? ".", "units.json");
        }

        /// <summary>
        /// Load saved unit selections from disk and apply them to <see cref="UnitSystem"/>.
        /// If the file does not exist or is invalid, defaults are kept.
        /// </summary>
        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return;

                var json = File.ReadAllText(SettingsPath);
                var data = JsonSerializer.Deserialize<UnitSettingsData>(json);
                if (data == null)
                    return;

                UnitSystem.Length = ParseEnum(data.Length, UnitSystem.Length);
                UnitSystem.Force = ParseEnum(data.Force, UnitSystem.Force);
                UnitSystem.Mass = ParseEnum(data.Mass, UnitSystem.Mass);
                UnitSystem.Temperature = ParseEnum(data.Temperature, UnitSystem.Temperature);
                UnitSystem.Angle = ParseEnum(data.Angle, UnitSystem.Angle);
            }
            catch
            {
                // Corrupt file — keep defaults.
            }
        }

        /// <summary>
        /// Persist the current <see cref="UnitSystem"/> selections to disk.
        /// </summary>
        public static void Save()
        {
            try
            {
                var data = new UnitSettingsData
                {
                    Length = UnitSystem.Length.ToString(),
                    Force = UnitSystem.Force.ToString(),
                    Mass = UnitSystem.Mass.ToString(),
                    Temperature = UnitSystem.Temperature.ToString(),
                    Angle = UnitSystem.Angle.ToString()
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Non-critical — settings will revert to defaults next session.
            }
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
        {
            if (string.IsNullOrEmpty(value))
                return fallback;
            return Enum.TryParse<T>(value, true, out var result) ? result : fallback;
        }

        private class UnitSettingsData
        {
            public string Length { get; set; }
            public string Force { get; set; }
            public string Mass { get; set; }
            public string Temperature { get; set; }
            public string Angle { get; set; }
        }
    }
}
