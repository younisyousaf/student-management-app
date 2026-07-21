using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class StudentsView : UserControl
    {
        private readonly ApiClient _apiClient;
        private List<StudentModel> _cachedStudents = [];

        public StudentsView()
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            Loaded += async (s, e) => await LoadStudentsAsync();
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<ApiResponse<List<StudentModel>>>("students");
                _cachedStudents = response?.Data ?? [];

                if (_cachedStudents.Count == 0)
                {
                    DgStudents.Visibility = Visibility.Collapsed;
                    PanelEmptyStudents.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelEmptyStudents.Visibility = Visibility.Collapsed;
                    DgStudents.Visibility = Visibility.Visible;

                    // Force WPF to clear internal structural caching and re-render fresh items
                    DgStudents.ItemsSource = null;
                    DgStudents.ItemsSource = _cachedStudents;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to sync student profiles: {ex.Message}", "API Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnOpenRegisterForm_Click(object sender, RoutedEventArgs e)
        {
            var formWindow = new StudentFormWindow(_cachedStudents)
            {
                Owner = Window.GetWindow(this)
            };

            // Shows modal workspace window
            formWindow.ShowDialog();

            // Re-sync UI state immediately following closure regardless of DialogResult flags
            await LoadStudentsAsync();
        }

        private async void BtnEditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StudentModel selectedStudent)
            {
                var formWindow = new StudentFormWindow(_cachedStudents, selectedStudent)
                {
                    Owner = Window.GetWindow(this)
                };

                formWindow.ShowDialog();

                // Re-sync UI state immediately following edit submission closure
                await LoadStudentsAsync();
            }
        }

        private async void BtnDeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StudentModel selectedStudent)
            {
                var confirmation = MessageBox.Show(
                    $"Are you sure you want to permanently remove {selectedStudent.FirstName} {selectedStudent.LastName} (Roll No: {selectedStudent.RollNumber})?",
                    "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirmation == MessageBoxResult.Yes)
                {
                    try
                    {
                        var httpResponse = await _apiClient.DeleteAsync($"students/{selectedStudent.Id}");
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            await LoadStudentsAsync();
                        }
                        else
                        {
                            MessageBox.Show("Server rejected profile removal request.", "Deletion Denied", MessageBoxButton.OK, MessageBoxImage.Stop);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Connection dropped: {ex.Message}", "Network Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}