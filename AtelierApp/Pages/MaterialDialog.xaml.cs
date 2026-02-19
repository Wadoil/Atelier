using AtelierApp.Data;
using AtelierApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using MaterialModel = AtelierApp.Models.Materials; // Псевдоним для избежания конфликта

namespace AtelierApp.Pages
{
    public partial class MaterialDialog : Window
    {
        private readonly AppDbContext _context;
        private readonly MaterialModel _material;
        private readonly bool _isEditMode;

        internal MaterialDialog(AppDbContext context, MaterialModel material = null)
        {
            InitializeComponent();
            _context = context;
            _material = material ?? new MaterialModel();
            _isEditMode = material != null;

            TitleTextBlock.Text = _isEditMode ? "Редактирование материала" : "Добавление материала";

            LoadComboBoxes();
            LoadMaterialData();
        }

        private void LoadComboBoxes()
        {
            // Загрузка категорий
            var categories = _context.MaterialCategories
                .OrderBy(c => c.Name)
                .ToList();
            CategoryComboBox.ItemsSource = categories;
            CategoryComboBox.DisplayMemberPath = "Name";
            CategoryComboBox.SelectedValuePath = "ID";

            // Загрузка единиц измерения
            var units = _context.Measurements
                .OrderBy(u => u.Name)
                .ToList();
            UnitComboBox.ItemsSource = units;
            UnitComboBox.DisplayMemberPath = "Name";
            UnitComboBox.SelectedValuePath = "ID";

            // Загрузка поставщиков
            var suppliers = _context.Suppliers
                .OrderBy(s => s.CompanyName)
                .ToList();
            SupplierComboBox.ItemsSource = suppliers;
            SupplierComboBox.DisplayMemberPath = "CompanyName";
            SupplierComboBox.SelectedValuePath = "ID";
        }

        private void LoadMaterialData()
        {
            if (_isEditMode)
            {
                ArticleTextBox.Text = _material.Article;
                NameTextBox.Text = _material.Name;
                CategoryComboBox.SelectedValue = _material.CategoryID;
                ColorTextBox.Text = _material.Color;
                UnitComboBox.SelectedValue = _material.UnitOfMeasurementID;
                CurrentAmountTextBox.Text = _material.CurrentAmount.ToString();
                MinimalAmountTextBox.Text = _material.MinimalAmountForReplenishment.ToString();
                PriceTextBox.Text = _material.Price.ToString();
                SupplierComboBox.SelectedValue = _material.SupplierID;
            }
            else
            {
                // Значения по умолчанию для нового материала
                CurrentAmountTextBox.Text = "0";
                MinimalAmountTextBox.Text = "0";
                PriceTextBox.Text = "0";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInputs())
                    return;

                // Заполняем данные материала
                _material.Article = ArticleTextBox.Text.Trim();
                _material.Name = NameTextBox.Text.Trim();
                _material.CategoryID = (int)CategoryComboBox.SelectedValue;
                _material.Color = string.IsNullOrWhiteSpace(ColorTextBox.Text) ? null : ColorTextBox.Text.Trim();
                _material.UnitOfMeasurementID = (int)UnitComboBox.SelectedValue;
                _material.CurrentAmount = decimal.Parse(CurrentAmountTextBox.Text);
                _material.MinimalAmountForReplenishment = decimal.Parse(MinimalAmountTextBox.Text);
                _material.Price = decimal.Parse(PriceTextBox.Text);
                _material.SupplierID = (int)SupplierComboBox.SelectedValue;

                if (_isEditMode)
                {
                    _context.Materials.Update(_material);
                }
                else
                {
                    // Проверка уникальности артикула
                    if (_context.Materials.Any(m => m.Article == _material.Article))
                    {
                        ShowError("Материал с таким артикулом уже существует");
                        return;
                    }

                    _context.Materials.Add(_material);
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка сохранения: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            // Проверка заполнения обязательных полей
            if (string.IsNullOrWhiteSpace(ArticleTextBox.Text))
            {
                ShowError("Введите артикул");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowError("Введите наименование");
                return false;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                ShowError("Выберите категорию");
                return false;
            }

            if (UnitComboBox.SelectedItem == null)
            {
                ShowError("Выберите единицу измерения");
                return false;
            }

            if (SupplierComboBox.SelectedItem == null)
            {
                ShowError("Выберите поставщика");
                return false;
            }

            // Проверка числовых полей
            if (!decimal.TryParse(CurrentAmountTextBox.Text, out decimal currentAmount) || currentAmount < 0)
            {
                ShowError("Введите корректное количество (положительное число)");
                return false;
            }

            if (!decimal.TryParse(MinimalAmountTextBox.Text, out decimal minimalAmount) || minimalAmount < 0)
            {
                ShowError("Введите корректный минимальный запас (положительное число)");
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price < 0)
            {
                ShowError("Введите корректную цену (положительное число)");
                return false;
            }

            return true;
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