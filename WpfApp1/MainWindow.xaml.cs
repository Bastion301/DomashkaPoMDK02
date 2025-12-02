using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private PartnerOrdersEntities1 partnerOrders;

        public MainWindow()
        {
            InitializeComponent();
            partnerOrders = new PartnerOrdersEntities1();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPartnerRequests();
        }

        private void LoadPartnerRequests()
        {
            try
            {
                // Альтернативный способ - загружаем данные отдельно
                var partnerRequests = partnerOrders.PartnerRequests
                    .OrderByDescending(r => r.RequestDate)
                    .ToList();

                var requestViewModels = new List<PartnerRequestViewModel>();

                foreach (var request in partnerRequests)
                {
                    // Явно загружаем связанные данные
                    partnerOrders.Entry(request).Reference(r => r.Partners).Load();
                    partnerOrders.Entry(request).Collection(r => r.RequestItems).Load();

                    var partner = request.Partners;
                    var itemsCount = request.RequestItems?.Count ?? 0;
                    var totalAmount = request.RequestItems?.Sum(ri => ri.TotalPrice) ?? 0;

                    var viewModel = new PartnerRequestViewModel
                    {
                        RequestID = request.RequestID,
                        PartnerInfo = $"{partner?.PartnerType ?? "Неизвестно"} | {partner?.PartnerName ?? "Неизвестный партнер"}",
                        DirectorName = partner?.DirectorName ?? "Не указано",
                        LegalAddress = partner?.LegalAddress ?? "Адрес не указан",
                        Phone = partner?.Phone ?? "Телефон не указан",
                        Email = partner?.Email ?? "Email не указан",
                        Rating = partner?.Rating ?? 0,
                        TotalAmount = totalAmount,
                        RequestDate = request.RequestDate ?? DateTime.Now,
                        Status = request.Status ?? "Новая",
                        ItemsCount = itemsCount
                    };

                    requestViewModels.Add(viewModel);
                }

                PartnerRequestsItemsControl.ItemsSource = requestViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBDCFA"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C4882"));
            }
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0C4882"));
            }
        }

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is Border border && border.Tag != null &&
                int.TryParse(border.Tag.ToString(), out int requestId))
            {
                // Открываем форму редактирования при двойном клике
                var editWindow = new EditRequestWindow(requestId);
                editWindow.Owner = this;

                if (editWindow.ShowDialog() == true)
                {
                    // Обновляем данные после сохранения изменений
                    LoadPartnerRequests();
                }
            }
        }

        private void AddRequestButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем форму добавления новой заявки
            var addWindow = new EditRequestWindow();
            addWindow.Owner = this;

            if (addWindow.ShowDialog() == true)
            {
                // Обновляем данные после добавления новой заявки
                LoadPartnerRequests();
            }
        }
    }

    // Модель представления для отображения заявок
    public class PartnerRequestViewModel
    {
        public int RequestID { get; set; }
        public string PartnerInfo { get; set; }
        public string DirectorName { get; set; }
        public string LegalAddress { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int Rating { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public int ItemsCount { get; set; }
    }
}