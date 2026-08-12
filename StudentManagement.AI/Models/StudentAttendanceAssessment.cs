using System;
using System.Collections.Generic;
using System.Text;

namespace StudentManagement.AI.Models
{
    public record StudentAttendanceAssessment(
     int StudentId,
     string StudentName,
     int TotalRecords,
     int PresentCount,
     int AbsentCount,
     int LateCount,
     int ExcusedCount,
     double AttendancePercentage,
     string DataStatus,
     string Summary,
     string Observation);
}
