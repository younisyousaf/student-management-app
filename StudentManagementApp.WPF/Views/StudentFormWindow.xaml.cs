using System.Net.Http.Json;
using System.Windows;
using System.Windows.Media;
using StudentManagementApp.WPF.DTOs;
using StudentManagementApp.WPF.Services;

namespace StudentManagementApp.WPF.Views
{
    public partial class StudentFormWindow : Window
    {
        private readonly ApiClient _apiClient;
        private readonly List<StudentModel> _existingStudents;
        private readonly StudentModel? _studentToEdit;

        [System.Text.RegularExpressions.GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial System.Text.RegularExpressions.Regex EmailRegex();

        public bool IsDataSaved { get; private set; } = false;
        private bool IsEditMode => _studentToEdit != null;

        public StudentFormWindow(List<StudentModel> currentLoadedData, StudentModel? studentToEdit = null)
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _existingStudents = currentLoadedData ?? [];
            _studentToEdit = studentToEdit;

            if (IsEditMode)
            {
                Title = "Update Student Profile";
                TxtHeaderTitle.Text = "Update Student Profile";
                TxtHeaderSubtitle.Text = "Modify the fields below to update the student record.";
                BtnSaveStudent.Content = "Update Profile";
                LoadStudentDataIntoForm();
            }
            else
            {
                DpDob.SelectedDate = DateTime.Now.AddYears(-18);
            }
        }

        private void LoadStudentDataIntoForm()
        {
            if (_studentToEdit == null) return;

            TxtRollNumber.Text = _studentToEdit.RollNumber;
            TxtFirstName.Text = _studentToEdit.FirstName;
            TxtLastName.Text = _studentToEdit.LastName;
            TxtEmail.Text = _studentToEdit.Email;
            DpDob.SelectedDate = _studentToEdit.DateOfBirth;
            TxtPhone.Text = _studentToEdit.Phone;
            TxtAddress.Text = _studentToEdit.Address;

            // Roll number is usually an immutable structural key once registered
            TxtRollNumber.IsEnabled = false;
        }

        private async void BtnSaveStudent_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRollNumber.Text) ||
                string.IsNullOrWhiteSpace(TxtFirstName.Text) ||
                string.IsNullOrWhiteSpace(TxtLastName.Text) ||
                string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                ShowFeedback("All fields marked with (*) are required variables.", Brushes.Orange);
                return;
            }

            string emailInput = TxtEmail.Text.Trim();
            string rollInput = TxtRollNumber.Text.Trim();

            if (!EmailRegex().IsMatch(emailInput))
            {
                ShowFeedback("Please supply a structurally accurate email address configuration.", Brushes.Red);
                return;
            }

            // Duplicate validations (Skip validating against self if editing)
            bool rollDuplicate = _existingStudents.Any(s =>
                (!IsEditMode || !s.RollNumber.Equals(_studentToEdit!.RollNumber, StringComparison.OrdinalIgnoreCase))
                && s.RollNumber.Equals(rollInput, StringComparison.OrdinalIgnoreCase));

            bool emailDuplicate = _existingStudents.Any(s =>
                (!IsEditMode || !s.Email.Equals(_studentToEdit!.Email, StringComparison.OrdinalIgnoreCase))
                && s.Email.Equals(emailInput, StringComparison.OrdinalIgnoreCase));

            if (rollDuplicate)
            {
                ShowFeedback($"Operation Denied: A student with Roll Number '{rollInput}' is already registered.", Brushes.Red);
                return;
            }

            if (emailDuplicate)
            {
                ShowFeedback($"Operation Denied: The email address '{emailInput}' is already tied to a profile.", Brushes.Red);
                return;
            }

            var dto = new StudentModel
            {
                Id = IsEditMode ? _studentToEdit!.Id : 0,
                RollNumber = rollInput,
                FirstName = TxtFirstName.Text.Trim(),
                LastName = TxtLastName.Text.Trim(),
                Email = emailInput,
                DateOfBirth = DpDob.SelectedDate ?? DateTime.Now.AddYears(-18),
                Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim(),
                Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim()
            };

            try
            {
                System.Net.Http.HttpResponseMessage httpResponse;

                if (IsEditMode)
                {
                    // Call API PUT endpoint for update tracking cycles
                    httpResponse = await _apiClient.PutAsync($"students/{dto.Id}", dto);
                }
                else
                {
                    // Call API POST endpoint for new data configurations
                    httpResponse = await _apiClient.PostAsync("students", dto);
                }

                if (httpResponse.IsSuccessStatusCode)
                {
                    IsDataSaved = true;
                    MessageBox.Show(IsEditMode ? "Student changes saved!" : "Student registered smoothly!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    var err = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>();
                    ShowFeedback(err?.Message ?? "Validation parameters failed on backend API pipeline.", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"Network Interruption: {ex.Message}", Brushes.Red);
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