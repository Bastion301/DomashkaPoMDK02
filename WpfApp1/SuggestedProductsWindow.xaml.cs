using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class SuggestedProductsWindow : Window
    {
        private PartnerOrdersEntities1 partnerOrders;
        private int partnerId;
        private Products selectedProduct;

        public SuggestedProductsWindow(int partnerId)
        {
            InitializeComponent();
            partnerOrders = new PartnerOrdersEntities1();
            this.partnerId = partnerId;

            LoadSuggestedProducts();
        }

        private void LoadSuggestedProducts()
        {
            try
            {
                // Получаем информацию о партнере
                var partner = partnerOrders.Partners.FirstOrDefault(p => p.PartnerID == partnerId);
                if (partner != null)
                {
                    WindowTitle.Text = $"Предлагаемая продукция для {partner.PartnerName}";
                }

                // Получаем всю продукцию с информацией о складе
                var suggestedProducts = new List<SuggestedProductViewModel>();

                foreach (var product in partnerOrders.Products.ToList())
                {
                    var stock = partnerOrders.ProductStock
                        .FirstOrDefault(ps => ps.ProductID == product.ProductID);

                    suggestedProducts.Add(new SuggestedProductViewModel
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        ProductTypeName = product.ProductTypes?.ProductTypeName ?? "Неизвестно",
                        ArticleNumber = product.ArticleNumber,
                        MinPartnerPrice = product.MinPartnerPrice,
                        StockQuantity = stock?.CurrentStock ?? 0
                    });
                }

                ProductsDataGrid.ItemsSource = suggestedProducts
                    .OrderBy(p => p.ProductTypeName)
                    .ThenBy(p => p.ProductName)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке продукции: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is SuggestedProductViewModel selected)
            {
                selectedProduct = partnerOrders.Products.FirstOrDefault(p => p.ProductID == selected.ProductID);
                CalculateButton.IsEnabled = selectedProduct != null;
                AddToRequestButton.IsEnabled = selectedProduct != null;
            }
            else
            {
                selectedProduct = null;
                CalculateButton.IsEnabled = false;
                AddToRequestButton.IsEnabled = false;
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите продукцию для расчета", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество продукции", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            if (!double.TryParse(Param1TextBox.Text, out double param1) || param1 <= 0)
            {
                MessageBox.Show("Введите корректное значение параметра 1", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Param1TextBox.Focus();
                return;
            }

            if (!double.TryParse(Param2TextBox.Text, out double param2) || param2 <= 0)
            {
                MessageBox.Show("Введите корректное значение параметра 2", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Param2TextBox.Focus();
                return;
            }

            try
            {
                CalculateMaterials(selectedProduct, quantity, param1, param2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateMaterials(Products product, int quantity, double param1, double param2)
        {
            var results = new List<string>();

            // Получаем материалы, необходимые для этой продукции
            var productMaterials = partnerOrders.ProductMaterials
                .Where(pm => pm.ProductID == product.ProductID)
                .ToList();

            if (!productMaterials.Any())
            {
                results.Add("Для данной продукции не указаны материалы");
                MaterialsResultsItemsControl.ItemsSource = results;
                return;
            }

            // Получаем количество на складе
            var stock = partnerOrders.ProductStock
                .FirstOrDefault(ps => ps.ProductID == product.ProductID);
            int stockQuantity = stock?.CurrentStock ?? 0;

            foreach (var pm in productMaterials)
            {
                var material = pm.Materials;
                var materialType = material?.MaterialTypes;

                if (material == null || materialType == null)
                {
                    results.Add("Ошибка: материал не найден");
                    continue;
                }

                // Используем метод расчета материалов
                int requiredMaterial = MaterialCalculator.CalculateRequiredMaterial(
                    product.ProductTypeID,
                    material.MaterialTypeID,
                    quantity,
                    stockQuantity,
                    param1,
                    param2);

                if (requiredMaterial >= 0)
                {
                    results.Add($"{material.MaterialName}: {requiredMaterial} {material.UnitOfMeasure}");
                }
                else
                {
                    results.Add($"{material.MaterialName}: ошибка расчета");
                }
            }

            results.Insert(0, $"Продукция: {product.ProductName}");
            results.Insert(1, $"Требуется произвести: {Math.Max(0, quantity - stockQuantity)} шт.");
            results.Insert(2, "---");

            MaterialsResultsItemsControl.ItemsSource = results;
        }

        private void AddToRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите продукцию для добавления в заявку", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество продукции", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return;
            }

            if (!double.TryParse(Param1TextBox.Text, out double param1) || param1 <= 0)
            {
                MessageBox.Show("Введите корректное значение параметра 1", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Param1TextBox.Focus();
                return;
            }

            if (!double.TryParse(Param2TextBox.Text, out double param2) || param2 <= 0)
            {
                MessageBox.Show("Введите корректное значение параметра 2", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Param2TextBox.Focus();
                return;
            }

            // Создаем объект с данными для передачи
            var selectionData = new ProductSelectionData
            {
                Product = selectedProduct,
                Quantity = quantity,
                Parameter1 = param1,
                Parameter2 = param2,
                MaterialCalculations = MaterialsResultsItemsControl.ItemsSource as List<string> ?? new List<string>()
            };

            // Возвращаем данные в родительское окно
            this.Tag = selectionData;
            this.DialogResult = true;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    public class SuggestedProductViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductTypeName { get; set; }
        public string ArticleNumber { get; set; }
        public decimal MinPartnerPrice { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductSelectionData
    {
        public Products Product { get; set; }
        public int Quantity { get; set; }
        public double Parameter1 { get; set; }
        public double Parameter2 { get; set; }
        public List<string> MaterialCalculations { get; set; }
    }
}