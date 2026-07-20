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
    public partial class FeesView : UserControl
    {
        private readonly ApiClient _apiClient;

        public FeesView()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            Loaded += async (s, e) => await LoadFeeLedgersAsync();
        }

        private async Task LoadFeeLedgersAsync()
        {
            try
            {
                // Fetch all collections from the API
                var studentRes = await _apiClient.GetAsync<ApiResponse<List<StudentModel>>>("students");
                var courseRes = await _apiClient.GetAsync<ApiResponse<List<CourseModel>>>("courses");
                var feeRes = await _apiClient.GetAsync<ApiResponse<List<FeeVm>>>("fees");

                var students = studentRes?.Data ?? [];
                var courses = courseRes?.Data ?? [];
                var ledgers = feeRes?.Data ?? [];

                // Populate the dropdown lists
                CboStudents.ItemsSource = students.Select(s => new
                {
                    Id = s.Id,
                    FullName = $"{s.FirstName} {s.LastName}".Trim()
                }).ToList();

                CboCourses.ItemsSource = courses;

                // MANUAL IN-MEMORY JOIN FIX:
                // Since backend "student" and "course" fields are null, look them up using IDs
                foreach (var ledger in ledgers)
                {
                    var matchedStudent = students.FirstOrDefault(s => s.Id == ledger.StudentId);
                    if (matchedStudent != null)
                    {
                        // We create a dummy model object locally so our FeeVm properties don't throw NullReferenceExceptions
                        ledger.Student = matchedStudent;
                    }

                    var matchedCourse = courses.FirstOrDefault(c => c.Id == ledger.CourseId);
                    if (matchedCourse != null)
                    {
                        ledger.Course = matchedCourse;
                    }
                }

                // Update UI Grid Source
                if (ledgers.Count == 0)
                {
                    DgFees.Visibility = Visibility.Collapsed;
                    PanelEmptyFees.Visibility = Visibility.Visible;
                }
                else
                {
                    PanelEmptyFees.Visibility = Visibility.Collapsed;
                    DgFees.Visibility = Visibility.Visible;
                    DgFees.ItemsSource = ledgers;
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Error pulling down accounting data maps: {ex.Message}", Brushes.Red);
            }
        }

        private async void BtnProcessPayment_Click(object sender, RoutedEventArgs e)
        {
            if (CboStudents.SelectedValue == null || CboCourses.SelectedValue == null)
            {
                ShowFeedback("Required selection profiles are blank.", Brushes.Orange);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAmountPaid.Text) || !decimal.TryParse(TxtAmountPaid.Text, out decimal parsedAmount) || parsedAmount <= 0)
            {
                ShowFeedback("Please enter a valid financial payment amount.", Brushes.Orange);
                return;
            }

            var dto = new FeeDto
            {
                StudentId = (int)CboStudents.SelectedValue,
                CourseId = (int)CboCourses.SelectedValue,
                AmountPaid = parsedAmount,
                Remarks = TxtRemarks.Text.Trim()
            };

            try
            {
                var httpResponse = await _apiClient.PostAsync("fees/pay", dto);
                if (httpResponse.IsSuccessStatusCode)
                {
                    ShowFeedback("Remittance recorded completely into ledger accounts.", Brushes.Green);

                    CboStudents.SelectedIndex = -1;
                    CboCourses.SelectedIndex = -1;
                    TxtAmountPaid.Clear();
                    TxtRemarks.Clear();

                    await LoadFeeLedgersAsync();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Server rejected financial balance profile verification.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network connection disrupted: {ex.Message}", Brushes.Red);
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