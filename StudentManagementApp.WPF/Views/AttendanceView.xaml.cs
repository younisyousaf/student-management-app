using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class AttendanceView : UserControl
    {
        private readonly ApiClient _apiClient;
        private List<StudentModel> _cachedStudents = [];
        private List<CourseModel> _cachedCourses = [];

        public AttendanceView()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            Loaded += async (s, e) => await LoadAttendanceAsync();
        }

        private async Task LoadAttendanceAsync()
        {
            try
            {
                // Fetch all three collections
                var studentRes = await _apiClient.GetAsync<ApiResponse<List<StudentModel>>>("students");
                var courseRes = await _apiClient.GetAsync<ApiResponse<List<CourseModel>>>("courses");
                var attendanceRes = await _apiClient.GetAsync<ApiResponse<List<AttendanceVm>>>("attendance");

                _cachedStudents = studentRes?.Data ?? [];
                _cachedCourses = courseRes?.Data ?? [];
                var records = attendanceRes?.Data ?? [];

                // Populate the dropdown lists
                CboStudents.ItemsSource = _cachedStudents.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.FirstName} {s.LastName}".Trim()
                }).ToList();

                CboCourses.ItemsSource = _cachedCourses;

              //Show names intead of Id's
                foreach (var record in records)
                {
                    var matchedStudent = _cachedStudents.FirstOrDefault(s => s.Id == record.StudentId);
                    if (matchedStudent != null)
                    {
                        record.StudentName = $"{matchedStudent.FirstName} {matchedStudent.LastName}".Trim();
                    }

                    var matchedCourse = _cachedCourses.FirstOrDefault(c => c.Id == record.CourseId);
                    if (matchedCourse != null)
                    {
                        record.CourseName = matchedCourse.Name;
                    }
                }

                // Update Grid UI state
                if (records.Count == 0)
                {
                    DgAttendance.Visibility = Visibility.Collapsed;
                    PanelEmptyAttendance.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelEmptyAttendance.Visibility = Visibility.Collapsed;
                    DgAttendance.Visibility = Visibility.Visible;
                    DgAttendance.ItemsSource = records.OrderByDescending(r => r.Date).ToList();
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Error loading attendance data: {ex.Message}", Brushes.Red);
            }
        }

        private async void BtnMark_Click(object sender, RoutedEventArgs e)
        {
            if (CboStudents.SelectedValue == null || CboCourses.SelectedValue == null)
            {
                ShowFeedback("Please select both a student and a course.", Brushes.Orange);
                return;
            }

            if (DpDate.SelectedDate == null)
            {
                ShowFeedback("Please select a date.", Brushes.Orange);
                return;
            }

            var dto = new MarkAttendanceDto
            {
                StudentId = (int)CboStudents.SelectedValue,
                CourseId = (int)CboCourses.SelectedValue,
                Date = DpDate.SelectedDate.Value,
                Status = (AttendanceStatus)CboStatus.SelectedIndex,
                Remarks = string.IsNullOrWhiteSpace(TxtRemarks.Text) ? null : TxtRemarks.Text.Trim()
            };

            try
            {
                var httpResponse = await _apiClient.PostAsync("attendance", dto);
                if (httpResponse.IsSuccessStatusCode)
                {
                    ShowFeedback("Attendance marked successfully.", Brushes.Green);

                    CboStudents.SelectedIndex = -1;
                    CboCourses.SelectedIndex = -1;
                    DpDate.SelectedDate = null;
                    CboStatus.SelectedIndex = 0;
                    TxtRemarks.Clear();

                    await LoadAttendanceAsync();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Server rejected the attendance record.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network error: {ex.Message}", Brushes.Red);
            }
        }

        private async void BtnEditAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AttendanceVm record)
            {
                var formWindow = new EditAttendanceWindow(record)
                {
                    Owner = Window.GetWindow(this)
                };

                formWindow.ShowDialog();

                if (formWindow.IsDataSaved)
                {
                    await LoadAttendanceAsync();
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