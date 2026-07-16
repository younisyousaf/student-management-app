using Microsoft.Data.SqlClient;
using StudentManagement.Infrastructure.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;
using System.Data;

namespace StudentManagement.Infrastructure.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly SqlHelper _sqlHelper;

    public CourseRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public void Add(Course entity)
    {
        const string query = @"
            INSERT INTO Courses (Code, Name, Description, DurationMonths, FeeAmount)
            VALUES (@Code, @Name, @Description, @DurationMonths, @FeeAmount);";

        _sqlHelper.ExecuteNonQuery(query, MapParameters(entity));
    }

    public Course? GetById(int id)
    {
        const string query = "SELECT * FROM Courses WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public IEnumerable<Course> GetAll()
    {
        const string query = "SELECT * FROM Courses;";
        var table = _sqlHelper.ExecuteQuery(query);
        return table.AsEnumerable().Select(ToEntity);
    }

    public void Update(Course entity)
    {
        const string query = @"
            UPDATE Courses 
            SET Name = @Name, Description = @Description, 
                DurationMonths = @DurationMonths, FeeAmount = @FeeAmount
            WHERE Id = @Id;";

        var parameters = MapParameters(entity).ToList();
        parameters.Add(new SqlParameter("@Id", entity.Id));

        _sqlHelper.ExecuteNonQuery(query, parameters.ToArray());
    }

    public void Delete(int id)
    {
        const string query = "DELETE FROM Courses WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public Course? GetByCode(string code)
    {
        const string query = "SELECT * FROM Courses WHERE Code = @Code;";
        var parameters = new[] { new SqlParameter("@Code", code) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    private static Course ToEntity(DataRow row)
    {
        var course = (Course)Activator.CreateInstance(typeof(Course), true)!;

        typeof(BaseEntity).GetProperty("Id")!.SetValue(course, Convert.ToInt32(row["Id"]));
        typeof(Course).GetProperty("Code")!.SetValue(course, row["Code"].ToString());
        typeof(Course).GetProperty("Name")!.SetValue(course, row["Name"].ToString());
        typeof(Course).GetProperty("Description")!.SetValue(course, row["Description"] == DBNull.Value ? null : row["Description"].ToString());
        typeof(Course).GetProperty("DurationMonths")!.SetValue(course, Convert.ToInt32(row["DurationMonths"]));
        typeof(Course).GetProperty("FeeAmount")!.SetValue(course, Convert.ToDecimal(row["FeeAmount"]));

        return course;
    }

    private static SqlParameter[] MapParameters(Course entity)
    {
        return new[]
        {
            new SqlParameter("@Code", entity.Code),
            new SqlParameter("@Name", entity.Name),
            new SqlParameter("@Description", (object?)entity.Description ?? DBNull.Value),
            new SqlParameter("@DurationMonths", entity.DurationMonths),
            new SqlParameter("@FeeAmount", entity.FeeAmount)
        };
    }
}