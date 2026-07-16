using Microsoft.Data.SqlClient;
using StudentManagement.Infrastructure.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;
using System.Data;

namespace StudentManagement.Infrastructure.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly SqlHelper _sqlHelper;

    public EnrollmentRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public void Add(Enrollment entity)
    {
        const string query = @"
            INSERT INTO Enrollments (StudentId, CourseId, EnrollDate, Status)
            VALUES (@StudentId, @CourseId, @EnrollDate, @Status);";

        _sqlHelper.ExecuteNonQuery(query, MapParameters(entity));
    }

    public Enrollment? GetById(int id)
    {
        const string query = "SELECT * FROM Enrollments WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public IEnumerable<Enrollment> GetAll()
    {
        const string query = "SELECT * FROM Enrollments;";
        var table = _sqlHelper.ExecuteQuery(query);
        return table.AsEnumerable().Select(ToEntity);
    }

    public void Update(Enrollment entity)
    {
        const string query = @"
            UPDATE Enrollments 
            SET Status = @Status 
            WHERE Id = @Id;";

        var parameters = new[]
        {
            new SqlParameter("@Id", entity.Id),
            new SqlParameter("@Status", entity.Status)
        };

        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public void Delete(int id)
    {
        const string query = "DELETE FROM Enrollments WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public bool IsAlreadyEnrolled(int studentId, int courseId)
    {
        const string query = @"
            SELECT COUNT(1) 
            FROM Enrollments 
            WHERE StudentId = @StudentId AND CourseId = @CourseId AND Status != 'Dropped';";

        var parameters = new[]
        {
            new SqlParameter("@StudentId", studentId),
            new SqlParameter("@CourseId", courseId)
        };

        int count = _sqlHelper.ExecuteScalar<int>(query, parameters);
        return count > 0;
    }

    public IEnumerable<Enrollment> GetByStudentId(int studentId)
    {
        const string query = "SELECT * FROM Enrollments WHERE StudentId = @StudentId;";
        var parameters = new[] { new SqlParameter("@StudentId", studentId) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);
        return table.AsEnumerable().Select(ToEntity);
    }

    private static Enrollment ToEntity(DataRow row)
    {
        var enrollment = (Enrollment)Activator.CreateInstance(typeof(Enrollment), true)!;

        typeof(BaseEntity).GetProperty("Id")!.SetValue(enrollment, Convert.ToInt32(row["Id"]));
        typeof(Enrollment).GetProperty("StudentId")!.SetValue(enrollment, Convert.ToInt32(row["StudentId"]));
        typeof(Enrollment).GetProperty("CourseId")!.SetValue(enrollment, Convert.ToInt32(row["CourseId"]));
        typeof(Enrollment).GetProperty("EnrollDate")!.SetValue(enrollment, Convert.ToDateTime(row["EnrollDate"]));
        typeof(Enrollment).GetProperty("Status")!.SetValue(enrollment, row["Status"].ToString());

        return enrollment;
    }

    private static SqlParameter[] MapParameters(Enrollment entity)
    {
        return new[]
        {
            new SqlParameter("@StudentId", entity.StudentId),
            new SqlParameter("@CourseId", entity.CourseId),
            new SqlParameter("@EnrollDate", entity.EnrollDate),
            new SqlParameter("@Status", entity.Status)
        };
    }
}