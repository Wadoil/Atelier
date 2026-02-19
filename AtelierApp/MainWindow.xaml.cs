using AtelierApp.Models;
using AtelierApp.Pages;
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
using Authorization = AtelierApp.Pages.Authorization;

namespace AtelierApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private User _currentUser;
        public MainWindow()
        {
            InitializeComponent();
            FrmMain.Navigate(new Authorization());
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            FrmMain.GoBack();
        }

        private void FrmMain_ContentRendered(object sender, EventArgs e)
        {
            if (FrmMain.CanGoBack)
                btnBack.Visibility = Visibility.Visible;
            else
                btnBack.Visibility = Visibility.Hidden;
        }
        // Метод для установки пользователя после авторизации
        internal void SetCurrentUser(User user)
        {
            _currentUser = user;
            UpdateNavigationButtons();
        }

        // Обновление видимости кнопок навигации
        private void UpdateNavigationButtons()
        {
            if (_currentUser != null)
            {
                // Показываем общие кнопки для всех авторизованных
                NavServices.Visibility = Visibility.Visible;
                NavProfile.Visibility = Visibility.Visible;

                // Скрываем все кнопки по умолчанию
                NavAdminPanel.Visibility = Visibility.Collapsed;
                NavMasterWorkspace.Visibility = Visibility.Collapsed;
                NavSeamstressWorkspace.Visibility = Visibility.Collapsed;

                // Показываем кнопки в зависимости от роли
                switch (_currentUser.Role)
                {
                    case "admin":
                        NavAdminPanel.Visibility = Visibility.Visible;
                        break;
                    case "master":
                        NavMasterWorkspace.Visibility = Visibility.Visible;
                        break;
                    case "seamstress":
                        NavSeamstressWorkspace.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        // Обработчики навигационных кнопок
        private void NavServices_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                FrmMain.Navigate(new CreateOrder(_currentUser));
            }
        }

        private void NavProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null)
            {
                FrmMain.Navigate(new Account());
            }
        }

        private void NavAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null && _currentUser.Role == "admin")
            {
                FrmMain.Navigate(new AdminMaterials());
            }
        }

        private void NavMasterWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null && _currentUser.Role == "master")
            {
                FrmMain.Navigate(new MasterWorkspace(_currentUser));
            }
        }

        private void NavSeamstressWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser != null && _currentUser.Role == "seamstress")
            {
                FrmMain.Navigate(new SeamstressWorkspace(_currentUser));
            }
        }
    }
}
