using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class EnrollmentsView : UserControl
    {
        private readonly ApiClient _apiClient;

        public EnrollmentsView()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            Loaded += async (s, e) => await CompleteDataFetchInit();
        }

        private async Task CompleteDataFetchInit()
        {
            try
            {
                // Concurrent Pipeline Dispatching
                var studentRes = await _apiClient.GetAsync<ApiResponse<List<StudentModel>>>("students");
                var courseRes = await _apiClient.GetAsync<ApiResponse<List<CourseModel>>>("courses");
                var enrollmentRes = await _apiClient.GetAsync<ApiResponse<List<EnrollmentVm>>>("enrollments");

                var students = studentRes?.Data ?? [];
                var courses = courseRes?.Data ?? [];
                var enrollments = enrollmentRes?.Data ?? [];

                // Setup ComboBox ItemsSource using the anonymous projection
                CboStudents.ItemsSource = students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.FirstName} {s.LastName}".Trim()
                }).ToList();

                CboCourses.ItemsSource = courses;

                // Join the lists in memory to match IDs to Names
                foreach (var emp in enrollments)
                {
                    // Find the matching student details from the downloaded students collection
                    var matchedStudent = students.Find(s => s.Id == emp.StudentId);
                    if (matchedStudent != null)
                    {
                        emp.StudentName = $"{matchedStudent.FirstName} {matchedStudent.LastName}".Trim();
                    }

                    // Find the matching course details from the downloaded courses collection
                    var matchedCourse = courses.Find(c => c.Id == emp.CourseId);
                    if (matchedCourse != null)
                    {
                        emp.CourseName = matchedCourse.Name;
                    }
                }

                // Update Grid UI State
                if (enrollments.Count == 0)
                {
                    DgEnrollments.Visibility = Visibility.Collapsed;
                    PanelEmptyEnrollments.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelEmptyEnrollments.Visibility = Visibility.Collapsed;
                    DgEnrollments.Visibility = Visibility.Visible;
                    DgEnrollments.ItemsSource = enrollments;
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Error syncing pipeline data models: {ex.Message}", Brushes.Red);
            }
        }

        private async void BtnEnroll_Click(object sender, RoutedEventArgs e)
        {
            if (CboStudents.SelectedValue == null || CboCourses.SelectedValue == null)
            {
                ShowFeedback("Dropdown allocations must be selected.", Brushes.Orange);
                return;
            }

            var dto = new EnrollStudentDto
            {
                StudentId = (int)CboStudents.SelectedValue,
                CourseId = (int)CboCourses.SelectedValue
            };

            try
            {
                var httpResponse = await _apiClient.PostAsync("enrollments", dto);
                if (httpResponse.IsSuccessStatusCode)
                {
                    ShowFeedback("Student successfully enrolled in course track!", Brushes.Green);
                    CboStudents.SelectedIndex = -1;
                    CboCourses.SelectedIndex = -1;
                    await CompleteDataFetchInit();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Server rejected enrollment validation parameters.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network processing failure: {ex.Message}", Brushes.Red);
            }
        }

        private async void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EnrollmentVm enrollment)
            {
                var confirm = MessageBox.Show(
                    $"Mark course track as completed for {enrollment.StudentName}?",
                    "Confirm Course Graduation Status",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Matches backend body expected schema: record CompleteRequest(int EnrollmentId)
                        var payload = new { EnrollmentId = enrollment.Id };
                        var httpResponse = await _apiClient.PostAsync("enrollments/complete", payload);

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Course tracking record successfully updated to Completed.", "Status Change Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
                            await CompleteDataFetchInit();
                        }
                        else
                        {
                            var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                            MessageBox.Show(err?.Message ?? "Action rejected by server rules.", "Execution Refused", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Pipeline network disconnect error: {ex.Message}", "Connection Defined Break", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnDrop_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EnrollmentVm enrollment)
            {
                var confirm = MessageBox.Show(
                    $"Are you sure you want to drop {enrollment.StudentName} from {enrollment.CourseName}?\nThis changes status metrics to 'Dropped' inside system auditing.",
                    "Confirm Course Drop Request",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        // DELETE api/enrollments/{id}
                        var httpResponse = await _apiClient.DeleteAsync($"enrollments/{enrollment.Id}");

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Enrollment tracking row successfully closed out to dropped state.", "Drop Execution Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
                            await CompleteDataFetchInit();
                        }
                        else
                        {
                            var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                            MessageBox.Show(err?.Message ?? "Drop request blocked by active service constraints.", "Execution Refused", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Pipeline network failure during deletion request: {ex.Message}", "Connection Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ShowFeedback(string text, Brush color)
        {
            LblStatus.Text = text;
            LblStatus.Foreground = color;
            LblStatus.Visibility = Visibility.Visible;
        }
    }
}