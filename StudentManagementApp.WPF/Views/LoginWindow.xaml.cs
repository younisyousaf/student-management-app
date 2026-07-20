using System;
using System.Net.Http.Json;
using System.Windows;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiClient _apiClient;

        public LoginWindow()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LblError.Visibility = Visibility.Collapsed;
            BtnLogin.IsEnabled = false;

            var loginDto = new LoginRequest
            {
                Username = TxtUsername.Text.Trim(),
                Password = TxtPassword.Password
            };

            try
            {
                var response = await _apiClient.LoginAsync(loginDto);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponseSuccess>();

                    if (result != null)
                    {
                        AuthService.Instance.JwtToken = result.Token;

                        // Open the main dashboard and close this window
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                }
                else
                {
                    LblError.Text = "Invalid username or password.";
                    LblError.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                LblError.Text = "Could not connect to backend server.";
                LblError.Visibility = Visibility.Visible;
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }

        private void LnkRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWin = new RegisterWindow();
            registerWin.Show();
            this.Close();
        }

        private class LoginResponseSuccess
        {
            public string Token { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}