using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class CoursesView : UserControl
    {
        private readonly ApiClient _apiClient;
        private List<CourseModel> _cachedCourses = [];

        public CoursesView()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            Loaded += async (s, e) => await LoadCoursesAsync();
        }

        private async Task LoadCoursesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<ApiResponse<List<CourseModel>>>("courses");
                _cachedCourses = response?.Data ?? [];

                if (_cachedCourses.Count == 0)
                {
                    DgCourses.Visibility = Visibility.Collapsed;
                    PanelEmptyCourses.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelEmptyCourses.Visibility = Visibility.Collapsed;
                    DgCourses.Visibility = Visibility.Visible;
                    DgCourses.ItemsSource = _cachedCourses;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to query curriculum entries: {ex.Message}", "Network Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnOpenCourseForm_Click(object sender, RoutedEventArgs e)
        {
            var formWindow = new CourseFormWindow(_cachedCourses, null)
            {
                Owner = Window.GetWindow(this)
            };

            formWindow.ShowDialog();

            if (formWindow.IsDataSaved)
            {
                await LoadCoursesAsync();
            }
        }

        private async void BtnEditCourse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CourseModel selectedCourse)
            {
                var formWindow = new CourseFormWindow(_cachedCourses, selectedCourse)
                {
                    Owner = Window.GetWindow(this)
                };

                formWindow.ShowDialog();

                if (formWindow.IsDataSaved)
                {
                    await LoadCoursesAsync();
                }
            }
        }

        private async void BtnDeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CourseModel selectedCourse)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to permanently delete course track: {selectedCourse.Name} ({selectedCourse.Code})?\nThis action cannot be undone.",
                    "Confirm Destructive Deletion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _apiClient.DeleteAsync($"courses/{selectedCourse.Id}");
                        if (response.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Course track successfully cleared from system logs.", "Clear Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadCoursesAsync();
                        }
                        else
                        {
                            MessageBox.Show("Server rejected erasure. Verify no student registrations remain bound to this course track.", "Deletion Blocked", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Network processing disconnect error: {ex.Message}", "Sync Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}