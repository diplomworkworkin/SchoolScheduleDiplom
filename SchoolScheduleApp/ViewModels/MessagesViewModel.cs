using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SchoolScheduleApp.ViewModels
{
    public class MessageCategoryOption
    {
        public MessageCategory Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class DayOption
    {
        public int Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class TeacherClassOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ConversationItemViewModel
    {
        public Guid Id { get; set; }
        public int TeacherId { get; set; }
        public string Header { get; set; } = string.Empty;
        public string LastMessagePreview { get; set; } = string.Empty;
        public string UpdatedAtText { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public Brush StatusBrush { get; set; } = Brushes.Gray;
        public MessageCategory Category { get; set; }
        public string CategoryText { get; set; } = string.Empty;
        public int? TargetClassId { get; set; }
        public string? TargetClassName { get; set; }
        public int? TargetDayOfWeek { get; set; }
        public int? TargetLessonIndex { get; set; }
    }

    public class ChatMessageViewModel
    {
        public string Text { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public HorizontalAlignment Alignment { get; set; }
        public Brush BubbleBackground { get; set; } = Brushes.Transparent;
        public Brush BubbleForeground { get; set; } = Brushes.White;
    }

    public class MessagesViewModel : ViewModelBase
    {
        private readonly Brush _teacherBubble = (Brush)new BrushConverter().ConvertFrom("#2D4F6E");
        private readonly Brush _adminBubble = (Brush)new BrushConverter().ConvertFrom("#0EA5E9");

        private ConversationItemViewModel? _selectedConversation;
        private string _chatInput = string.Empty;
        private string _newRequestMessage = string.Empty;
        private bool _isComposerOpen;

        private MessageCategoryOption? _selectedCategoryOption;
        private TeacherClassOption? _selectedClassOption;
        private DayOption? _selectedDayOption;
        private int _lessonIndex = 1;

        public bool IsAdmin => UserSession.CurrentUser?.Role == UserRole.Admin;
        public bool IsTeacher => UserSession.CurrentUser?.Role == UserRole.Teacher;

        public ObservableCollection<ConversationItemViewModel> Conversations { get; } = new();
        public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = new();

        public IReadOnlyList<MessageCategoryOption> Categories { get; } = new[]
        {
            new MessageCategoryOption { Value = MessageCategory.LessonReplacement, Title = "Замена урока" },
            new MessageCategoryOption { Value = MessageCategory.ScheduleChange, Title = "Внести изменения" }
        };

        public IReadOnlyList<DayOption> Days { get; } = new[]
        {
            new DayOption { Value = 1, Title = "Понедельник" },
            new DayOption { Value = 2, Title = "Вторник" },
            new DayOption { Value = 3, Title = "Среда" },
            new DayOption { Value = 4, Title = "Четверг" },
            new DayOption { Value = 5, Title = "Пятница" },
            new DayOption { Value = 6, Title = "Суббота" }
        };

        public ObservableCollection<TeacherClassOption> TeacherClasses { get; } = new();

        public ConversationItemViewModel? SelectedConversation
        {
            get => _selectedConversation;
            set
            {
                _selectedConversation = value;
                OnPropertyChanged();
                LoadSelectedConversationMessages();
            }
        }

        public string ChatInput
        {
            get => _chatInput;
            set { _chatInput = value; OnPropertyChanged(); }
        }

        public string NewRequestMessage
        {
            get => _newRequestMessage;
            set { _newRequestMessage = value; OnPropertyChanged(); }
        }

        public bool IsComposerOpen
        {
            get => _isComposerOpen;
            set { _isComposerOpen = value; OnPropertyChanged(); }
        }

        public MessageCategoryOption? SelectedCategoryOption
        {
            get => _selectedCategoryOption;
            set { _selectedCategoryOption = value; OnPropertyChanged(); }
        }

        public TeacherClassOption? SelectedClassOption
        {
            get => _selectedClassOption;
            set { _selectedClassOption = value; OnPropertyChanged(); }
        }

        public DayOption? SelectedDayOption
        {
            get => _selectedDayOption;
            set { _selectedDayOption = value; OnPropertyChanged(); }
        }

        public int LessonIndex
        {
            get => _lessonIndex;
            set
            {
                _lessonIndex = value;
                OnPropertyChanged();
            }
        }

        public bool CanSendInChat => SelectedConversation != null;
        public bool CanModerate => IsAdmin && SelectedConversation != null && SelectedConversation.Status == MessageStatus.Pending;

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ToggleComposerCommand { get; }
        public RelayCommand CreateRequestCommand { get; }
        public RelayCommand SendChatMessageCommand { get; }
        public RelayCommand ApproveCommand { get; }
        public RelayCommand RejectCommand { get; }

        public MessagesViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadConversations());
            ToggleComposerCommand = new RelayCommand(_ => IsComposerOpen = !IsComposerOpen);
            CreateRequestCommand = new RelayCommand(_ => CreateRequest());
            SendChatMessageCommand = new RelayCommand(_ => SendChatMessage());
            ApproveCommand = new RelayCommand(_ => ProcessRequest(true));
            RejectCommand = new RelayCommand(_ => ProcessRequest(false));

            SelectedCategoryOption = Categories.FirstOrDefault();
            SelectedDayOption = Days.FirstOrDefault(x => x.Value == 5) ?? Days.FirstOrDefault();
            LoadTeacherClasses();
            LoadConversations();
        }

        private void LoadTeacherClasses()
        {
            TeacherClasses.Clear();

            if (!IsTeacher || UserSession.CurrentUser?.TeacherId == null)
            {
                return;
            }

            var teacherId = UserSession.CurrentUser.TeacherId.Value;

            using var db = new SchoolDbContext();
            var classes = db.Lessons
                .Where(x => x.TeacherId == teacherId)
                .Include(x => x.AcademicClass)
                .Select(x => new { x.AcademicClassId, x.AcademicClass.Name })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToList();

            foreach (var item in classes)
            {
                TeacherClasses.Add(new TeacherClassOption { Id = item.AcademicClassId, Name = item.Name });
            }

            SelectedClassOption = TeacherClasses.FirstOrDefault();
        }

        private void CreateRequest()
        {
            if (!IsTeacher)
            {
                return;
            }

            var user = UserSession.CurrentUser;
            if (user == null)
            {
                ToastService.Show("Сессия пользователя недоступна.", "Ошибка", true);
                return;
            }

            if (SelectedCategoryOption == null)
            {
                ToastService.Show("Выберите категорию запроса.", "Проверка", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewRequestMessage))
            {
                ToastService.Show("Введите сообщение для администратора.", "Проверка", true);
                return;
            }

            if (SelectedCategoryOption.Value == MessageCategory.LessonReplacement)
            {
                if (SelectedClassOption == null)
                {
                    ToastService.Show("Для замены урока выберите класс.", "Проверка", true);
                    return;
                }

                if (SelectedDayOption == null)
                {
                    ToastService.Show("Для замены урока выберите день недели.", "Проверка", true);
                    return;
                }

                if (LessonIndex < 1 || LessonIndex > 8)
                {
                    ToastService.Show("Номер урока должен быть от 1 до 8.", "Проверка", true);
                    return;
                }
            }

            try
            {
                var id = MessageRequestService.CreateThreadFromTeacher(
                    user,
                    SelectedCategoryOption.Value,
                    NewRequestMessage,
                    SelectedCategoryOption.Value == MessageCategory.LessonReplacement ? SelectedClassOption?.Id : null,
                    SelectedCategoryOption.Value == MessageCategory.LessonReplacement ? SelectedClassOption?.Name : null,
                    SelectedCategoryOption.Value == MessageCategory.LessonReplacement ? SelectedDayOption?.Value : null,
                    SelectedCategoryOption.Value == MessageCategory.LessonReplacement ? LessonIndex : null);

                ToastService.Show("Заявка отправлена администратору.", "Сообщения");
                NewRequestMessage = string.Empty;
                IsComposerOpen = false;

                LoadConversations(id);
            }
            catch (Exception ex)
            {
                ToastService.Show("Не удалось отправить заявку: " + ex.Message, "Ошибка", true);
            }
        }

        private void SendChatMessage()
        {
            if (SelectedConversation == null)
            {
                ToastService.Show("Выберите чат.", "Проверка", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(ChatInput))
            {
                return;
            }

            var ok = false;
            if (IsTeacher)
            {
                var user = UserSession.CurrentUser;
                if (user == null)
                {
                    ToastService.Show("Сессия пользователя недоступна.", "Ошибка", true);
                    return;
                }

                ok = MessageRequestService.AddTeacherMessage(SelectedConversation.Id, user, ChatInput);
            }
            else if (IsAdmin)
            {
                var adminName = UserSession.CurrentUser?.FullName ?? "Администратор";
                ok = MessageRequestService.AddAdminMessage(SelectedConversation.Id, adminName, ChatInput);
            }

            if (!ok)
            {
                ToastService.Show("Не удалось отправить сообщение.", "Ошибка", true);
                return;
            }

            ChatInput = string.Empty;
            LoadConversations(SelectedConversation.Id);
        }

        private void ProcessRequest(bool approve)
        {
            if (!IsAdmin || SelectedConversation == null)
            {
                return;
            }

            if (SelectedConversation.Status != MessageStatus.Pending)
            {
                ToastService.Show("Заявка уже обработана.", "Информация");
                return;
            }

            try
            {
                if (approve && SelectedConversation.Category == MessageCategory.LessonReplacement)
                {
                    ApplyLessonReplacement(SelectedConversation);
                }

                var adminName = UserSession.CurrentUser?.FullName ?? "Администратор";
                var phrase = approve ? "Принять заявку" : "Отклонить заявку";
                MessageRequestService.AddAdminMessage(SelectedConversation.Id, adminName, phrase);

                var statusUpdated = MessageRequestService.UpdateStatus(
                    SelectedConversation.Id,
                    approve ? MessageStatus.Approved : MessageStatus.Rejected);

                if (!statusUpdated)
                {
                    ToastService.Show("Не удалось обновить статус заявки.", "Ошибка", true);
                    return;
                }

                ToastService.Show(approve ? "Заявка принята." : "Заявка отклонена.", "Сообщения");
                LoadConversations(SelectedConversation.Id);
            }
            catch (Exception ex)
            {
                ToastService.Show("Не удалось обработать заявку: " + ex.Message, "Ошибка", true);
            }
        }

        private void ApplyLessonReplacement(ConversationItemViewModel conversation)
        {
            if (conversation.TargetClassId == null
                || conversation.TargetDayOfWeek == null
                || conversation.TargetLessonIndex == null)
            {
                throw new InvalidOperationException("В заявке на замену отсутствуют класс/день/урок.");
            }

            using var db = new SchoolDbContext();

            var teacher = db.Teachers.FirstOrDefault(t => t.Id == conversation.TeacherId);
            if (teacher == null)
            {
                throw new InvalidOperationException("Учитель из заявки не найден.");
            }

            var lesson = db.Lessons.FirstOrDefault(l =>
                l.AcademicClassId == conversation.TargetClassId.Value
                && l.DayOfWeek == conversation.TargetDayOfWeek.Value
                && l.LessonIndex == conversation.TargetLessonIndex.Value);

            if (lesson == null)
            {
                throw new InvalidOperationException("Урок для замены не найден (класс/день/номер).");
            }

            var teacherBusy = db.Lessons.Any(l =>
                l.TeacherId == teacher.Id
                && l.DayOfWeek == lesson.DayOfWeek
                && l.LessonIndex == lesson.LessonIndex
                && l.Id != lesson.Id);

            if (teacherBusy)
            {
                throw new InvalidOperationException("Учитель уже занят в это время. Выберите другое решение.");
            }

            lesson.TeacherId = teacher.Id;
            if (teacher.SubjectId.HasValue)
            {
                lesson.SubjectId = teacher.SubjectId.Value;
            }

            db.SaveChanges();
        }

        private void LoadConversations(Guid? selectId = null)
        {
            Conversations.Clear();

            IReadOnlyList<MessageThread> threads;
            if (IsAdmin)
            {
                threads = MessageRequestService.GetForAdmin();
                MessageRequestService.MarkAdminRead();
            }
            else if (IsTeacher && UserSession.CurrentUser?.TeacherId != null)
            {
                var teacherId = UserSession.CurrentUser.TeacherId.Value;
                threads = MessageRequestService.GetForTeacher(teacherId);
                MessageRequestService.MarkTeacherRead(teacherId);
            }
            else
            {
                threads = Array.Empty<MessageThread>();
            }

            foreach (var thread in threads)
            {
                Conversations.Add(MapConversation(thread));
            }

            var selected = selectId.HasValue
                ? Conversations.FirstOrDefault(x => x.Id == selectId.Value)
                : Conversations.FirstOrDefault();

            SelectedConversation = selected;
            OnPropertyChanged(nameof(CanSendInChat));
            OnPropertyChanged(nameof(CanModerate));
        }

        private void LoadSelectedConversationMessages()
        {
            ChatMessages.Clear();
            OnPropertyChanged(nameof(CanSendInChat));
            OnPropertyChanged(nameof(CanModerate));

            if (SelectedConversation == null)
            {
                return;
            }

            MessageThread? thread;
            if (IsAdmin)
            {
                thread = MessageRequestService.GetForAdmin().FirstOrDefault(x => x.Id == SelectedConversation.Id);
            }
            else if (IsTeacher && UserSession.CurrentUser?.TeacherId != null)
            {
                var teacherId = UserSession.CurrentUser.TeacherId.Value;
                thread = MessageRequestService.GetForTeacher(teacherId).FirstOrDefault(x => x.Id == SelectedConversation.Id);
            }
            else
            {
                thread = null;
            }

            if (thread == null)
            {
                return;
            }

            foreach (var msg in thread.Messages.OrderBy(x => x.SentAt))
            {
                var fromCurrentUser = (IsTeacher && msg.SenderRole == UserRole.Teacher)
                    || (IsAdmin && msg.SenderRole == UserRole.Admin);

                ChatMessages.Add(new ChatMessageViewModel
                {
                    Text = msg.Text,
                    Meta = $"{msg.SenderName} · {msg.SentAt:dd.MM.yyyy HH:mm}",
                    Alignment = fromCurrentUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    BubbleBackground = fromCurrentUser ? _adminBubble : _teacherBubble,
                    BubbleForeground = Brushes.White
                });
            }
        }

        private ConversationItemViewModel MapConversation(MessageThread thread)
        {
            var last = thread.Messages.LastOrDefault()?.Text ?? string.Empty;
            var shortLast = last.Length <= 68 ? last : last[..68] + "…";

            return new ConversationItemViewModel
            {
                Id = thread.Id,
                TeacherId = thread.TeacherId,
                Header = IsAdmin ? thread.TeacherName : "Администратор",
                LastMessagePreview = shortLast,
                UpdatedAtText = thread.UpdatedAt.ToString("dd.MM HH:mm"),
                Status = thread.Status,
                StatusText = StatusText(thread.Status),
                StatusBrush = StatusBrush(thread.Status),
                Category = thread.Category,
                CategoryText = thread.Category == MessageCategory.LessonReplacement ? "Замена урока" : "Внести изменения",
                TargetClassId = thread.TargetClassId,
                TargetClassName = thread.TargetClassName,
                TargetDayOfWeek = thread.TargetDayOfWeek,
                TargetLessonIndex = thread.TargetLessonIndex
            };
        }

        private static string StatusText(MessageStatus status) => status switch
        {
            MessageStatus.Pending => "Ожидает",
            MessageStatus.Approved => "Принята",
            MessageStatus.Rejected => "Отклонена",
            _ => "Неизвестно"
        };

        private static Brush StatusBrush(MessageStatus status) => status switch
        {
            MessageStatus.Pending => Brushes.Goldenrod,
            MessageStatus.Approved => Brushes.LimeGreen,
            MessageStatus.Rejected => Brushes.IndianRed,
            _ => Brushes.Gray
        };
    }
}
