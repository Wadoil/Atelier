using AtelierApp.Data;
using AtelierApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AtelierApp.Pages
{
    public partial class MasterWorkspace : Page
    {
        private readonly AppDbContext _context;
        private readonly User _currentUser;
        private Employee _currentEmployee;

        // Коллекции для отображения
        private ObservableCollection<OrderInfo> _ordersInProgress;
        private ObservableCollection<OrderInfo> _ordersForAssignment;
        private ObservableCollection<SeamstressInfo> _seamstresses;
        private ObservableCollection<OrderInfo> _qualityControlOrders;

        internal MasterWorkspace(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _context = new AppDbContext();

            // Получаем данные текущего мастера
            LoadCurrentEmployee();

            // Инициализация коллекций
            _ordersInProgress = new ObservableCollection<OrderInfo>();
            _ordersForAssignment = new ObservableCollection<OrderInfo>();
            _seamstresses = new ObservableCollection<SeamstressInfo>();
            _qualityControlOrders = new ObservableCollection<OrderInfo>();

            // Привязка к ListView
            OrdersInProgressListView.ItemsSource = _ordersInProgress;
            OrdersForAssignmentListView.ItemsSource = _ordersForAssignment;
            SeamstressesComboBox.ItemsSource = _seamstresses;
            QualityControlListView.ItemsSource = _qualityControlOrders;

            // Загрузка данных
            LoadOrdersInProgress();
            LoadOrdersForAssignment();
            LoadSeamstresses();
            LoadQualityControlOrders();
        }

        private void LoadCurrentEmployee()
        {
            try
            {
                _currentEmployee = _context.Employees
                    .Include(e => e.Position)
                    .FirstOrDefault(e => e.UserID == _currentUser.ID);
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки данных сотрудника: {ex.Message}");
            }
        }

        private void LoadOrdersInProgress()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.Client)
                        .ThenInclude(c => c.User)
                    .Include(o => o.Status)
                    .Include(o => o.OrderEmployees)
                        .ThenInclude(oe => oe.Employee)
                            .ThenInclude(e => e.User)
                    .Where(o => o.Status.Name == "В работе" || o.Status.Name == "Готов к примерке")
                    .OrderBy(o => o.DateOfReadiness)
                    .ToList();

                _ordersInProgress.Clear();
                foreach (var order in orders)
                {
                    // Защита от null
                    if (order.Client?.User == null) continue;

                    string seamstressName = "Не назначена";
                    var seamstressAssignment = order.OrderEmployees
                        ?.FirstOrDefault(oe => oe.Employee?.Position?.Name == "Швея");

                    if (seamstressAssignment?.Employee?.User != null)
                    {
                        seamstressName = $"{seamstressAssignment.Employee.User.Surname} {seamstressAssignment.Employee.User.Name}";
                    }

                    _ordersInProgress.Add(new OrderInfo
                    {
                        OrderId = order.ID,
                        ClientName = order.Client.User != null
                            ? $"{order.Client.User.Surname} {order.Client.User.Name}"
                            : "Неизвестный клиент",
                        Description = order.Description ?? "",
                        Status = order.Status?.Name ?? "Неизвестно",
                        Cost = order.Cost,
                        DateOfReadiness = order.DateOfReadiness,
                        Seamstress = seamstressName,
                        Order = order
                    });
                }

                OrdersInProgressListView.Items.Refresh();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void LoadOrdersForAssignment()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.Client)
                        .ThenInclude(c => c.User)
                    .Include(o => o.Status)
                    .Where(o => o.Status.Name == "Новый" || o.Status.Name == "Крой готов")
                    .OrderBy(o => o.DateOfReadiness)
                    .ToList();

                _ordersForAssignment.Clear();
                foreach (var order in orders)
                {
                    _ordersForAssignment.Add(new OrderInfo
                    {
                        OrderId = order.ID,
                        ClientName = $"{order.Client.User.Surname} {order.Client.User.Name}",
                        Description = order.Description,
                        Status = order.Status.Name,
                        Cost = order.Cost,
                        DateOfReadiness = order.DateOfReadiness,
                        Order = order
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void LoadSeamstresses()
        {
            try
            {
                var seamstresses = _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Position)
                    .Where(e => e.Position.Name == "Швея")
                    .ToList();

                _seamstresses.Clear();
                foreach (var seamstress in seamstresses)
                {
                    // Получаем актуальное количество активных заказов
                    int activeOrders = _context.OrderEmployees
                        .Count(oe => oe.EmployeeID == seamstress.ID);

                    _seamstresses.Add(new SeamstressInfo
                    {
                        EmployeeId = seamstress.ID,
                        FullName = $"{seamstress.User?.Surname} {seamstress.User?.Name}",
                        CurrentLoad = activeOrders
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки швей: {ex.Message}");
            }
        }

        private int GetSeamstressLoad(int employeeId)
        {
            return _context.OrderEmployees
                .Count(oe => oe.EmployeeID == employeeId);
        }

        private void LoadQualityControlOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.Client)
                        .ThenInclude(c => c.User)
                    .Include(o => o.Status)
                    .Include(o => o.OrderEmployees)
                        .ThenInclude(oe => oe.Employee)
                            .ThenInclude(e => e.User)
                    .Where(o => o.Status.Name == "Готов к выдаче" ||
                               o.Status.Name == "Готов к примерке")
                    .OrderBy(o => o.DateOfReadiness)
                    .ToList();

                _qualityControlOrders.Clear();
                foreach (var order in orders)
                {
                    if (order.Client?.User == null) continue;

                    string seamstressName = "Не назначена";
                    var seamstressAssignment = order.OrderEmployees
                        ?.FirstOrDefault(oe => oe.Employee?.Position?.Name == "Швея");

                    if (seamstressAssignment?.Employee?.User != null)
                    {
                        seamstressName = $"{seamstressAssignment.Employee.User.Surname} {seamstressAssignment.Employee.User.Name}";
                    }

                    _qualityControlOrders.Add(new OrderInfo
                    {
                        OrderId = order.ID,
                        ClientName = $"{order.Client.User.Surname} {order.Client.User.Name}",
                        Description = order.Description ?? "",
                        Status = order.Status?.Name ?? "Неизвестно",
                        Cost = order.Cost,
                        DateOfReadiness = order.DateOfReadiness,
                        Seamstress = seamstressName,
                        Order = order
                    });
                }

                QualityControlListView.Items.Refresh();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void AssignSeamstress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is OrderInfo orderInfo)
                {
                    var selectedSeamstress = SeamstressesComboBox.SelectedItem as SeamstressInfo;

                    if (selectedSeamstress == null)
                    {
                        ShowMessage("Выберите швею");
                        return;
                    }

                    // Начинаем транзакцию
                    using (var transaction = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Получаем актуальный заказ из БД
                            var order = _context.Orders.Find(orderInfo.OrderId);
                            if (order == null)
                            {
                                ShowMessage("Заказ не найден");
                                return;
                            }

                            // Проверяем, есть ли уже назначение для этого заказа
                            var existingAssignment = _context.OrderEmployees
                                .FirstOrDefault(oe => oe.OrderID == orderInfo.OrderId);

                            if (existingAssignment != null)
                            {
                                // Обновляем существующее назначение
                                existingAssignment.EmployeeID = selectedSeamstress.EmployeeId;
                                _context.OrderEmployees.Update(existingAssignment);
                            }
                            else
                            {
                                // Создаем новое назначение
                                _context.OrderEmployees.Add(new OrderEmployee
                                {
                                    OrderID = orderInfo.OrderId,
                                    EmployeeID = selectedSeamstress.EmployeeId
                                });
                            }

                            // Обновляем статус заказа
                            var inProgressStatus = _context.Statuses
                                .FirstOrDefault(s => s.Name == "В работе");

                            if (inProgressStatus != null)
                            {
                                order.StatusID = inProgressStatus.ID;
                                _context.Orders.Update(order);
                            }

                            // Сохраняем изменения
                            _context.SaveChanges();

                            // Подтверждаем транзакцию
                            transaction.Commit();

                            ShowMessage("Швея назначена успешно!", "Green");

                            // Обновляем списки
                            LoadOrdersForAssignment();
                            LoadOrdersInProgress();
                            LoadSeamstresses(); // Обновляем загрузку швей

                            // Сбрасываем выбор в комбобоксе
                            SeamstressesComboBox.SelectedItem = null;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ShowMessage($"Ошибка при сохранении: {ex.InnerException?.Message ?? ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка при назначении швеи: {ex.Message}");
            }
        }

        private void MarkAsReady_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is OrderInfo orderInfo)
                {
                    var order = _context.Orders.Find(orderInfo.OrderId);
                    if (order != null)
                    {
                        var readyForIssueStatus = _context.Statuses.First(s => s.Name == "Готов к выдаче");
                        order.StatusID = readyForIssueStatus.ID;

                        _context.SaveChanges();

                        ShowMessage("Заказ отмечен как готовый!", "Green");

                        // Обновляем списки
                        LoadOrdersInProgress();
                        LoadQualityControlOrders();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка при отметке заказа: {ex.Message}");
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

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOrdersInProgress();
            LoadOrdersForAssignment();
            LoadSeamstresses();
            LoadQualityControlOrders();
            ShowMessage("Данные обновлены", "Green");
        }
    }

    // Классы для отображения данных
    public class OrderInfo
    {
        public int OrderId { get; set; }
        public string ClientName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public decimal Cost { get; set; }
        public DateTime? DateOfReadiness { get; set; }
        public string Seamstress { get; set; }
        internal Order Order { get; set; }

        public string DisplayDate => DateOfReadiness?.ToString("dd.MM.yyyy") ?? "Не указана";
        public string DisplayCost => Cost.ToString("C");
        public SolidColorBrush StatusColor
        {
            get
            {
                switch (Status)
                {
                    case "В работе": return Brushes.Orange;
                    case "Готов к примерке": return Brushes.Blue;
                    case "Готов к выдаче": return Brushes.Green;
                    case "Новый": return Brushes.Gray;
                    default: return Brushes.Black;
                }
            }
        }
    }

    public class SeamstressInfo
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public int CurrentLoad { get; set; }

        public string DisplayInfo => $"{FullName} (заказов: {CurrentLoad})";
    }
}