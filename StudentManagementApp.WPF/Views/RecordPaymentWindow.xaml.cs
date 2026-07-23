using System;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class RecordPaymentWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly FeeVm _fee;

        public bool IsDataSaved { get; private set; } = false;

        public RecordPaymentWindow(FeeVm fee)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _fee = fee;

            TxtStudentName.Text = fee.StudentName;
            TxtCourseName.Text = fee.CourseCode;
            TxtAmountDue.Text = fee.AmountDue.ToString("N2");
            TxtAmountPaidSoFar.Text = fee.AmountPaid.ToString("N2");
            TxtRemainingBalance.Text = fee.RemainingBalance.ToString("N2");
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtAmountPaid.Text, out decimal amount) || amount <= 0)
            {
                ShowFeedback("Enter a valid payment amount.", Brushes.Orange);
                return;
            }

            if (amount > _fee.RemainingBalance)
            {
                ShowFeedback($"Amount exceeds the remaining balance of {_fee.RemainingBalance:N2} PKR.", Brushes.Orange);
                return;
            }

            var dto = new FeeDto
            {
                StudentId = _fee.StudentId,
                CourseId = _fee.CourseId,
                AmountPaid = amount,
                Remarks = TxtRemarks.Text.Trim()
            };

            try
            {
                var httpResponse = await _apiClient.PostAsync("fees/pay", dto);
                if (httpResponse.IsSuccessStatusCode)
                {
                    IsDataSaved = true;
                    MessageBox.Show("Payment recorded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Server rejected the payment.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network error: {ex.Message}", Brushes.Red);
            }
        }

        private void ShowFeedback(string text, Brush color)
        {
            LblStatus.Text = text;
            LblStatus.Foreground = color;
            LblStatus.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}