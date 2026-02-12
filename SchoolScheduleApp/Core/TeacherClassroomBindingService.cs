using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SchoolScheduleApp.Core
{
    public static class TeacherClassroomBindingService
    {
        private static readonly object Locker = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string StoragePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "teacher_classrooms.json");

        public static int? GetClassroomId(int teacherId)
        {
            lock (Locker)
            {
                var map = LoadUnsafe();
                return map.TryGetValue(teacherId, out var id) ? id : null;
            }
        }

        public static IReadOnlyDictionary<int, int> GetAll()
        {
            lock (Locker)
            {
                return new Dictionary<int, int>(LoadUnsafe());
            }
        }

        public static void SetClassroom(int teacherId, int? classroomId)
        {
            lock (Locker)
            {
                var map = LoadUnsafe();

                if (classroomId.HasValue)
                {
                    map[teacherId] = classroomId.Value;
                }
                else
                {
                    map.Remove(teacherId);
                }

                SaveUnsafe(map);
            }
        }

        private static Dictionary<int, int> LoadUnsafe()
        {
            if (!File.Exists(StoragePath))
            {
                return new Dictionary<int, int>();
            }

            var json = File.ReadAllText(StoragePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<int, int>();
            }

            return JsonSerializer.Deserialize<Dictionary<int, int>>(json, JsonOptions) ?? new Dictionary<int, int>();
        }

        private static void SaveUnsafe(Dictionary<int, int> map)
        {
            var json = JsonSerializer.Serialize(map, JsonOptions);
            File.WriteAllText(StoragePath, json);
        }
    }
}
