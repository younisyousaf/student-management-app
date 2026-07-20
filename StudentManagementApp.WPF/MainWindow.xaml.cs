using System.Windows;
using StudentManagementApp.WPF.Views;

namespace StudentManagementApp.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnShowStudents_Click(object sender, RoutedEventArgs e)
        {
            // Instantiates the Students custom UserControl we made and sets it as the primary child
            NavigateToModuleView(
                header: "Student Directory",
                subHeader: "Manage global student profiles, registration statuses, and enrollment entries.",
                view: new StudentsView()
            );
        }

        private void BtnShowCourses_Click(object sender, RoutedEventArgs e)
        {
            NavigateToModuleView(
                header: "Academic Courses",
                subHeader: "Maintain administrative curriculums, credit capacities, and module codes.",
                view: new CoursesView()
            );
        }

        private void BtnShowEnrollments_Click(object sender, RoutedEventArgs e)
        {
            NavigateToModuleView(
                header: "Class Enrollments",
                subHeader: "Process student enrollment links to existing course catalogs.",
                view: new EnrollmentsView()
            );
        }

        private void BtnShowFees_Click(object sender, RoutedEventArgs e)
        {
            NavigateToModuleView(
                header: "Tuition Fees Audits",
                subHeader: "Record billing ledgers, adjust amounts due, and review payment statuses.",
                view: new FeesView()
            );
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Clear global memory login states safely
            Services.AuthService.Instance.Logout();

            var loginWin = new LoginWindow();
            loginWin.Show();
            this.Close();
        }

        /// <summary>
        /// Centralized responsive view swapper. Handles layout context toggling with error prevention.
        /// </summary>
        private void NavigateToModuleView(string header, string subHeader, System.Windows.Controls.UserControl view)
        {
            // Update textual headers instantly
            TxtViewHeader.Text = header;
            TxtViewSubHeader.Text = subHeader;

            // Clear defaults out of visual bounds
            PanelWelcomeView.Visibility = Visibility.Collapsed;

            // Load the chosen operational view inside our shell container control
            ActiveViewContainer.Content = view;
            ActiveViewContainer.Visibility = Visibility.Visible;
        }
    }
}