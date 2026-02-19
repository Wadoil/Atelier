using AtelierApp.Data;
using AtelierApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialModel = AtelierApp.Models.Materials;

namespace AtelierApp.Pages
{
    public partial class AdminMaterials : Page
    {
        private readonly AppDbContext _context;
        private ObservableCollection<MaterialViewModel> _allMaterials;
        private ObservableCollection<MaterialViewModel> _filteredMaterials;
        private ObservableCollection<CategoryItem> _categories;

        public AdminMaterials()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _allMaterials = new ObservableCollection<MaterialViewModel>();
            _filteredMaterials = new ObservableCollection<MaterialViewModel>();
            _categories = new ObservableCollection<CategoryItem>();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загрузка категорий для фильтра
                LoadCategories();

                // Загрузка материалов
                LoadMaterials();

                StatusTextBlock.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка загрузки данных: {ex.Message}", "Red");
            }
        }

        private void LoadCategories()
        {
            var categories = _context.MaterialCategories
                .OrderBy(c => c.Name)
                .ToList();

            _categories.Clear();
            _categories.Add(new CategoryItem { Id = 0, Name = "Все категории" });

            foreach (var category in categories)
            {
                _categories.Add(new CategoryItem
                {
                    Id = category.ID,
                    Name = category.Name
                });
            }

            CategoryFilterComboBox.ItemsSource = _categories;
            CategoryFilterComboBox.DisplayMemberPath = "Name";
            CategoryFilterComboBox.SelectedValuePath = "Id";
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void LoadMaterials()
        {
            var materials = _context.Materials
                .Include(m => m.Category)
                .Include(m => m.UnitOfMeasurement)
                .Include(m => m.Supplier)
                .OrderBy(m => m.Name)
                .ToList();

            _allMaterials.Clear();
            foreach (var material in materials)
            {
                _allMaterials.Add(new MaterialViewModel
                {
                    ID = material.ID,
                    Article = material.Article,
                    Name = material.Name,
                    CategoryId = material.CategoryID,
                    CategoryName = material.Category?.Name ?? "Без категории",
                    Color = material.Color,
                    UnitId = material.UnitOfMeasurementID,
                    UnitName = material.UnitOfMeasurement?.Name ?? "",
                    CurrentAmount = material.CurrentAmount,
                    MinimalAmountForReplenishment = material.MinimalAmountForReplenishment,
                    Price = material.Price,
                    SupplierId = material.SupplierID,
                    SupplierName = material.Supplier?.CompanyName ?? "Не указан",
                    IsLowStock = material.CurrentAmount < material.MinimalAmountForReplenishment
                });
            }

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string searchText = SearchTextBox.Text?.ToLower() ?? "";
            var selectedCategory = CategoryFilterComboBox.SelectedItem as CategoryItem;

            var filtered = _allMaterials.Where(m =>
                (string.IsNullOrEmpty(searchText) ||
                 m.Name.ToLower().Contains(searchText) ||
                 m.Article.ToLower().Contains(searchText)) &&
                (selectedCategory == null || selectedCategory.Id == 0 || m.CategoryId == selectedCategory.Id)
            ).ToList();

            _filteredMaterials.Clear();
            foreach (var material in filtered)
            {
                _filteredMaterials.Add(material);
            }

            MaterialsListView.ItemsSource = _filteredMaterials;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MaterialDialog(_context, null);
            if (dialog.ShowDialog() == true)
            {
                LoadMaterials();
                ShowMessage("Материал успешно добавлен", "Green");
            }
        }

        private void EditMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int materialId)
            {
                var material = _context.Materials.Find(materialId);
                if (material != null)
                {
                    var dialog = new MaterialDialog(_context, material);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadMaterials();
                        ShowMessage("Материал успешно обновлен", "Green");
                    }
                }
            }
        }

        private void MaterialsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MaterialsListView.SelectedItem is MaterialViewModel selected)
            {
                var material = _context.Materials.Find(selected.ID);
                if (material != null)
                {
                    var dialog = new MaterialDialog(_context, material);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadMaterials();
                        ShowMessage("Материал успешно обновлен", "Green");
                    }
                }
            }
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int materialId)
            {
                var result = MessageBox.Show(
                    "Вы уверены, что хотите удалить этот материал?\n\n" +
                    "Внимание: материал может использоваться в заказах!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var material = _context.Materials.Find(materialId);
                        if (material != null)
                        {
                            // Проверяем, используется ли материал в заказах
                            bool isUsed = _context.UsedMaterials.Any(um => um.MaterialID == materialId);

                            if (isUsed)
                            {
                                MessageBox.Show(
                                    "Невозможно удалить материал, так как он используется в заказах.\n\n" +
                                    "Вы можете отредактировать его или пометить как неактивный (добавить статус).",
                                    "Ошибка удаления",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }

                            _context.Materials.Remove(material);
                            _context.SaveChanges();

                            LoadMaterials();
                            ShowMessage("Материал успешно удален", "Green");
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowMessage($"Ошибка при удалении: {ex.Message}", "Red");
                    }
                }
            }
        }

        private void OrderFromSupplier_Click(object sender, RoutedEventArgs e)
        {
            // Показываем материалы с низким запасом
            var lowStockMaterials = _allMaterials.Where(m => m.IsLowStock).ToList();

            if (!lowStockMaterials.Any())
            {
                MessageBox.Show(
                    "Нет материалов с низким запасом.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            string message = "Материалы для заказа:\n\n";
            foreach (var material in lowStockMaterials)
            {
                message += $"- {material.Name} (арт. {material.Article}): " +
                          $"текущий запас {material.CurrentAmount} {material.UnitName}, " +
                          $"минимальный запас {material.MinimalAmountForReplenishment}\n";
            }

            MessageBox.Show(message, "Рекомендации по закупке", MessageBoxButton.OK, MessageBoxImage.Information);
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

    // ViewModel для отображения материала
    public class MaterialViewModel
    {
        public int ID { get; set; }
        public string Article { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Color { get; set; }
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal MinimalAmountForReplenishment { get; set; }
        public decimal Price { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public bool IsLowStock { get; set; }
    }

    // Класс для категории в фильтре
    public class CategoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}