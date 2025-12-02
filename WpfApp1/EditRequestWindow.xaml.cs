using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class EditRequestWindow : Window
    {
        private PartnerOrdersEntities1 partnerOrders;
        private int currentRequestId;
        private bool isEditMode;
        private ObservableCollection<ProductItemViewModel> productItems;
        private List<Partners> allPartners;

        public EditRequestWindow() : this(0) { }

        public EditRequestWindow(int requestId)
        {
            InitializeComponent();
            partnerOrders = new PartnerOrdersEntities1();
            currentRequestId = requestId;
            isEditMode = requestId > 0;
            productItems = new ObservableCollection<ProductItemViewModel>();

            InitializeWindow();
            LoadData();
        }

        private void InitializeWindow()
        {
            if (isEditMode)
            {
                WindowTitle.Text = "Редактирование заявки";
                SaveButton.Content = "Сохранить изменения";
            }
            else
            {
                WindowTitle.Text = "Добавление заявки";
                SaveButton.Content = "Создать заявку";
            }

            // Загружаем всех партнеров для комбобокса
            allPartners = partnerOrders.Partners.ToList();
            PartnerComboBox.ItemsSource = allPartners;

            // Заполняем комбобоксы
            PartnerTypeComboBox.ItemsSource = new List<string> { "ООО", "ЗАО", "ОАО", "ПАО", "ИП" };
            StatusComboBox.ItemsSource = new List<string>
            {
                "Новая",
                "Ожидает предоплаты",
                "В производстве",
                "Готово к отгрузке",
                "Выполнена",
                "Отменена"
            };

            // Настраиваем DataGrid
            ProductsDataGrid.ItemsSource = productItems;

            // Загружаем список продукции для комбобокса
            var products = partnerOrders.Products.ToList();
            ProductComboBoxColumn.ItemsSource = products;

            // Подписываемся на события изменения данных
            productItems.CollectionChanged += (s, e) => UpdateTotalAmount();
        }

        private void LoadData()
        {
            try
            {
                if (isEditMode)
                {
                    // Режим редактирования - загружаем существующую заявку
                    var request = partnerOrders.PartnerRequests
                        .FirstOrDefault(r => r.RequestID == currentRequestId);

                    if (request != null)
                    {
                        var partner = partnerOrders.Partners
                            .FirstOrDefault(p => p.PartnerID == request.PartnerID);

                        if (partner != null)
                        {
                            // Выбираем партнера в комбобоксе
                            PartnerComboBox.SelectedItem = allPartners.FirstOrDefault(p => p.PartnerID == partner.PartnerID);

                            // Остальные поля заполняются автоматически через PartnerComboBox_SelectionChanged
                            StatusComboBox.SelectedItem = request.Status ?? "Новая";
                        }

                        // Загружаем продукцию в заявке
                        var requestItems = partnerOrders.RequestItems
                            .Where(ri => ri.RequestID == currentRequestId)
                            .ToList();

                        foreach (var item in requestItems)
                        {
                            productItems.Add(new ProductItemViewModel
                            {
                                RequestItemID = item.RequestItemID,
                                ProductID = item.ProductID,
                                ProductName = item.Products.ProductName,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                TotalPrice = item.TotalPrice
                            });
                        }

                        UpdateTotalAmount();
                    }
                }
                else
                {
                    // Режим добавления - устанавливаем значения по умолчанию
                    StatusComboBox.SelectedItem = "Новая";
                    UpdateTotalAmount();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PartnerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PartnerComboBox.SelectedItem is Partners selectedPartner)
            {
                // Автоматически заполняем все поля данными выбранного партнера
                PartnerTypeComboBox.SelectedItem = selectedPartner.PartnerType;
                DirectorNameTextBox.Text = selectedPartner.DirectorName;
                AddressTextBox.Text = selectedPartner.LegalAddress;
                RatingTextBox.Text = selectedPartner.Rating.ToString();
                PhoneTextBox.Text = selectedPartner.Phone;
                EmailTextBox.Text = selectedPartner.Email;
            }
            else
            {
                // Очищаем поля, если партнер не выбран
                ClearPartnerFields();
            }
        }

        private void ClearPartnerFields()
        {
            PartnerTypeComboBox.SelectedItem = null;
            DirectorNameTextBox.Text = string.Empty;
            AddressTextBox.Text = string.Empty;
            RatingTextBox.Text = string.Empty;
            PhoneTextBox.Text = string.Empty;
            EmailTextBox.Text = string.Empty;
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            // Добавляем новую пустую строку для продукции
            productItems.Add(new ProductItemViewModel
            {
                Quantity = 1,
                UnitPrice = 0,
                TotalPrice = 0
            });
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductItemViewModel item)
            {
                productItems.Remove(item);
            }
        }

        private void ProductsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Row.Item is ProductItemViewModel item)
            {
                // Обновляем данные после редактирования ячейки
                UpdateProductItem(item);
            }
        }

        private void UpdateProductItem(ProductItemViewModel item)
        {
            if (item.ProductID > 0)
            {
                var product = partnerOrders.Products.FirstOrDefault(p => p.ProductID == item.ProductID);
                if (product != null)
                {
                    item.UnitPrice = product.MinPartnerPrice;
                    item.ProductName = product.ProductName;
                }
            }

            // Пересчитываем сумму
            item.TotalPrice = item.Quantity * item.UnitPrice;
            UpdateTotalAmount();
        }

        private void UpdateTotalAmount()
        {
            decimal total = productItems.Sum(item => item.TotalPrice);
            TotalAmountText.Text = $"₽ {total:N2}";
        }

        private bool ValidateInput()
        {
            // Проверка выбора партнера
            if (PartnerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать партнера.", "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PartnerComboBox.Focus();
                return false;
            }

            // Проверка продукции
            foreach (var item in productItems)
            {
                if (item.ProductID == 0)
                {
                    MessageBox.Show("Для всех позиций должна быть выбрана продукция.", "Ошибка валидации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (item.Quantity <= 0)
                {
                    MessageBox.Show("Количество продукции должно быть больше 0.", "Ошибка валидации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                // Обновляем все позиции перед сохранением
                foreach (var item in productItems.ToList())
                {
                    UpdateProductItem(item);
                }

                if (isEditMode)
                {
                    UpdateExistingRequest();
                }
                else
                {
                    CreateNewRequest();
                }

                partnerOrders.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateExistingRequest()
        {
            var request = partnerOrders.PartnerRequests
                .FirstOrDefault(r => r.RequestID == currentRequestId);

            if (request != null && PartnerComboBox.SelectedItem is Partners selectedPartner)
            {
                // Обновляем статус заявки
                request.Status = StatusComboBox.SelectedItem?.ToString();

                // Обновляем продукцию в заявке
                UpdateRequestItems(request.RequestID);
            }
        }

        private void CreateNewRequest()
        {
            if (PartnerComboBox.SelectedItem is Partners selectedPartner)
            {
                // Создаем новую заявку для выбранного партнера
                var newRequest = new PartnerRequests
                {
                    PartnerID = selectedPartner.PartnerID,
                    RequestDate = DateTime.Now,
                    Status = StatusComboBox.SelectedItem?.ToString(),
                    TotalAmount = productItems.Sum(item => item.TotalPrice)
                };

                partnerOrders.PartnerRequests.Add(newRequest);
                partnerOrders.SaveChanges();

                // Добавляем продукцию в заявку
                UpdateRequestItems(newRequest.RequestID);
            }
        }

        private void UpdateRequestItems(int requestId)
        {
            try
            {
                // Удаляем старые позиции
                var existingItems = partnerOrders.RequestItems
                    .Where(ri => ri.RequestID == requestId)
                    .ToList();

                foreach (var item in existingItems)
                {
                    partnerOrders.RequestItems.Remove(item);
                }

                partnerOrders.SaveChanges();

                // Добавляем новые позиции
                foreach (var item in productItems)
                {
                    var requestItem = new RequestItems
                    {
                        RequestID = requestId,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    };
                    partnerOrders.RequestItems.Add(requestItem);
                }

                // Обновляем общую сумму заявки
                var request = partnerOrders.PartnerRequests.FirstOrDefault(r => r.RequestID == requestId);
                if (request != null)
                {
                    request.TotalAmount = productItems.Sum(item => item.TotalPrice);
                }

                partnerOrders.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении позиций заявки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SuggestedProductsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PartnerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите партнера", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedPartner = (Partners)PartnerComboBox.SelectedItem;
            var suggestedProductsWindow = new SuggestedProductsWindow(selectedPartner.PartnerID);
            suggestedProductsWindow.Owner = this;

            if (suggestedProductsWindow.ShowDialog() == true && suggestedProductsWindow.Tag is ProductSelectionData selectionData)
            {
                // Добавляем выбранную продукцию в заявку с учетом количества
                var existingItem = productItems.FirstOrDefault(pi => pi.ProductID == selectionData.Product.ProductID);

                if (existingItem != null)
                {
                    // Если продукция уже есть в заявке - увеличиваем количество
                    existingItem.Quantity += selectionData.Quantity;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    // Добавляем новую позицию
                    productItems.Add(new ProductItemViewModel
                    {
                        ProductID = selectionData.Product.ProductID,
                        ProductName = selectionData.Product.ProductName,
                        Quantity = selectionData.Quantity,
                        UnitPrice = selectionData.Product.MinPartnerPrice,
                        TotalPrice = selectionData.Quantity * selectionData.Product.MinPartnerPrice
                    });
                }

                UpdateTotalAmount();

                // Показываем информацию о добавленной продукции
                MessageBox.Show($"Добавлено в заявку:\n{selectionData.Product.ProductName}\nКоличество: {selectionData.Quantity} шт.\nСумма: {selectionData.Quantity * selectionData.Product.MinPartnerPrice:N2}₽",
                    "Продукция добавлена",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    // Класс ProductItemViewModel остается без изменений
    public class ProductItemViewModel : INotifyPropertyChanged
    {
        private int productID;
        private string productName;
        private int quantity;
        private decimal unitPrice;
        private decimal totalPrice;

        public int RequestItemID { get; set; }

        public int ProductID
        {
            get => productID;
            set
            {
                productID = value;
                OnPropertyChanged(nameof(ProductID));
            }
        }

        public string ProductName
        {
            get => productName;
            set
            {
                productName = value;
                OnPropertyChanged(nameof(ProductName));
            }
        }

        public int Quantity
        {
            get => quantity;
            set
            {
                quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public decimal UnitPrice
        {
            get => unitPrice;
            set
            {
                unitPrice = value;
                OnPropertyChanged(nameof(UnitPrice));
            }
        }

        public decimal TotalPrice
        {
            get => totalPrice;
            set
            {
                totalPrice = value;
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}