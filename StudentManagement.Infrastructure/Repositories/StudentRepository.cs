using Microsoft.Data.SqlClient;
using StudentManagement.Infrastructure.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;
using System.Data;

namespace StudentManagement.Infrastructure.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly SqlHelper _sqlHelper;

    public StudentRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public void Add(Student entity)
    {
        const string query = @"
            INSERT INTO Students (RollNumber, FirstName, LastName, Email, Phone, Address, DateOfBirth, AdmissionDate)
            VALUES (@RollNumber, @FirstName, @LastName, @Email, @Phone, @Address, @DateOfBirth, @AdmissionDate);";

        _sqlHelper.ExecuteNonQuery(query, MapParameters(entity));
    }

    public Student? GetById(int id)
    {
        const string query = "SELECT * FROM Students WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public IEnumerable<Student> GetAll()
    {
        const string query = "SELECT * FROM Students;";
        var table = _sqlHelper.ExecuteQuery(query);
        return table.AsEnumerable().Select(ToEntity);
    }

    public void Update(Student entity)
    {
        const string query = @"
            UPDATE Students 
            SET FirstName = @FirstName, LastName = @LastName, Email = @Email, 
                Phone = @Phone, Address = @Address
            WHERE Id = @Id;";

        var parameters = MapParameters(entity).ToList();
        parameters.Add(new SqlParameter("@Id", entity.Id));

        _sqlHelper.ExecuteNonQuery(query, parameters.ToArray());
    }

    public void Delete(int id)
    {
        const string query = "DELETE FROM Students WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public Student? GetByRollNumber(string rollNumber)
    {
        const string query = "SELECT * FROM Students WHERE RollNumber = @RollNumber;";
        var parameters = new[] { new SqlParameter("@RollNumber", rollNumber) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public Student? GetByEmail(string email)
    {
        const string query = "SELECT * FROM Students WHERE Email = @Email;";
        var parameters = new[] { new SqlParameter("@Email", email) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    // Helper method to convert a data row back into a Rich Domain Model using reflection/constructor matching safely
    private static Student ToEntity(DataRow row)
    {
        // Utilizing the protected parameterless constructor implicitly via reflection mapping activation
        var student = (Student)Activator.CreateInstance(typeof(Student), true)!;

        typeof(BaseEntity).GetProperty("Id")!.SetValue(student, Convert.ToInt32(row["Id"]));
        typeof(Student).GetProperty("RollNumber")!.SetValue(student, row["RollNumber"].ToString());
        typeof(Student).GetProperty("FirstName")!.SetValue(student, row["FirstName"].ToString());
        typeof(Student).GetProperty("LastName")!.SetValue(student, row["LastName"].ToString());
        typeof(Student).GetProperty("Email")!.SetValue(student, row["Email"].ToString());
        typeof(Student).GetProperty("Phone")!.SetValue(student, row["Phone"] == DBNull.Value ? null : row["Phone"].ToString());
        typeof(Student).GetProperty("Address")!.SetValue(student, row["Address"] == DBNull.Value ? null : row["Address"].ToString());
        typeof(Student).GetProperty("DateOfBirth")!.SetValue(student, Convert.ToDateTime(row["DateOfBirth"]));
        typeof(Student).GetProperty("AdmissionDate")!.SetValue(student, Convert.ToDateTime(row["AdmissionDate"]));

        return student;
    }

    private static SqlParameter[] MapParameters(Student entity)
    {
        return new[]
        {
            new SqlParameter("@RollNumber", entity.RollNumber),
            new SqlParameter("@FirstName", entity.FirstName),
            new SqlParameter("@LastName", entity.LastName),
            new SqlParameter("@Email", entity.Email),
            new SqlParameter("@Phone", (object?)entity.Phone ?? DBNull.Value),
            new SqlParameter("@Address", (object?)entity.Address ?? DBNull.Value),
            new SqlParameter("@DateOfBirth", entity.DateOfBirth),
            new SqlParameter("@AdmissionDate", entity.AdmissionDate)
        };
    }
}