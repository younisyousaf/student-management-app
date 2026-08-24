using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentManagement.AI.Observability;
using StudentManagement.AI.Tools.Hosted;

namespace StudentManagement.AI.Agents;

public static class HostedStudentManagementToolFactory
{
    public static IList<AITool> Create(
        IServiceProvider services)
    {
        var studentTools =
            services.GetRequiredService<HostedStudentTools>();

        var courseTools =
            services.GetRequiredService<HostedCourseTools>();

        var enrollmentTools =
            services.GetRequiredService<HostedEnrollmentTools>();

        var attendanceTools =
            services.GetRequiredService<HostedAttendanceTools>();

        var feeTools =
            services.GetRequiredService<HostedFeeTools>();

        var knowledgeTools =
            services.GetRequiredService<HostedKnowledgeTools>();

        var logger =
            services.GetRequiredService<
                ILogger<TimedAIFunction>>();

        AIFunction Timed(
            AIFunction function)
        {
            return new TimedAIFunction(
                function,
                logger);
        }

        AIFunction RequiresApproval(
            AIFunction function)
        {
            return new ApprovalRequiredAIFunction(
                Timed(function));
        }

        // -------------------------
        // WRITE TOOLS
        // -------------------------

        AIFunction enrollStudent =
            RequiresApproval(
                AIFunctionFactory.Create(
                    enrollmentTools.EnrollStudent,
                    name: "enroll_student",
                    description:
                        "Enroll a student in a course. " +
                        "This operation modifies student enrollment data."));

        AIFunction markAttendance =
            RequiresApproval(
                AIFunctionFactory.Create(
                    attendanceTools.MarkAttendance,
                    name: "mark_attendance",
                    description:
                        "Mark attendance for a student on an explicitly supplied date. " +
                        "This operation modifies attendance data."));

        AIFunction markAttendanceToday =
            RequiresApproval(
                AIFunctionFactory.Create(
                    attendanceTools.MarkAttendanceToday,
                    name: "mark_attendance_today",
                    description:
                        "Mark today's attendance for a student in a course. " +
                        "The application determines today's date from its configured timezone."));

        AIFunction updateAttendance =
            RequiresApproval(
                AIFunctionFactory.Create(
                    attendanceTools.UpdateAttendance,
                    name: "update_attendance",
                    description:
                        "Update the status or remarks of an existing attendance record."));

        AIFunction processStudentPayment =
            RequiresApproval(
                AIFunctionFactory.Create(
                    feeTools.ProcessStudentPayment,
                    name: "process_student_payment",
                    description:
                        "Record a payment against a student's course fee. " +
                        "This modifies financial data."));

        AIFunction dropCourse =
            RequiresApproval(
                AIFunctionFactory.Create(
                    enrollmentTools.DropCourse,
                    name: "drop_course",
                    description:
                        "Drop an existing enrollment. " +
                        "This modifies enrollment data."));

        AIFunction completeCourse =
            RequiresApproval(
                AIFunctionFactory.Create(
                    enrollmentTools.CompleteCourse,
                    name: "complete_course",
                    description:
                        "Mark an existing enrollment as completed. " +
                        "This modifies enrollment data."));

        AIFunction updateStudentProfile =
            RequiresApproval(
                AIFunctionFactory.Create(
                    studentTools.UpdateStudentProfile,
                    name: "update_student_profile",
                    description:
                        "Update an existing student's profile. " +
                        "The exact student must be verified first."));

        AIFunction removeStudent =
            RequiresApproval(
                AIFunctionFactory.Create(
                    studentTools.RemoveStudent,
                    name: "remove_student",
                    description:
                        "Permanently remove a student. " +
                        "This is a destructive operation."));

        AIFunction updateCourseDetails =
            RequiresApproval(
                AIFunctionFactory.Create(
                    courseTools.UpdateCourseDetails,
                    name: "update_course_details",
                    description:
                        "Update details of an existing course."));

        AIFunction updateCoursePricing =
            RequiresApproval(
                AIFunctionFactory.Create(
                    courseTools.UpdateCoursePricing,
                    name: "update_course_pricing",
                    description:
                        "Update the fee amount of an existing course."));

        AIFunction removeCourse =
            RequiresApproval(
                AIFunctionFactory.Create(
                    courseTools.RemoveCourse,
                    name: "remove_course",
                    description:
                        "Permanently remove an existing course. " +
                        "This is a destructive operation."));

        // -------------------------
        // COMPLETE TOOL SET
        // -------------------------

        IList<AITool> tools =
        [
            // Students
            Timed(
                AIFunctionFactory.Create(
                    studentTools.GetStudentByRollNumber)),

            Timed(
                AIFunctionFactory.Create(
                    studentTools.SearchStudentsByName)),

            Timed(
                AIFunctionFactory.Create(
                    studentTools.GetStudentById)),

            // Courses
            Timed(
                AIFunctionFactory.Create(
                    courseTools.GetCourseById)),

            Timed(
                AIFunctionFactory.Create(
                    courseTools.GetCourseByCode)),

            Timed(
                AIFunctionFactory.Create(
                    courseTools.GetAllCourses)),

            // Enrollments
            Timed(
                AIFunctionFactory.Create(
                    enrollmentTools.GetEnrollmentsByStudent)),

            Timed(
                AIFunctionFactory.Create(
                    enrollmentTools.GetEnrollmentById)),

            // Attendance
            Timed(
                AIFunctionFactory.Create(
                    attendanceTools.GetAttendanceForStudent)),

            Timed(
                AIFunctionFactory.Create(
                    attendanceTools.GetAttendanceForCourseOnDate)),

            Timed(
                AIFunctionFactory.Create(
                    attendanceTools.GetAttendanceById)),

            Timed(
                AIFunctionFactory.Create(
                    attendanceTools.GetAttendanceSummaryForStudent)),

            // Fees
            Timed(
                AIFunctionFactory.Create(
                    feeTools.GetFeeById)),

            Timed(
                AIFunctionFactory.Create(
                    feeTools.GetFeeStatement)),

            // RAG
            Timed(
                AIFunctionFactory.Create(
                    knowledgeTools.SearchInstitutionalKnowledge)),

            // Approval-required writes
            enrollStudent,
            markAttendance,
            markAttendanceToday,
            updateAttendance,
            processStudentPayment,
            dropCourse,
            completeCourse,
            updateStudentProfile,
            removeStudent,
            updateCourseDetails,
            updateCoursePricing,
            removeCourse
        ];

        return tools;
    }
}
