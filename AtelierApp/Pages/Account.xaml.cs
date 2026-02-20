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
    public partial class Account : Page
    {
        private readonly AppDbContext _context;
        private readonly User _currentUser;
        private Client _client;

        // Коллекции - используем уникальное имя класса
        private ObservableCollection<AccountOrderInfo> _recentOrders;

        internal Account(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _context = new AppDbContext();

            // Инициализация коллекций
            _recentOrders = new ObservableCollection<AccountOrderInfo>();
            RecentOrdersListView.ItemsSource = _recentOrders;

            // Загрузка данных
            LoadUserData();
            LoadRecentOrders();
        }

        private void LoadUserData()
        {
            try
            {
                // Загружаем клиента
                _client = _context.Clients
                    .FirstOrDefault(c => c.UserID == _currentUser.ID);

                // Заполняем поля
                SurnameTextBlock.Text = _currentUser.Surname ?? "";
                NameTextBlock.Text = _currentUser.Name ?? "";
                MiddleNameTextBlock.Text = _currentUser.MiddleName ?? "";
                EmailTextBlock.Text = _currentUser.Email ?? "";
                PhoneTextBox.Text = _currentUser.PhoneNumber ?? "";

                if (_client != null)
                {
                    AddressTextBox.Text = _client.Address ?? "";
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки данных: {ex.Message}", "Red");
            }
        }

        private void LoadRecentOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Status)
                    .Include(o => o.OrderServices)
                        .ThenInclude(os => os.Service)
                    .Where(o => o.Client.UserID == _currentUser.ID)
                    .OrderByDescending(o => o.ID)
                    .Take(5)
                    .ToList();

                _recentOrders.Clear();
                foreach (var order in orders)
                {
                    var services = _context.OrderServices
                        .Where(os => os.OrderID == order.ID)
                        .Select(os => os.Service.Name)
                        .ToList();

                    _recentOrders.Add(new AccountOrderInfo
                    {
                        ID = order.ID,
                        OrderDate = order.DateOfIssue ?? order.DateOfReadiness ?? DateTime.Now,
                        StatusName = order.Status?.Name ?? "Неизвестно",
                        Cost = order.Cost,
                        Description = order.Description ?? "",
                        Services = string.Join(", ", services)
                    });
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки заказов: {ex.Message}", "Red");
            }
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновляем данные пользователя
                _currentUser.PhoneNumber = PhoneTextBox.Text;

                if (_client != null)
                {
                    _client.Address = AddressTextBox.Text;
                    _context.Clients.Update(_client);
                }

                _context.Users.Update(_currentUser);
                _context.SaveChanges();

                ShowMessage("Данные успешно сохранены!", "Green");
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка сохранения: {ex.Message}", "Red");
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ChangePasswordDialog(_currentUser);
            if (dialog.ShowDialog() == true)
            {
                ShowMessage("Пароль успешно изменен!", "Green");
            }
        }

        private void ViewOrderDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int orderId)
            {
                var order = _recentOrders.FirstOrDefault(o => o.ID == orderId);
                if (order != null)
                {
                    MessageBox.Show(
                        $"Заказ №{order.ID}\n\n" +
                        $"Дата: {order.OrderDate:dd.MM.yyyy}\n" +
                        $"Статус: {order.StatusName}\n" +
                        $"Услуги: {order.Services}\n" +
                        $"Описание: {order.Description}\n" +
                        $"Стоимость: {order.Cost:C}",
                        "Детали заказа",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void ShowMessage(string message, string color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = color == "Red" ?
                System.Windows.Media.Brushes.Red :
                System.Windows.Media.Brushes.Green;
            StatusTextBlock.Visibility = Visibility.Visible;
        }
    }

    // УНИКАЛЬНОЕ ИМЯ КЛАССА - переименовано с OrderInfo на AccountOrderInfo
    public class AccountOrderInfo
    {
        public int ID { get; set; }
        public DateTime OrderDate { get; set; }
        public string StatusName { get; set; }
        public decimal Cost { get; set; }
        public string Description { get; set; }
        public string Services { get; set; }
    }
}