using AtelierApp.Data;
using AtelierApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AtelierApp.Pages
{
    /// <summary>
    /// Логика взаимодействия для Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        public Registration()
        {
            InitializeComponent();
        }
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string surname = SurnameTextBox.Text.Trim();
                string name = NameTextBox.Text.Trim();
                string middleName = MiddleNameTextBox.Text?.Trim();
                string phone = PhoneTextBox.Text?.Trim();
                string email = EmailTextBox.Text?.Trim();
                string address = AddressTextBox.Text.Trim();
                string login = LoginTextBox.Text.Trim();
                string password = PasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                if (string.IsNullOrEmpty(surname) || string.IsNullOrEmpty(name) ||
                    string.IsNullOrEmpty(address) || string.IsNullOrEmpty(login) ||
                    string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                {
                    ShowError("Заполните все обязательные поля");
                    return;
                }

                if (password != confirmPassword)
                {
                    ShowError("Пароли не совпадают");
                    return;
                }

                if (password.Length < 6)
                {
                    ShowError("Пароль должен содержать минимум 6 символов");
                    return;
                }

                using (var db = new AppDbContext())
                {
                    if (db.Authorizations.Any(a => a.Login == login))
                    {
                        ShowError("Пользователь с таким логином уже существует");
                        return;
                    }

                    // Создание авторизации
                    var auth = new Authorizations
                    {
                        Login = login,
                        Password = password
                    };
                    db.Authorizations.Add(auth);
                    db.SaveChanges();

                    // Создание пользователя
                    var user = new User
                    {
                        Surname = surname,
                        Name = name,
                        MiddleName = middleName,
                        PhoneNumber = phone,
                        Email = email,
                        Role = "client",
                        RegisterDate = DateTime.Now,
                        AuthorisationID = auth.ID
                    };
                    db.Users.Add(user);
                    db.SaveChanges();

                    //Создание клиента
                    var client = new Client
                    {
                        Address = address,
                        UserID = user.ID
                    };
                    db.Clients.Add(client);
                    db.SaveChanges();

                    MessageBox.Show("Регистрация прошла успешно!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Переход в личный кабинет
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.SetCurrentUser(user);
                        NavigationService.Navigate(new Account());
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при регистрации: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
