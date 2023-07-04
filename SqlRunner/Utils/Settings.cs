using System;
using System.IO;

namespace SqlRunner.Utils
{
    internal class Settings
    {
        public static string PreferencesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Preferences.json");
    }
}
