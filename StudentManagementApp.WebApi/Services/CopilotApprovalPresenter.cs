using System.Globalization;
using System.Text.Json;
using StudentManagement.Core.Interfaces;

namespace StudentManagementApp.WebApi.Services;

public sealed record CopilotApprovalDisplayItem(
    string Label,
    string Value);

public sealed record CopilotApprovalPresentation(
    string Title,
    IReadOnlyList<CopilotApprovalDisplayItem> Details,
    string? Warning = null);

public sealed class CopilotApprovalPresenter
{
    private readonly IStudentService _studentService;
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IAttendanceService _attendanceService;

    public CopilotApprovalPresenter(
        IStudentService studentService,
        ICourseService courseService,
        IEnrollmentService enrollmentService,
        IAttendanceService attendanceService)
    {
        _studentService = studentService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
        _attendanceService = attendanceService;
    }

    public CopilotApprovalPresentation Present(
        string toolName,
        object? arguments)
    {
        var args = JsonSerializer.SerializeToElement(
            arguments ?? new { });

        return toolName switch
        {
            "create_student" => PresentCreateStudent(args),
            "create_course" => PresentCreateCourse(args),
            "enroll_student" => PresentEnrollStudent(args),
            "mark_attendance" => PresentMarkAttendance(args, false),
            "mark_attendance_today" => PresentMarkAttendance(args, true),
            "update_attendance" => PresentUpdateAttendance(args),
            "process_student_payment" => PresentPayment(args),
            "drop_course" => PresentEnrollmentChange(args, "Drop Course"),
            "complete_course" => PresentEnrollmentChange(args, "Complete Course"),
            "update_student_profile" => PresentUpdateStudent(args),
            "remove_student" => PresentRemoveStudent(args),
            "update_course_details" => PresentUpdateCourse(args),
            "update_course_pricing" => PresentUpdateCoursePricing(args),
            "remove_course" => PresentRemoveCourse(args),

            _ => new CopilotApprovalPresentation(
                Humanize(toolName),
                [],
                "Review this action carefully before approving.")
        };
    }

    private CopilotApprovalPresentation PresentCreateStudent(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var firstName = GetString(args, "firstName");
        var lastName = GetString(args, "lastName");

        Add(
            details,
            "Student",
            JoinName(firstName, lastName));

        Add(details, "Roll Number", GetString(args, "rollNumber"));
        Add(details, "Email", GetString(args, "email"));
        Add(details, "Date of Birth", GetDate(args, "dateOfBirth"));
        Add(details, "Phone", GetString(args, "phone"));
        Add(details, "Address", GetString(args, "address"));

        return new CopilotApprovalPresentation(
            "Create Student",
            details);
    }

    private CopilotApprovalPresentation PresentCreateCourse(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(details, "Course", GetString(args, "name"));
        Add(details, "Code", GetString(args, "code"));

        var duration = GetInt(args, "durationMonths");

        if (duration.HasValue)
        {
            Add(
                details,
                "Duration",
                $"{duration.Value} month{(duration.Value == 1 ? "" : "s")}");
        }

        var fee = GetDecimal(args, "feeAmount");

        if (fee.HasValue)
        {
            Add(details, "Fee", FormatAmount(fee.Value));
        }

        Add(details, "Description", GetString(args, "description"));

        return new CopilotApprovalPresentation(
            "Create Course",
            details);
    }

    private CopilotApprovalPresentation PresentEnrollStudent(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(
            details,
            "Student",
            GetStudentDisplay(
                GetInt(args, "studentId")));

        Add(
            details,
            "Course",
            GetCourseDisplay(
                GetInt(args, "courseId")));

        return new CopilotApprovalPresentation(
            "Enroll Student",
            details);
    }

    private CopilotApprovalPresentation PresentMarkAttendance(
        JsonElement args,
        bool today)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(
            details,
            "Student",
            GetStudentDisplay(
                GetInt(args, "studentId")));

        Add(
            details,
            "Course",
            GetCourseDisplay(
                GetInt(args, "courseId")));

        Add(
            details,
            "Date",
            today
                ? "Today"
                : GetDate(args, "date"));

        Add(
            details,
            "Status",
            GetString(args, "status"));

        Add(
            details,
            "Remarks",
            GetString(args, "remarks"));

