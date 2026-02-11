using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SchoolScheduleApp.Core
{
    public enum MessageCategory
    {
        LessonReplacement = 0,
        ScheduleChange = 1
    }

    public enum MessageStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class MessageRequest
    {
        public Guid Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public MessageCategory Category { get; set; }
        public string Body { get; set; } = string.Empty;
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? AdminComment { get; set; }
        public bool IsReadByAdmin { get; set; }
        public bool IsReadByTeacher { get; set; }
    }

    public static class MessageRequestService
    {
        private static readonly object Locker = new();
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string StoragePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "message_requests.json");

        public static IReadOnlyList<MessageRequest> GetForAdmin()
        {
            lock (Locker)
            {
                return LoadUnsafe().OrderByDescending(x => x.CreatedAt).ToList();
            }
        }

        public static IReadOnlyList<MessageRequest> GetForTeacher(int teacherId)
        {
            lock (Locker)
            {
                return LoadUnsafe()
                    .Where(x => x.TeacherId == teacherId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }
        }

        public static int GetUnreadAdminCount()
        {
            lock (Locker)
            {
                return LoadUnsafe().Count(x => !x.IsReadByAdmin);
            }
        }

        public static int GetUnreadTeacherCount(int teacherId)
        {
            lock (Locker)
            {
                return LoadUnsafe().Count(x => x.TeacherId == teacherId && !x.IsReadByTeacher);
            }
        }

        public static void MarkAdminRead()
        {
            lock (Locker)
            {
                var items = LoadUnsafe();
                foreach (var item in items)
                {
                    item.IsReadByAdmin = true;
                }

                SaveUnsafe(items);
            }
        }

        public static void MarkTeacherRead(int teacherId)
        {
            lock (Locker)
            {
                var items = LoadUnsafe();
                foreach (var item in items.Where(x => x.TeacherId == teacherId))
                {
                    item.IsReadByTeacher = true;
                }

                SaveUnsafe(items);
            }
        }

        public static void CreateFromTeacher(User user, MessageCategory category, string body)
        {
            if (user.TeacherId == null)
            {
                throw new InvalidOperationException("Сообщение может отправлять только учитель.");
            }

            lock (Locker)
            {
                var items = LoadUnsafe();
                items.Add(new MessageRequest
                {
                    Id = Guid.NewGuid(),
                    TeacherId = user.TeacherId.Value,
                    TeacherName = user.FullName,
                    Category = category,
                    Body = body.Trim(),
                    Status = MessageStatus.Pending,
                    CreatedAt = DateTime.Now,
                    IsReadByAdmin = false,
                    IsReadByTeacher = true
                });

                SaveUnsafe(items);
            }
        }

        public static bool UpdateStatus(Guid id, MessageStatus status, string? adminComment)
        {
            lock (Locker)
            {
                var items = LoadUnsafe();
                var found = items.FirstOrDefault(x => x.Id == id);
                if (found == null)
                {
                    return false;
                }

                found.Status = status;
                found.ProcessedAt = DateTime.Now;
                found.AdminComment = string.IsNullOrWhiteSpace(adminComment) ? null : adminComment.Trim();
                found.IsReadByAdmin = true;
                found.IsReadByTeacher = false;

                SaveUnsafe(items);
                return true;
            }
        }

        private static List<MessageRequest> LoadUnsafe()
        {
            if (!File.Exists(StoragePath))
            {
                return new List<MessageRequest>();
            }

            var json = File.ReadAllText(StoragePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<MessageRequest>();
            }

            return JsonSerializer.Deserialize<List<MessageRequest>>(json, JsonOptions) ?? new List<MessageRequest>();
        }

        private static void SaveUnsafe(List<MessageRequest> data)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(StoragePath, json);
        }
    }
}
