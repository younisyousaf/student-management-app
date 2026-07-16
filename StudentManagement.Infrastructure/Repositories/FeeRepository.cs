using Microsoft.Data.SqlClient;
using StudentManagement.Infrastructure.Helpers;
using StudentManagement.Core.Models;
using StudentManagement.Core.Interfaces;
using System.Data;

namespace StudentManagement.Infrastructure.Repositories;

public class FeeRepository : IFeeRepository
{
    private readonly SqlHelper _sqlHelper;

    public FeeRepository(SqlHelper sqlHelper)
    {
        _sqlHelper = sqlHelper;
    }

    public void Add(Fee entity)
    {
        const string query = @"
            INSERT INTO Fees (StudentId, CourseId, AmountDue, AmountPaid, PaymentDate, Remarks)
            VALUES (@StudentId, @CourseId, @AmountDue, @AmountPaid, @PaymentDate, @Remarks);";

        _sqlHelper.ExecuteNonQuery(query, MapParameters(entity));
    }

    public Fee? GetById(int id)
    {
        const string query = "SELECT * FROM Fees WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    public IEnumerable<Fee> GetAll()
    {
        const string query = "SELECT * FROM Fees;";
        var table = _sqlHelper.ExecuteQuery(query);
        return table.AsEnumerable().Select(ToEntity);
    }

    public void Update(Fee entity)
    {
        const string query = @"
            UPDATE Fees 
            SET AmountPaid = @AmountPaid, 
                PaymentDate = @PaymentDate, 
                Remarks = @Remarks 
            WHERE Id = @Id;";

        var parameters = new[]
        {
            new SqlParameter("@Id", entity.Id),
            new SqlParameter("@AmountPaid", entity.AmountPaid),
            new SqlParameter("@PaymentDate", (object?)entity.PaymentDate ?? DBNull.Value),
            new SqlParameter("@Remarks", (object?)entity.Remarks ?? DBNull.Value)
        };

        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public void Delete(int id)
    {
        const string query = "DELETE FROM Fees WHERE Id = @Id;";
        var parameters = new[] { new SqlParameter("@Id", id) };
        _sqlHelper.ExecuteNonQuery(query, parameters);
    }

    public Fee? GetByStudentAndCourse(int studentId, int courseId)
    {
        const string query = "SELECT * FROM Fees WHERE StudentId = @StudentId AND CourseId = @CourseId;";
        var parameters = new[]
        {
            new SqlParameter("@StudentId", studentId),
            new SqlParameter("@CourseId", courseId)
        };
        var table = _sqlHelper.ExecuteQuery(query, parameters);

        return table.Rows.Count == 0 ? null : ToEntity(table.Rows[0]);
    }

    private static Fee ToEntity(DataRow row)
    {
        var fee = (Fee)Activator.CreateInstance(typeof(Fee), true)!;

        typeof(BaseEntity).GetProperty("Id")!.SetValue(fee, Convert.ToInt32(row["Id"]));
        typeof(Fee).GetProperty("StudentId")!.SetValue(fee, Convert.ToInt32(row["StudentId"]));
        typeof(Fee).GetProperty("CourseId")!.SetValue(fee, Convert.ToInt32(row["CourseId"]));
        typeof(Fee).GetProperty("AmountDue")!.SetValue(fee, Convert.ToDecimal(row["AmountDue"]));
        typeof(Fee).GetProperty("AmountPaid")!.SetValue(fee, Convert.ToDecimal(row["AmountPaid"]));
        typeof(Fee).GetProperty("PaymentDate")!.SetValue(fee, row["PaymentDate"] == DBNull.Value ? null : Convert.ToDateTime(row["PaymentDate"]));
        typeof(Fee).GetProperty("Remarks")!.SetValue(fee, row["Remarks"] == DBNull.Value ? null : row["Remarks"].ToString());

        return fee;
    }

    private static SqlParameter[] MapParameters(Fee entity)
    {
        return new[]
        {
            new SqlParameter("@StudentId", entity.StudentId),
            new SqlParameter("@CourseId", entity.CourseId),
            new SqlParameter("@AmountDue", entity.AmountDue),
            new SqlParameter("@AmountPaid", entity.AmountPaid),
            new SqlParameter("@PaymentDate", (object?)entity.PaymentDate ?? DBNull.Value),
            new SqlParameter("@Remarks", (object?)entity.Remarks ?? DBNull.Value)
        };
    }
}