        return new CopilotApprovalPresentation(
            today
                ? "Mark Today's Attendance"
                : "Mark Attendance",
            details);
    }

    private CopilotApprovalPresentation PresentUpdateAttendance(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var attendanceId =
            GetInt(args, "attendanceId");

        if (attendanceId.HasValue)
        {
            var attendance =
                _attendanceService.GetAttendanceById(
                    attendanceId.Value);

            if (attendance is not null)
            {
                Add(
                    details,
                    "Student",
                    GetStudentDisplay(
                        attendance.StudentId));

                Add(
                    details,
                    "Course",
                    GetCourseDisplay(
                        attendance.CourseId));

                Add(
                    details,
                    "Attendance Date",
                    attendance.Date.ToString(
                        "yyyy-MM-dd"));
            }
        }

        Add(
            details,
            "New Status",
            GetString(args, "status"));

        Add(
            details,
            "Remarks",
            GetString(args, "remarks"));

        return new CopilotApprovalPresentation(
            "Update Attendance",
            details);
    }

    private CopilotApprovalPresentation PresentPayment(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(
            details,
            "Student",
            GetStudentDisplay(
                GetInt(args, "studentId")));

        Add(
            details,
            "Course",
            GetCourseDisplay(
                GetInt(args, "courseId")));

        var amount =
            GetDecimal(args, "amount");

        if (amount.HasValue)
        {
            Add(
                details,
                "Amount",
                FormatAmount(amount.Value));
        }

        Add(
            details,
            "Remarks",
            GetString(args, "remarks"));

        return new CopilotApprovalPresentation(
            "Record Payment",
            details,
            "This action records a financial transaction.");
    }

    private CopilotApprovalPresentation PresentEnrollmentChange(
        JsonElement args,
        string title)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var enrollmentId =
            GetInt(args, "enrollmentId");

        if (enrollmentId.HasValue)
        {
            var enrollment =
                _enrollmentService.GetEnrollmentById(
                    enrollmentId.Value);

            if (enrollment is not null)
            {
                Add(
                    details,
                    "Student",
                    GetStudentDisplay(
                        enrollment.StudentId));

                Add(
                    details,
                    "Course",
                    GetCourseDisplay(
                        enrollment.CourseId));

                Add(
                    details,
                    "Current Status",
                    enrollment.Status);
            }
        }

        return new CopilotApprovalPresentation(
            title,
            details);
    }

    private CopilotApprovalPresentation PresentUpdateStudent(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var studentId =
            GetInt(args, "studentId");

        Add(
            details,
            "Student",
            GetStudentDisplay(studentId));

        AddIfPresent(
            details,
            args,
            "firstName",
            "First Name");

        AddIfPresent(
            details,
            args,
            "lastName",
            "Last Name");

        AddIfPresent(
            details,
            args,
            "email",
            "Email");

        AddIfPresent(
            details,
            args,
            "phone",
            "Phone");

        AddIfPresent(
            details,
            args,
            "address",
            "Address");

        return new CopilotApprovalPresentation(
            "Update Student Profile",
            details);
    }

    private CopilotApprovalPresentation PresentRemoveStudent(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var studentId =
            GetInt(args, "studentId");

        Add(
            details,
            "Student",
            GetStudentDisplay(studentId));

        return new CopilotApprovalPresentation(
            "Remove Student",
            details,
            "This permanently removes the student from the system.");
    }

    private CopilotApprovalPresentation PresentUpdateCourse(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        var courseId =
            GetInt(args, "courseId");

        Add(
            details,
            "Course",
            GetCourseDisplay(courseId));

        AddIfPresent(
            details,
            args,
            "name",
            "New Name");

        AddIfPresent(
            details,
            args,
            "description",
            "Description");

        var duration =
            GetInt(args, "durationMonths");

        if (duration.HasValue)
        {
            Add(
                details,
                "Duration",
                $"{duration.Value} month{(duration.Value == 1 ? "" : "s")}");
        }

        return new CopilotApprovalPresentation(
            "Update Course Details",
            details);
    }

    private CopilotApprovalPresentation PresentUpdateCoursePricing(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(
            details,
            "Course",
            GetCourseDisplay(
                GetInt(args, "courseId")));

        var amount =
            GetDecimal(args, "newFeeAmount");

        if (amount.HasValue)
        {
            Add(
                details,
                "New Fee",
                FormatAmount(amount.Value));
        }

        return new CopilotApprovalPresentation(
            "Update Course Pricing",
            details);
    }

    private CopilotApprovalPresentation PresentRemoveCourse(
        JsonElement args)
    {
        var details = new List<CopilotApprovalDisplayItem>();

        Add(
            details,
            "Course",
            GetCourseDisplay(
                GetInt(args, "courseId")));

        return new CopilotApprovalPresentation(
            "Remove Course",
            details,
            "This permanently removes the course from the system.");
    }

    private string? GetStudentDisplay(
        int? studentId)
    {
        if (!studentId.HasValue)
        {
            return null;
        }

        var student =
            _studentService.GetStudentById(
                studentId.Value);

        if (student is null)
        {
            return "Student record unavailable";
        }

        return string.IsNullOrWhiteSpace(
            student.RollNumber)
            ? student.FullName
            : $"{student.FullName} ({student.RollNumber})";
    }

    private string? GetCourseDisplay(
        int? courseId)
    {
        if (!courseId.HasValue)
        {
            return null;
        }

        var course =
            _courseService.GetCourseById(
                courseId.Value);

        if (course is null)
        {
            return "Course record unavailable";
        }

        return string.IsNullOrWhiteSpace(
            course.Code)
            ? course.Name
            : $"{course.Name} ({course.Code})";
    }

    private static void AddIfPresent(
        List<CopilotApprovalDisplayItem> details,
        JsonElement args,
        string propertyName,
        string label)
    {
        var value =
            GetString(args, propertyName);

        Add(details, label, value);
    }

    private static void Add(
        List<CopilotApprovalDisplayItem> details,
        string label,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        details.Add(
            new CopilotApprovalDisplayItem(
                label,
                value));
    }

    private static JsonElement? Find(
        JsonElement args,
        string propertyName)
    {
        if (
            args.ValueKind !=
            JsonValueKind.Object)
        {
            return null;
        }

        foreach (
            var property
            in args.EnumerateObject())
        {
            if (
                string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string? GetString(
        JsonElement args,
        string propertyName)
    {
        var value =
            Find(args, propertyName);

        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.ValueKind switch
        {
            JsonValueKind.String =>
                value.Value.GetString(),

            JsonValueKind.Number =>
                value.Value.GetRawText(),

            JsonValueKind.True =>
                "Yes",

            JsonValueKind.False =>
                "No",

            _ => null
        };
    }

    private static int? GetInt(
        JsonElement args,
        string propertyName)
    {
        var value =
            Find(args, propertyName);

        if (!value.HasValue)
        {
            return null;
        }

        if (
            value.Value.ValueKind ==
                JsonValueKind.Number &&
            value.Value.TryGetInt32(
                out var number))
        {
            return number;
        }

        if (
            value.Value.ValueKind ==
                JsonValueKind.String &&
            int.TryParse(
                value.Value.GetString(),
                out number))
        {
            return number;
        }

        return null;
    }

    private static decimal? GetDecimal(
        JsonElement args,
        string propertyName)
    {
        var value =
            Find(args, propertyName);

        if (!value.HasValue)
        {
            return null;
        }

        if (
            value.Value.ValueKind ==
                JsonValueKind.Number &&
            value.Value.TryGetDecimal(
                out var number))
        {
            return number;
        }

        if (
            value.Value.ValueKind ==
                JsonValueKind.String &&
            decimal.TryParse(
                value.Value.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out number))
        {
            return number;
        }

        return null;
    }

    private static string? GetDate(
        JsonElement args,
        string propertyName)
    {
        var value =
            GetString(args, propertyName);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var date)
                ? date.ToString("yyyy-MM-dd")
                : value;
    }

    private static string JoinName(
        string? firstName,
        string? lastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                lastName
            }
            .Where(
                value =>
                    !string.IsNullOrWhiteSpace(
                        value)));
    }

    private static string FormatAmount(
        decimal amount)
    {
        return amount.ToString(
            "N2",
            CultureInfo.InvariantCulture);
    }

    private static string Humanize(
        string value)
    {
        return string.Join(
            " ",
            value.Split(
                    '_',
                    StringSplitOptions
                        .RemoveEmptyEntries)
                .Select(
                    word =>
                        char.ToUpperInvariant(
                            word[0]) +
                        word[1..]));
    }
}