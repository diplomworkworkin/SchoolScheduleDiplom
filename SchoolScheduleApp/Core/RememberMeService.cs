using System;
using System.IO;
using System.Text.Json;

namespace SchoolScheduleApp.Core
{
    public class RememberMeData
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public static class RememberMeService
    {
        private static readonly object Locker = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static string StoragePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember_me.json");

        public static RememberMeData? Load()
        {
            lock (Locker)
            {
                if (!File.Exists(StoragePath))
                {
                    return null;
                }

                var json = File.ReadAllText(StoragePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<RememberMeData>(json, JsonOptions);
            }
        }

        public static void Save(string username, string password)
        {
            lock (Locker)
            {
                var data = new RememberMeData
                {
                    Username = username?.Trim() ?? string.Empty,
                    Password = password ?? string.Empty
                };

                File.WriteAllText(StoragePath, JsonSerializer.Serialize(data, JsonOptions));
            }
        }

        public static void Clear()
        {
            lock (Locker)
            {
                if (File.Exists(StoragePath))
                {
                    File.Delete(StoragePath);
                }
            }
        }
    }
}
