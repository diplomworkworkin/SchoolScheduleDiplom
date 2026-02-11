using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace SchoolScheduleApp.ViewModels
{
    public class MessageItemViewModel
    {
        public Guid Id { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string CategoryText { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public Brush StatusBrush { get; set; } = Brushes.Gray;
        public string CreatedAtText { get; set; } = string.Empty;
        public string ProcessedAtText { get; set; } = "—";
        public string AdminComment { get; set; } = "—";
        public bool CanReview { get; set; }
    }

    public class MessageCategoryOption
    {
        public MessageCategory Value { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class MessagesViewModel : ViewModelBase
    {
        private string _messageBody = string.Empty;
        private MessageCategory _selectedCategory = MessageCategory.LessonReplacement;

        public bool IsAdmin => UserSession.CurrentUser?.Role == UserRole.Admin;
        public bool IsTeacher => UserSession.CurrentUser?.Role == UserRole.Teacher;

        public ObservableCollection<MessageItemViewModel> Messages { get; } = new();

        public IReadOnlyList<MessageCategoryOption> Categories { get; } = new[]
        {
            new MessageCategoryOption { Value = MessageCategory.LessonReplacement, Title = "Замена урока" },
            new MessageCategoryOption { Value = MessageCategory.ScheduleChange, Title = "Внести изменения" }
        };

        public MessageCategory SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        private MessageCategoryOption? _selectedCategoryOption;
        public MessageCategoryOption? SelectedCategoryOption
        {
            get => _selectedCategoryOption;
            set
            {
                _selectedCategoryOption = value;
                if (value != null)
                {
                    SelectedCategory = value.Value;
                }
                OnPropertyChanged();
            }
        }

        public string MessageBody
        {
            get => _messageBody;
            set { _messageBody = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand SendCommand { get; }
        public RelayCommand ApproveCommand { get; }
        public RelayCommand RejectCommand { get; }

        public MessagesViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadMessages());
            SendCommand = new RelayCommand(_ => SendMessage());
            ApproveCommand = new RelayCommand(o => ReviewMessage(o as MessageItemViewModel, true));
            RejectCommand = new RelayCommand(o => ReviewMessage(o as MessageItemViewModel, false));

            SelectedCategoryOption = Categories.FirstOrDefault();
            LoadMessages();
        }

        private void SendMessage()
        {
            if (!IsTeacher)
            {
                return;
            }

            var user = UserSession.CurrentUser;
            if (user == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(MessageBody))
            {
                ToastService.Show("Введите текст сообщения для администратора.", "Проверка", true);
                return;
            }

            MessageRequestService.CreateFromTeacher(user, SelectedCategory, MessageBody);
            MessageBody = string.Empty;
            ToastService.Show("Сообщение отправлено администратору.", "Сообщения");
            LoadMessages();
        }

        private void ReviewMessage(MessageItemViewModel? message, bool approve)
        {
            if (!IsAdmin || message == null)
            {
                return;
            }

            var status = approve ? MessageStatus.Approved : MessageStatus.Rejected;
            var comment = approve
                ? "Заявка принята. Проверьте изменения в расписании."
                : "Заявка отклонена. Уточните детали и отправьте снова.";

            var ok = MessageRequestService.UpdateStatus(message.Id, status, comment);
            if (!ok)
            {
                ToastService.Show("Не удалось обновить заявку.", "Ошибка", true);
                return;
            }

            ToastService.Show(approve ? "Заявка принята." : "Заявка отклонена.", "Сообщения");
            LoadMessages();
        }

        private void LoadMessages()
        {
            Messages.Clear();

            if (IsAdmin)
            {
                var items = MessageRequestService.GetForAdmin();
                foreach (var item in items)
                {
                    Messages.Add(Map(item));
                }

                MessageRequestService.MarkAdminRead();
                return;
            }

            if (IsTeacher && UserSession.CurrentUser?.TeacherId is int teacherId)
            {
                var items = MessageRequestService.GetForTeacher(teacherId);
                foreach (var item in items)
                {
                    Messages.Add(Map(item));
                }

                MessageRequestService.MarkTeacherRead(teacherId);
            }
        }

        private MessageItemViewModel Map(MessageRequest item)
        {
            return new MessageItemViewModel
            {
                Id = item.Id,
                TeacherName = item.TeacherName,
                CategoryText = item.Category == MessageCategory.LessonReplacement ? "Замена урока" : "Внести изменения",
                Body = item.Body,
                StatusText = item.Status switch
                {
                    MessageStatus.Pending => "Ожидает",
                    MessageStatus.Approved => "Принято",
                    MessageStatus.Rejected => "Отклонено",
                    _ => "Неизвестно"
                },
                StatusBrush = item.Status switch
                {
                    MessageStatus.Pending => Brushes.Goldenrod,
                    MessageStatus.Approved => Brushes.LimeGreen,
                    MessageStatus.Rejected => Brushes.IndianRed,
                    _ => Brushes.Gray
                },
                CreatedAtText = item.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                ProcessedAtText = item.ProcessedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
                AdminComment = string.IsNullOrWhiteSpace(item.AdminComment) ? "—" : item.AdminComment,
                CanReview = IsAdmin && item.Status == MessageStatus.Pending
            };
        }
    }
}
