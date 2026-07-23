using Microsoft.Data.SqlClient;
using StudentManagement.Core.Enums;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Helpers;
using System.Data;

namespace StudentManagement.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly SqlHelper _sqlHelper;

    public AttendanceRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public void Add(Attendance entity)
    {
        const string query = @"
            INSERT INTO Attendances (StudentId, CourseId, Date, Status, Remarks)
            VALUES (@StudentId, @CourseId, @Date, @Status, @Remarks);";

        _sqlHelper.ExecuteNonQuery(query, MapParameters(entity));
    }

    public Attendance? GetById(int id)
    {
        const string query = "SELECT * FROM Attendances WHERE Id = @Id;";
        var table = _sqlHelper.ExecuteQuery(query, new[] { new SqlParameter("@Id", id) });
        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public IEnumerable<Attendance> GetAll()
    {
        var table = _sqlHelper.ExecuteQuery("SELECT * FROM Attendances;");
        return table.AsEnumerable().Select(ToEntity);
    }

    public void Update(Attendance entity)
    {
        const string query = "UPDATE Attendances SET Status = @Status, Remarks = @Remarks WHERE Id = @Id;";
        var parameters = new[]
        {
            new SqlParameter("@Id", entity.Id),
            new SqlParameter("@Status", (int)entity.Status),
            new SqlParameter("@Remarks", (object?)entity.Remarks ?? DBNull.Value)
        };
        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public void Delete(int id)
    {
        _sqlHelper.ExecuteNonQuery("DELETE FROM Attendances WHERE Id = @Id;", new[] { new SqlParameter("@Id", id) });
    }

    public IEnumerable<Attendance> GetByStudentId(int studentId)
    {
        const string query = "SELECT * FROM Attendances WHERE StudentId = @StudentId ORDER BY Date DESC;";
        var table = _sqlHelper.ExecuteQuery(query, new[] { new SqlParameter("@StudentId", studentId) });
        return table.AsEnumerable().Select(ToEntity);
    }

    public IEnumerable<Attendance> GetByCourseAndDate(int courseId, DateTime date)
    {
        const string query = "SELECT * FROM Attendances WHERE CourseId = @CourseId AND Date = @Date;";
        var parameters = new[] { new SqlParameter("@CourseId", courseId), new SqlParameter("@Date", date.Date) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);
        return table.AsEnumerable().Select(ToEntity);
    }

    public Attendance? GetByStudentCourseAndDate(int studentId, int courseId, DateTime date)
    {
        const string query = @"
            SELECT * FROM Attendances
            WHERE StudentId = @StudentId AND CourseId = @CourseId AND Date = @Date;";
        var parameters = new[]
        {
            new SqlParameter("@StudentId", studentId),
            new SqlParameter("@CourseId", courseId),
            new SqlParameter("@Date", date.Date)
        };
        var table = _sqlHelper.ExecuteQuery(query, parameters);
        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    private static Attendance ToEntity(DataRow row)
    {
        var attendance = (Attendance)Activator.CreateInstance(typeof(Attendance), true)!;

        typeof(BaseEntity).GetProperty("Id")!.SetValue(attendance, Convert.ToInt32(row["Id"]));
        typeof(Attendance).GetProperty("StudentId")!.SetValue(attendance, Convert.ToInt32(row["StudentId"]));
        typeof(Attendance).GetProperty("CourseId")!.SetValue(attendance, Convert.ToInt32(row["CourseId"]));
        typeof(Attendance).GetProperty("Date")!.SetValue(attendance, Convert.ToDateTime(row["Date"]));
        typeof(Attendance).GetProperty("Status")!.SetValue(attendance, (AttendanceStatus)Convert.ToInt32(row["Status"]));
        typeof(Attendance).GetProperty("Remarks")!.SetValue(attendance, row["Remarks"] == DBNull.Value ? null : row["Remarks"].ToString());

        return attendance;
    }

    private static SqlParameter[] MapParameters(Attendance entity) => new[]
    {
        new SqlParameter("@StudentId", entity.StudentId),
        new SqlParameter("@CourseId", entity.CourseId),
        new SqlParameter("@Date", entity.Date),
        new SqlParameter("@Status", (int)entity.Status),
        new SqlParameter("@Remarks", (object?)entity.Remarks ?? DBNull.Value)
    };
}