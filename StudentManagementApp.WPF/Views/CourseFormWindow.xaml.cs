using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class CourseFormWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly List<CourseModel> _existingCourses;
        private readonly CourseModel? _courseToEdit;

        public bool IsDataSaved { get; private set; } = false;
        private bool IsEditMode => _courseToEdit != null;

        public CourseFormWindow(List<CourseModel> currentLoadedData, CourseModel? courseToEdit = null)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _existingCourses = currentLoadedData ?? [];
            _courseToEdit = courseToEdit;

            if (IsEditMode)
            {
                Title = "Update Course Details";
                TxtHeaderTitle.Text = "Modify Course Profile";
                TxtHeaderSubtitle.Text = "Adjust core program descriptions or institutional tuition variables.";
                BtnSaveCourse.Content = "Update Course";
                LoadCourseDataIntoForm();
            }
        }

        private void LoadCourseDataIntoForm()
        {
            if (_courseToEdit == null) return;

            TxtCourseCode.Text = _courseToEdit.Code;
            TxtCourseName.Text = _courseToEdit.Name;
            TxtDescription.Text = _courseToEdit.Description;
            TxtDuration.Text = _courseToEdit.DurationMonths.ToString();
            TxtFee.Text = _courseToEdit.FeeAmount.ToString("F2");

            // Course code is a unique identifier and should not be modified during an edit
            TxtCourseCode.IsEnabled = false;
        }

        private async void BtnSaveCourse_Click(object sender, RoutedEventArgs e)
        {
            // Text Field Length Boundaries Validations
            if (string.IsNullOrWhiteSpace(TxtCourseCode.Text) || string.IsNullOrWhiteSpace(TxtCourseName.Text))
            {
                ShowFeedback("Course Code and Course Name are mandatory fields.", Brushes.Orange);
                return;
            }

            string codeInput = TxtCourseCode.Text.Trim();
            string nameInput = TxtCourseName.Text.Trim();

            if (!int.TryParse(TxtDuration.Text, out int duration) || duration <= 0)
            {
                ShowFeedback("Please input a valid numeric duration above 0 months.", Brushes.Red);
                return;
            }

            if (!decimal.TryParse(TxtFee.Text, out decimal feeAmount) || feeAmount < 0)
            {
                ShowFeedback("Please specify a valid financial base fee configuration.", Brushes.Red);
                return;
            }

            // Client-Side Duplicate Prevention Guard
            bool codeDuplicate = _existingCourses.Any(c =>
                (!IsEditMode || !c.Code.Equals(_courseToEdit!.Code, StringComparison.OrdinalIgnoreCase))
                && c.Code.Equals(codeInput, StringComparison.OrdinalIgnoreCase));

            if (codeDuplicate)
            {
                ShowFeedback($"Operation Aborted: Course code '{codeInput}' is already in use.", Brushes.Red);
                return;
            }

            var dto = new CreateCourseDto
            {
                Code = codeInput,
                Name = nameInput,
                Description = TxtDescription.Text.Trim(),
                DurationMonths = duration,
                FeeAmount = feeAmount
            };

            try
            {
                System.Net.Http.HttpResponseMessage httpResponse;

                if (IsEditMode)
                {
                    httpResponse = await _apiClient.PutAsync($"courses/{_courseToEdit!.Id}", dto);
                }
                else
                {
                    httpResponse = await _apiClient.PostAsync("courses", dto);
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    IsDataSaved = true;
                    MessageBox.Show(IsEditMode ? "Course updated successfully!" : "Course created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    // Safe parsing wrapper block to handle blank or plain text error strings elegantly
                    if (httpResponse.Content.Headers.ContentLength > 0)
                    {
                        try
                        {
                            var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                            ShowFeedback(err?.Message ?? "Server architecture validation logic rejected transaction.", Brushes.Red);
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            string rawError = await httpResponse.Content.ReadAsStringAsync();
                            ShowFeedback(!string.IsNullOrWhiteSpace(rawError) ? rawError : $"Server Error: {httpResponse.StatusCode}", Brushes.Red);
                        }
                    }
                    else
                    {
                        ShowFeedback($"Server rejected execution with status code: {httpResponse.StatusCode}", Brushes.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network Pipeline Failure: {ex.Message}", Brushes.Red);
            }
        }

        private void ShowFeedback(string text, Brush color)
        {
            LblStatus.Text = text;
            LblStatus.Foreground = color;
            LblStatus.Visibility = Visibility.Visible;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}