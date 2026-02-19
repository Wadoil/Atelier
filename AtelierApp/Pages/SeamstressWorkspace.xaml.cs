using AtelierApp.Data;
using AtelierApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AtelierApp.Pages
{
    public partial class SeamstressWorkspace : Page
    {
        private readonly AppDbContext _context;
        private readonly User _currentUser;
        private Employee _currentSeamstress;

        // Коллекции
        private ObservableCollection<SeamstressTask> _myTasks;
        private ObservableCollection<string> _statusFilters;

        // Текущий выбранный фильтр
        private string _selectedStatus;

        internal SeamstressWorkspace(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _context = new AppDbContext();

            // Инициализация коллекций
            _myTasks = new ObservableCollection<SeamstressTask>();
            _statusFilters = new ObservableCollection<string>();

            // Привязка к ListView
            MyTasksListView.ItemsSource = _myTasks;
            StatusFilterComboBox.ItemsSource = _statusFilters;

            // Загрузка данных
            LoadCurrentSeamstress();
            LoadStatusFilters();
            LoadMyTasks();
        }

        private void LoadCurrentSeamstress()
        {
            try
            {
                _currentSeamstress = _context.Employees
                    .Include(e => e.Position)
                    .FirstOrDefault(e => e.UserID == _currentUser.ID);

                if (_currentSeamstress == null)
                {
                    ShowMessage("Сотрудник не найден");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadStatusFilters()
        {
            _statusFilters.Clear();
            _statusFilters.Add("Все задания");
            _statusFilters.Add("Новые");
            _statusFilters.Add("В работе");
            _statusFilters.Add("Готово");

            StatusFilterComboBox.SelectedIndex = 0;
        }
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMyTasks();
            ShowMessage("Список заданий обновлен", "Green");
        }
        private void LoadMyTasks()
        {
            try
            {
                if (_currentSeamstress == null) return;

                var tasks = _context.OrderEmployees
                    .Include(oe => oe.Order)
                        .ThenInclude(o => o.Client)
                            .ThenInclude(c => c.User)
                    .Include(oe => oe.Order)
                        .ThenInclude(o => o.Status)
                    .Include(oe => oe.Order)
                        .ThenInclude(o => o.OrderServices)
                            .ThenInclude(os => os.Service)
                    .Where(oe => oe.EmployeeID == _currentSeamstress.ID)
                    .Select(oe => oe.Order)
                    .Where(o => o.Status.Name != "Выдан" && o.Status.Name != "Отменен")
                    .OrderBy(o => o.DateOfReadiness)
                    .ToList();

                _myTasks.Clear();
                foreach (var order in tasks)
                {
                    if (order.Client?.User == null) continue;

                    // Получаем список услуг для этого заказа
                    var services = _context.OrderServices
                        .Where(os => os.OrderID == order.ID)
                        .Select(os => os.Service.Name)
                        .ToList();

                    _myTasks.Add(new SeamstressTask
                    {
                        OrderId = order.ID,
                        Description = order.Description ?? "",
                        Deadline = order.DateOfReadiness,
                        StatusName = order.Status?.Name ?? "Неизвестно",
                        ClientName = $"{order.Client.User.Surname} {order.Client.User.Name}",
                        Services = string.Join(", ", services),
                        Cost = order.Cost,
                        Order = order
                    });
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки заданий: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            if (_myTasks == null) return;

            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_myTasks);

            switch (_selectedStatus)
            {
                case "Новые":
                    view.Filter = item => ((SeamstressTask)item).StatusName == "Новый";
                    break;
                case "В работе":
                    view.Filter = item => ((SeamstressTask)item).StatusName == "В работе";
                    break;
                case "Готово":
                    view.Filter = item => ((SeamstressTask)item).StatusName == "Готов к примерке" ||
                                          ((SeamstressTask)item).StatusName == "Готов к выдаче";
                    break;
                default:
                    view.Filter = null; // Все задания
                    break;
            }
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is string selectedStatus)
            {
                _selectedStatus = selectedStatus;
                ApplyFilter();
            }
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is int orderId)
                {
                    var order = _context.Orders.Find(orderId);
                    if (order == null)
                    {
                        ShowMessage("Заказ не найден");
                        return;
                    }

                    // Определяем следующий статус в зависимости от текущего
                    string nextStatus = GetNextStatus(order.Status.Name);

                    var result = MessageBox.Show(
                        $"Подтвердить выполнение заказа №{orderId}?\n\n" +
                        $"Текущий статус: {order.Status.Name}\n" +
                        $"Следующий статус: {nextStatus}",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var newStatus = _context.Statuses.First(s => s.Name == nextStatus);
                        order.StatusID = newStatus.ID;

                        _context.SaveChanges();

                        ShowMessage($"Заказ №{orderId} переведен в статус '{nextStatus}'", "Green");

                        // Обновляем список заданий
                        LoadMyTasks();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка: {ex.Message}");
            }
        }

        private string GetNextStatus(string currentStatus)
        {
            switch (currentStatus)
            {
                case "Новый":
                    return "В работе";
                case "В работе":
                    return "Готов к примерке";
                case "Готов к примерке":
                    return "В работе"; // После примерки может быть доработка
                default:
                    return "Готов к выдаче";
            }
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                var task = _myTasks.FirstOrDefault(t => t.OrderId == orderId);
                if (task != null)
                {
                    MessageBox.Show(
                        $"Заказ №{task.OrderId}\n\n" +
                        $"Клиент: {task.ClientName}\n" +
                        $"Услуги: {task.Services}\n" +
                        $"Описание: {task.Description}\n" +
                        $"Стоимость: {task.Cost:C}\n" +
                        $"Статус: {task.StatusName}\n" +
                        $"Срок: {task.Deadline?.ToString("dd.MM.yyyy") ?? "Не указан"}",
                        "Детали заказа",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void ShowMessage(string message, string color = "Red")
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = color == "Red" ?
                System.Windows.Media.Brushes.Red :
                System.Windows.Media.Brushes.Green;
            StatusTextBlock.Visibility = Visibility.Visible;
        }
    }

    // Класс для задания швеи
    public class SeamstressTask
    {
        public int OrderId { get; set; }
        public string Description { get; set; }
        public DateTime? Deadline { get; set; }
        public string StatusName { get; set; }
        public string ClientName { get; set; }
        public string Services { get; set; }
        public decimal Cost { get; set; }
        internal Order Order { get; set; }

        public string DisplayDeadline => Deadline?.ToString("dd.MM.yyyy") ?? "Не указан";
        public string DisplayCost => Cost.ToString("C");
    }
}