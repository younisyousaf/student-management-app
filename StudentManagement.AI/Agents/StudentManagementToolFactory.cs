using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentManagement.AI.Observability;
using StudentManagement.AI.Tools;

namespace StudentManagement.AI.Agents;

public static class StudentManagementToolFactory
{
    public static IList<AITool> Create(
        IServiceProvider sp)
    {
        var studentTools =
            sp.GetRequiredService<StudentTools>();

        var courseTools =
            sp.GetRequiredService<CourseTools>();

        var enrollmentTools =
            sp.GetRequiredService<EnrollmentTools>();

        var attendanceTools =
            sp.GetRequiredService<AttendanceTools>();

        var feeTools =
            sp.GetRequiredService<FeeTools>();

        var knowledgeTools =
            sp.GetRequiredService<KnowledgeTools>();

        var toolLogger =
            sp.GetRequiredService<
                ILogger<TimedAIFunction>>();
        var reportingTools =
            sp.GetRequiredService<ReportingTools>();

        AIFunction Timed(
            AIFunction function) =>
                new TimedAIFunction(
                    function,
                    toolLogger);

        // -------------------------
        // Write tools
        // -------------------------

        AIFunction enrollStudentFunction =
            Timed(
                AIFunctionFactory.Create(
                    enrollmentTools.EnrollStudent,
                    name: "enroll_student",
                    description:
                        "Enroll a student in a course. " +
                        "This operation modifies student enrollment data."));

        AIFunction approvalRequiredEnrollStudent =
            new ApprovalRequiredAIFunction(
                enrollStudentFunction);

        AIFunction markAttendanceTodayFunction =
            AIFunctionFactory.Create(
                attendanceTools.MarkAttendanceToday,
                name: "mark_attendance_today",
                description:
                    "Mark today's attendance for a student in a course. " +
                    "The application determines today's date from the configured timezone.");

        AIFunction markAttendanceTodayWithApproval =
            new ApprovalRequiredAIFunction(
                markAttendanceTodayFunction);

        AIFunction updateAttendanceFunction =
            AIFunctionFactory.Create(
                attendanceTools.UpdateAttendance,
                name: "update_attendance",
                description:
                    "Update the status or remarks of an existing attendance record. " +
                    "First verify the exact attendance record using GetAttendanceById. " +
                    "This operation modifies application data.");

        AIFunction updateAttendanceWithApproval =
            new ApprovalRequiredAIFunction(
                updateAttendanceFunction);

        AIFunction processStudentPaymentFunction =
            AIFunctionFactory.Create(
                feeTools.ProcessStudentPayment,
                name: "process_student_payment",
                description:
                    "Record a payment against a student's course fee. " +
                    "This modifies financial data and requires human approval.");

        AIFunction processStudentPaymentWithApproval =
            new ApprovalRequiredAIFunction(
                processStudentPaymentFunction);

        AIFunction dropCourseFunction =
            AIFunctionFactory.Create(
                enrollmentTools.DropCourse,
                name: "drop_course",
                description:
                    "Drop an existing enrollment. " +
                    "This operation modifies enrollment data.");

        AIFunction dropCourseWithApproval =
            new ApprovalRequiredAIFunction(
                dropCourseFunction);

        AIFunction completeCourseFunction =
            AIFunctionFactory.Create(
                enrollmentTools.CompleteCourse,
                name: "complete_course",
                description:
                    "Mark an existing enrollment as completed. " +
                    "This operation modifies enrollment data.");

        AIFunction completeCourseWithApproval =
            new ApprovalRequiredAIFunction(
                completeCourseFunction);

        AIFunction updateStudentProfileFunction =
            AIFunctionFactory.Create(
                studentTools.UpdateStudentProfile,
                name: "update_student_profile",
                description:
                    "Update an existing student's profile. " +
                    "The exact student must be verified first. " +
                    "This operation modifies student data.");

        AIFunction updateStudentProfileWithApproval =
            new ApprovalRequiredAIFunction(
                updateStudentProfileFunction);

        AIFunction removeStudentFunction =
            AIFunctionFactory.Create(
                studentTools.RemoveStudent,
                name: "remove_student",
                description:
                    "Permanently remove a student from the system. " +
                    "The exact student must be verified first. " +
                    "This is a destructive operation.");

        AIFunction removeStudentWithApproval =
            new ApprovalRequiredAIFunction(
                removeStudentFunction);

        AIFunction updateCourseDetailsFunction =
            AIFunctionFactory.Create(
                courseTools.UpdateCourseDetails,
                name: "update_course_details",
                description:
                    "Update one or more details of an existing course. " +
                    "The exact course must be verified first. " +
                    "This operation modifies course data.");

        AIFunction updateCourseDetailsWithApproval =
            new ApprovalRequiredAIFunction(
                updateCourseDetailsFunction);

        AIFunction updateCoursePricingFunction =
            AIFunctionFactory.Create(
                courseTools.UpdateCoursePricing,
                name: "update_course_pricing",
                description:
                    "Update the fee amount of an existing course. " +
                    "The exact course must be verified first. " +
                    "This operation modifies course pricing.");

        AIFunction updateCoursePricingWithApproval =
            new ApprovalRequiredAIFunction(
                updateCoursePricingFunction);

        AIFunction removeCourseFunction =
            AIFunctionFactory.Create(
                courseTools.RemoveCourse,
                name: "remove_course",
                description:
                    "Permanently remove an existing course. " +
                    "The exact course must be verified first. " +
                    "This is a destructive operation.");

        AIFunction removeCourseWithApproval =
            new ApprovalRequiredAIFunction(
                removeCourseFunction);

        AIFunction createStudentFunction =
            Timed(
                AIFunctionFactory.Create(
                    studentTools.CreateStudent,
                    name: "create_student",
                    description:
                        "Create a new student record. " +
                        "All required student information must come from the user. " +
                        "This operation modifies student data."));

        AIFunction approvalRequiredCreateStudent =
            new ApprovalRequiredAIFunction(
                createStudentFunction);

        AIFunction createCourseFunction =
            Timed(
                AIFunctionFactory.Create(
                    courseTools.CreateCourse,
                    name: "create_course",
                    description:
                        "Create a new course. " +
                        "All required course information must come from the user. " +
                        "This operation modifies course data."));

        AIFunction approvalRequiredCreateCourse =
            new ApprovalRequiredAIFunction(
                createCourseFunction);

        // -------------------------
        // Read + write tool list
        // -------------------------

        return
        [
            AIFunctionFactory.Create(
                studentTools.GetStudentByRollNumber),

            AIFunctionFactory.Create(
                studentTools.SearchStudentsByName),

            Timed(
                AIFunctionFactory.Create(
                    studentTools.GetStudentById)),

            AIFunctionFactory.Create(
                courseTools.GetCourseById),

            AIFunctionFactory.Create(
                courseTools.GetCourseByCode),

            AIFunctionFactory.Create(
                courseTools.GetAllCourses),

            Timed(AIFunctionFactory.Create(
                courseTools.SearchCoursesByName)),

            AIFunctionFactory.Create(
                enrollmentTools.GetEnrollmentsByStudent),

            AIFunctionFactory.Create(
                enrollmentTools.GetEnrollmentById),

            Timed(AIFunctionFactory.Create(
                enrollmentTools.GetEnrollmentForStudentCourse)),

            Timed(AIFunctionFactory.Create(
                enrollmentTools.GetEnrollmentsByCourse)),

            approvalRequiredEnrollStudent,
            markAttendanceTodayWithApproval,
            updateAttendanceWithApproval,
            processStudentPaymentWithApproval,
            dropCourseWithApproval,
            completeCourseWithApproval,
            updateStudentProfileWithApproval,
            removeStudentWithApproval,
            updateCourseDetailsWithApproval,
            updateCoursePricingWithApproval,
            removeCourseWithApproval,
            approvalRequiredCreateStudent,
            approvalRequiredCreateCourse,

            AIFunctionFactory.Create(
                attendanceTools.GetAttendanceForStudent),

            AIFunctionFactory.Create(
                attendanceTools.GetAttendanceForCourseOnDate),

            AIFunctionFactory.Create(
                attendanceTools.GetAttendanceById),

            Timed(
                AIFunctionFactory.Create(
                    attendanceTools
                        .GetAttendanceSummaryForStudent)),

            AIFunctionFactory.Create(
                feeTools.GetFeeById),

            AIFunctionFactory.Create(
                feeTools.GetFeeStatement),

            Timed(AIFunctionFactory.Create(
                feeTools.GetFeesForStudent)),

            Timed(
                AIFunctionFactory.Create(
                    knowledgeTools
                        .SearchInstitutionalKnowledge)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetStudentsBelowAttendanceThreshold)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetStudentsWithOutstandingFees)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetCourseAttendanceSummary)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetStudentsWithNoAttendanceRecords)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetStudentsWithNoActiveEnrollment)),

            Timed(
                AIFunctionFactory.Create(
                    reportingTools
                        .GetInstitutionFeeSummary)),
        ];
    }
}
