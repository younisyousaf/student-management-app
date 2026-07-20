using System;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly ApiClient _apiClient;

        public RegisterWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            LblStatus.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TxtUsername.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                ShowMessage("All fields are mandatory.", Brushes.Red);
                return;
            }

            BtnRegister.IsEnabled = false;

            var registration = new RegisterRequest
            {
                Username = TxtUsername.Text.Trim(),
                Email = TxtEmail.Text.Trim(),
                Password = TxtPassword.Password,
                Role = "Admin"
            };

            try
            {
                var response = await _apiClient.RegisterAsync(registration);

                if (response.IsSuccessStatusCode)
                {
                    ShowMessage("Account registered successfully!", Brushes.Green);
                    await System.Threading.Tasks.Task.Delay(1500);
                    NavigateToLogin();
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ShowMessage(errorResult?.Message ?? "Registration failed. Try a different username/email.", Brushes.Red);
                }
            }
            catch (Exception)
            {
                ShowMessage("Could not communicate with the authentication server.", Brushes.Red);
            }
            finally
            {
                BtnRegister.IsEnabled = true;
            }
        }

        private void LnkLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigateToLogin();
        }

        private void NavigateToLogin()
        {
            var loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        private void ShowMessage(string message, Brush color)
        {
            LblStatus.Text = message;
            LblStatus.Foreground = color;
            LblStatus.Visibility = Visibility.Visible;
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}