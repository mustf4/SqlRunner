using System;
using System.IO;
using System.Text.Json;

namespace SqlRunner.Utils
{
    internal static class Serializer
    {
        public static void Serialize<T>(string path, T target)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (target == null) throw new ArgumentNullException(nameof(target));

            string serializedValue = JsonSerializer.Serialize(target);
            File.WriteAllText(path, serializedValue);
        }

        public static TR Deserialize<TR>(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path))
                return default;

            return JsonSerializer.Deserialize<TR>(File.ReadAllText(path));
        }
    }
}
