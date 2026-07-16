using StudentManagementSystem.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;

namespace StudentManagementSystem.Controllers;

public class StudentController
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    public void ManageStudents()
    {
        string[] menuOptions = { "Register New Student", "View All Students", "Find Student By Roll Number", "Update Student Profile", "Remove Student" };

        while (true)
        {
            int choice = ConsoleHelper.ShowMenu("Student Management", menuOptions);
            if (choice == 0) break;

            switch (choice)
            {
                case 1: RegisterNewStudent(); break;
                case 2: ViewAllStudents(); break;
                case 3: FindStudentByRoll(); break;
                case 4: UpdateStudent(); break;
                case 5: RemoveStudent(); break;
            }
            ConsoleHelper.Pause();
        }
    }

    private void RegisterNewStudent()
    {
        ConsoleHelper.Info("\n--- Register New Student ---");
        string roll = ConsoleHelper.ReadRequired("Enter Roll Number");
        string firstName = ConsoleHelper.ReadRequired("Enter First Name");
        string lastName = ConsoleHelper.ReadRequired("Enter Last Name");
        string email = ConsoleHelper.ReadRequired("Enter Email Address");
        DateTime dob = ConsoleHelper.ReadDate("Enter Date of Birth");

        // Capture the phone and address FIRST before building the object
        string phone = ConsoleHelper.ReadRequired("Enter Phone Number");
        string address = ConsoleHelper.ReadRequired("Enter Home Address");

        try
        {
            // Pass all captured variables cleanly into your constructor or domain mutation method
            var student = new Student(roll, firstName, lastName, email, dob);

            // Use the domain method to safely update the encapsulated state
            student.UpdateProfile(firstName, lastName, phone, address);

            _studentService.RegisterStudent(student);
            ConsoleHelper.Success("Student registered successfully in the database system.");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void ViewAllStudents()
    {
        var students = _studentService.GetAllStudents();
        ConsoleHelper.PrintList("Registered Students Ledger", students);
    }

    private void FindStudentByRoll()
    {
        string roll = ConsoleHelper.ReadRequired("Enter Roll Number to Search");
        var student = _studentService.GetStudentByRollNumber(roll);

        if (student == null)
        {
            ConsoleHelper.Error($"No record found matching Roll Number '{roll}'.");
            return;
        }

        ConsoleHelper.Info($"\nProfile Found:\nID: {student.Id}\nFull Name: {student.FullName}\nEmail: {student.Email}\nPhone: {student.Phone}\nAddress: {student.Address}");
    }

    private void UpdateStudent()
    {
        string roll = ConsoleHelper.ReadRequired("Enter Roll Number of the student to modify");
        var student = _studentService.GetStudentByRollNumber(roll);

        if (student == null)
        {
            ConsoleHelper.Error($"Student record matching Roll Number '{roll}' not found.");
            return;
        }

        ConsoleHelper.Info($"\nModifying records for: {student.FullName}");
        string firstName = ConsoleHelper.ReadOptional("First Name", student.FirstName);
        string lastName = ConsoleHelper.ReadOptional("Last Name", student.LastName);
        string email = ConsoleHelper.ReadOptional("Email Address", student.Email);
        string? phone = ConsoleHelper.ReadOptional("Phone Number", student.Phone ?? "");
        string? address = ConsoleHelper.ReadOptional("Address", student.Address ?? "");

        try
        {
            _studentService.UpdateStudentProfile(student.Id, firstName, lastName, phone, address, email);
            ConsoleHelper.Success("Student metrics successfully updated.");
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is KeyNotFoundException)
        {
            ConsoleHelper.Error(ex.Message);
        }
    }

    private void RemoveStudent()
    {
        string roll = ConsoleHelper.ReadRequired("Enter Roll Number to delete");
        var student = _studentService.GetStudentByRollNumber(roll);

        if (student == null)
        {
            ConsoleHelper.Error("Record not found.");
            return;
        }

        if (ConsoleHelper.Confirm($"Are you absolutely sure you want to permanently delete {student.FullName}? All historical enrollments will drop."))
        {
            try
            {
                _studentService.RemoveStudent(student.Id);
                ConsoleHelper.Success("Student records deleted completely.");
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error(ex.Message);
            }
        }
    }
}