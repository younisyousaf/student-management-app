namespace StudentManagement.Core.Models;

public class Student : BaseEntity
{
    public string RollNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public DateTime DateOfBirth { get; private set; }
    public DateTime AdmissionDate { get; init; }

    public string FullName => $"{FirstName} {LastName}";


    public Student(string rollNumber, string firstName, string lastName, string email, DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(rollNumber)) throw new ArgumentException("Roll number is mandatory.");
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Name components cannot be blank.");

        RollNumber = rollNumber;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        DateOfBirth = dateOfBirth;
        AdmissionDate = DateTime.UtcNow;
    }

    protected Student() { }

    public void UpdateProfile(string firstName, string lastName, string? phone, string? address)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Names cannot be overwritten with blank data.");

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Address = address;
    }

    public void ChangeEmail(string newEmail)
    {
        if (!newEmail.Contains('@')) throw new ArgumentException("Invalid Email syntax structural format.");
        Email = newEmail;
    }

    public override string ToString() => $"[{RollNumber}] {FullName} | Contact: {Email}";
}