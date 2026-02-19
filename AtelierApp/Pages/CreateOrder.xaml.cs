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
    public partial class CreateOrder : Page
    {
        private readonly AppDbContext _context;
        private readonly User _currentUser;

        // Коллекции для привязки данных
        private ObservableCollection<Service> _services;
        private ObservableCollection<SelectedServiceItem> _selectedServices;
        private Service _selectedService;
        private int _quantity = 1;
        private string _description;
        private decimal _totalCost;

        internal CreateOrder(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            _context = new AppDbContext();

            // Инициализация коллекций
            _selectedServices = new ObservableCollection<SelectedServiceItem>();
            SelectedServicesListView.ItemsSource = _selectedServices;

            // Загрузка данных
            LoadServices();

            // Подписка на изменения для обновления общей стоимости
            _selectedServices.CollectionChanged += (s, e) => RecalculateTotal();
        }

        private void LoadServices()
        {
            try
            {
                _services = new ObservableCollection<Service>(
                    _context.Services.OrderBy(s => s.Name).ToList()
                );
                ServicesComboBox.ItemsSource = _services;
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки услуг: {ex.Message}", "Red");
            }
        }

        private void RecalculateTotal()
        {
            _totalCost = _selectedServices.Sum(s => s.Total);
            TotalCostTextBlock.Text = _totalCost.ToString("C");
        }

        private void AddService_Click(object sender, RoutedEventArgs e)
        {
            // Проверка выбора услуги
            if (_selectedService == null)
            {
                ShowMessage("Выберите услугу", "Red");
                return;
            }

            // Проверка количества
            if (_quantity <= 0)
            {
                ShowMessage("Количество должно быть больше 0", "Red");
                return;
            }

            // Проверка существования услуги в списке
            var existingItem = _selectedServices.FirstOrDefault(s => s.ServiceId == _selectedService.ID);

            if (existingItem != null)
            {
                // Если услуга уже есть, увеличиваем количество
                existingItem.Quantity += _quantity;
                existingItem.Total = existingItem.Price * existingItem.Quantity;

                // Обновляем отображение
                var index = _selectedServices.IndexOf(existingItem);
                _selectedServices[index] = existingItem;
            }
            else
            {
                // Добавляем новую услугу
                _selectedServices.Add(new SelectedServiceItem
                {
                    ServiceId = _selectedService.ID,
                    ServiceName = _selectedService.Name,
                    Price = _selectedService.Price,
                    Quantity = _quantity,
                    Total = _selectedService.Price * _quantity
                });
            }

            // Сбрасываем выбор
            ServicesComboBox.SelectedIndex = -1;
            QuantityTextBox.Text = "1";

            // Общая стоимость пересчитается автоматически через CollectionChanged
            ShowMessage("", "Red"); // Очищаем сообщение об ошибке
        }

        private void RemoveService_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SelectedServiceItem item)
            {
                _selectedServices.Remove(item);
            }
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (!_selectedServices.Any())
                {
                    ShowMessage("Добавьте хотя бы одну услугу", "Red");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_description))
                {
                    ShowMessage("Заполните описание заказа", "Red");
                    return;
                }

                // Получаем клиента по текущему пользователю
                var client = _context.Clients.FirstOrDefault(c => c.UserID == _currentUser.ID);

                if (client == null)
                {
                    ShowMessage("Клиент не найден", "Red");
                    return;
                }

                // Получаем статус "Новый"
                var newStatus = _context.Statuses.FirstOrDefault(s => s.Name == "Новый");

                if (newStatus == null)
                {
                    ShowMessage("Статус заказа не найден", "Red");
                    return;
                }

                // Создаем заказ
                var order = new Order
                {
                    ClientID = client.ID,
                    Description = _description,
                    StatusID = newStatus.ID,
                    Cost = _totalCost,
                    Prepayment = null,
                    DateOfFitting = null,
                    DateOfReadiness = null,
                    DateOfIssue = null
                };

                _context.Orders.Add(order);
                _context.SaveChanges(); // Сохраняем, чтобы получить ID заказа

                // Добавляем услуги в заказ
                foreach (var item in _selectedServices)
                {
                    _context.OrderServices.Add(new OrderService
                    {
                        OrderID = order.ID,
                        ServiceID = item.ServiceId
                    });
                }

                _context.SaveChanges();

                ShowMessage("Заказ успешно создан!", "Green");

                // Очищаем форму
                ClearForm();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка при создании заказа: {ex.Message}", "Red");
            }
        }

        private void ClearForm()
        {
            _selectedServices.Clear();
            DescriptionTextBox.Text = string.Empty;
            _description = string.Empty;
            TotalCostTextBlock.Text = "0 ₽";
        }

        private void ShowMessage(string message, string color)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = color == "Red" ?
                System.Windows.Media.Brushes.Red :
                System.Windows.Media.Brushes.Green;
            StatusTextBlock.Visibility = string.IsNullOrEmpty(message) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        // Обработчики изменений текста
        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _description = ((TextBox)sender).Text;
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(((TextBox)sender).Text, out int result))
            {
                _quantity = result;
            }
        }

        private void ServicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedService = (Service)((ComboBox)sender).SelectedItem;
        }
    }

    // Класс для выбранной услуги
    public class SelectedServiceItem
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}