using System;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class EditAttendanceWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly AttendanceVm _attendanceToEdit;

        public bool IsDataSaved { get; private set; } = false;

        public EditAttendanceWindow(AttendanceVm attendanceToEdit)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _attendanceToEdit = attendanceToEdit;

            LoadAttendanceDataIntoForm();
        }

        private void LoadAttendanceDataIntoForm()
        {
            TxtStudentName.Text = _attendanceToEdit.StudentName;
            TxtCourseName.Text = _attendanceToEdit.CourseName;
            TxtDate.Text = _attendanceToEdit.Date.ToString("MM/dd/yyyy");
            CboStatus.SelectedIndex = (int)_attendanceToEdit.Status;
            TxtRemarks.Text = _attendanceToEdit.Remarks;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CboStatus.SelectedIndex < 0)
            {
                ShowFeedback("Please select a status.", Brushes.Orange);
                return;
            }

            var dto = new UpdateAttendanceDto
            {
                Status = (AttendanceStatus)CboStatus.SelectedIndex,
                Remarks = string.IsNullOrWhiteSpace(TxtRemarks.Text) ? null : TxtRemarks.Text.Trim()
            };

            try
            {
                var httpResponse = await _apiClient.PutAsync($"attendance/{_attendanceToEdit.Id}", dto);

                if (httpResponse.IsSuccessStatusCode)
                {
                    IsDataSaved = true;
                    MessageBox.Show("Attendance record updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Server rejected the update request.", Brushes.Red);
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}