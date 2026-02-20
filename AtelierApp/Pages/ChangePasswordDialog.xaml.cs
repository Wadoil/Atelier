using AtelierApp.Data;
using AtelierApp.Models;
using System;
using System.Linq;
using System.Windows;

namespace AtelierApp.Pages
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly AppDbContext _context;
        private readonly User _user;

        internal ChangePasswordDialog(User user)
        {
            InitializeComponent();
            _user = user;
            _context = new AppDbContext();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPassword = CurrentPasswordBox.Password;
                string newPassword = NewPasswordBox.Password;
                string confirmPassword = ConfirmPasswordBox.Password;

                // Валидация
                if (string.IsNullOrEmpty(currentPassword) ||
                    string.IsNullOrEmpty(newPassword) ||
                    string.IsNullOrEmpty(confirmPassword))
                {
                    ShowError("Заполните все поля");
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    ShowError("Новый пароль и подтверждение не совпадают");
                    return;
                }

                if (newPassword.Length < 6)
                {
                    ShowError("Пароль должен содержать минимум 6 символов");
                    return;
                }

                // Проверяем текущий пароль
                var auth = _context.Authorizations
                    .FirstOrDefault(a => a.ID == _user.AuthorisationID);

                if (auth == null)
                {
                    ShowError("Ошибка авторизации");
                    return;
                }

                // В реальном приложении здесь должно быть сравнение хешей
                if (auth.Password != currentPassword)
                {
                    ShowError("Неверный текущий пароль");
                    return;
                }

                // Сохраняем новый пароль
                auth.Password = newPassword;
                _context.Authorizations.Update(auth);
                _context.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}