using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using StudentManagement.AI.Agents;
using StudentManagement.AI.Configuration;
using StudentManagement.AI.Services;
using StudentManagement.AI.Sessions;
using StudentManagement.AI.Context;
using StudentManagement.AI.Tools;
using System.ClientModel;

namespace StudentManagement.AI.Extensions;

public static class AgentServiceCollectionExtensions
{
    public static IServiceCollection AddStudentManagementAI(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName));

        services.AddSingleton<IChatClient>(_ =>
        {
            var options = configuration.GetSection(OpenRouterOptions.SectionName).Get<OpenRouterOptions>()
                ?? new OpenRouterOptions();

            //var apiKey = !string.IsNullOrWhiteSpace(options.ApiKey)
            //    ? options.ApiKey
            //    : Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

            var apiKey = new[] { 
                //options.ApiKey,
                options.ApiKeyTwo
                //options.ApiKeyThree
            }
                .FirstOrDefault(k => !string.IsNullOrWhiteSpace(k))
                ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "OpenRouter API key not found. Set it via User Secrets (OpenRouter:ApiKey) or the OPENROUTER_API_KEY environment variable.");
            }

            var openAiClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) });

            return openAiClient.GetChatClient(options.Model).AsIChatClient();
        });
        services.AddScoped<ICopilotService, CopilotService>();
        //services.AddSingleton<ISessionStore, InMemorySessionStore>();

        services.AddScoped<StudentTools>();
        services.AddScoped<CourseTools>();
        services.AddScoped<EnrollmentTools>();
        services.AddScoped<AttendanceTools>();
        services.AddScoped<FeeTools>();
        services.AddScoped<AuthenticatedUserContextProvider>();
        services.AddScoped<IApplicationDateTime, ApplicationDateTime>();


        services.AddScoped<AIAgent>(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var studentTools = sp.GetRequiredService<StudentTools>();
            var courseTools = sp.GetRequiredService<CourseTools>();
            var enrollmentTools = sp.GetRequiredService<EnrollmentTools>();
            var attendanceTools = sp.GetRequiredService<AttendanceTools>();
            var feeTools = sp.GetRequiredService<FeeTools>();
            var authenticatedUserContext = sp.GetRequiredService<AuthenticatedUserContextProvider>();

            //Write Tools
            AIFunction enrollStudentFunction = AIFunctionFactory.Create( enrollmentTools.EnrollStudent, name: "enroll_student",
                description:
                    "Enroll a student in a course. " +
                    "This operation modifies student enrollment data.");
            AIFunction approvalRequiredEnrollStudent = new ApprovalRequiredAIFunction(enrollStudentFunction);

            //var markAttendanceFunction = AIFunctionFactory.Create(attendanceTools.MarkAttendance, name: "mark_attendance");
            //var markAttendanceWithApproval = new ApprovalRequiredAIFunction(markAttendanceFunction);
            var markAttendanceTodayFunction = 
            AIFunctionFactory.Create(attendanceTools.MarkAttendanceToday,name: "mark_attendance_today",
               description:
                "Mark today's attendance for a student in a course. " +
                "The application determines today's date from the configured timezone.");
            var markAttendanceTodayWithApproval = new ApprovalRequiredAIFunction(markAttendanceTodayFunction);
            var updateAttendanceFunction = 
            AIFunctionFactory.Create(attendanceTools.UpdateAttendance, name: "update_attendance",
                description:
                    "Update the status or remarks of an existing attendance record. " +
                    "First verify the exact attendance record using GetAttendanceById. " +
                    "This operation modifies application data.");
            var updateAttendanceWithApproval = new ApprovalRequiredAIFunction(updateAttendanceFunction);
            var processStudentPaymentFunction = 
            AIFunctionFactory.Create(feeTools.ProcessStudentPayment, name: "process_student_payment",
                description:
                    "Record a payment against a student's course fee. " +
                    "This modifies financial data and requires human approval.");
            var processStudentPaymentWithApproval = new ApprovalRequiredAIFunction(processStudentPaymentFunction);
            var dropCourseFunction =
            AIFunctionFactory.Create(enrollmentTools.DropCourse, name: "drop_course",
                description:
                    "Drop an existing enrollment. " +
                    "This operation modifies enrollment data.");
            var dropCourseWithApproval = new ApprovalRequiredAIFunction(dropCourseFunction);
            var completeCourseFunction =
            AIFunctionFactory.Create(enrollmentTools.CompleteCourse, name: "complete_course", 
                description: "Mark an existing enrollment as completed. " +
                "This operation modifies enrollment data.");
            var completeCourseWithApproval = new ApprovalRequiredAIFunction(completeCourseFunction);

            var updateStudentProfileFunction = 
            AIFunctionFactory.Create(studentTools.UpdateStudentProfile, name: "update_student_profile",
                description:
                    "Update an existing student's profile. " +
                    "The exact student must be verified first. " +
                    "This operation modifies student data.");
            var updateStudentProfileWithApproval = new ApprovalRequiredAIFunction(updateStudentProfileFunction);
            var removeStudentFunction =
            AIFunctionFactory.Create(studentTools.RemoveStudent, name: "remove_student",
                description:
                    "Permanently remove a student from the system. " +
                    "The exact student must be verified first. " +
                    "This is a destructive operation.");
            var removeStudentWithApproval = new ApprovalRequiredAIFunction(removeStudentFunction);
            var updateCourseDetailsFunction =
            AIFunctionFactory.Create(courseTools.UpdateCourseDetails, name: "update_course_details",
                description:
                    "Update one or more details of an existing course. " +
                    "The exact course must be verified first. " +
                    "This operation modifies course data.");
            var updateCourseDetailsWithApproval = new ApprovalRequiredAIFunction(updateCourseDetailsFunction);
            var updateCoursePricingFunction =
            AIFunctionFactory.Create(
                courseTools.UpdateCoursePricing,
                name: "update_course_pricing",
                description:
                    "Update the fee amount of an existing course. " +
                    "The exact course must be verified first. " +
                    "This operation modifies course pricing.");
            var updateCoursePricingWithApproval =
                new ApprovalRequiredAIFunction(
                    updateCoursePricingFunction);

            var removeCourseFunction =
            AIFunctionFactory.Create(
                courseTools.RemoveCourse,
                name: "remove_course",
                description:
                    "Permanently remove an existing course. " +
                    "The exact course must be verified first. " +
                    "This is a destructive operation.");
            var removeCourseWithApproval =
                new ApprovalRequiredAIFunction(
                    removeCourseFunction);

            IList<AITool> tools =
            [
                AIFunctionFactory.Create(studentTools.GetStudentByRollNumber),
                AIFunctionFactory.Create(studentTools.SearchStudentsByName),
                AIFunctionFactory.Create(studentTools.GetStudentById),

                AIFunctionFactory.Create(courseTools.GetCourseById),
                AIFunctionFactory.Create(courseTools.GetCourseByCode),
                AIFunctionFactory.Create(courseTools.GetAllCourses),

                AIFunctionFactory.Create(enrollmentTools.GetEnrollmentsByStudent),
                AIFunctionFactory.Create(enrollmentTools.GetEnrollmentById),
                //approval required tools
                approvalRequiredEnrollStudent,
                //markAttendanceWithApproval,
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

                AIFunctionFactory.Create(attendanceTools.GetAttendanceForStudent),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceForCourseOnDate),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceById),
                AIFunctionFactory.Create(attendanceTools.GetAttendanceSummaryForStudent),

                AIFunctionFactory.Create(feeTools.GetFeeById),
                AIFunctionFactory.Create(feeTools.GetFeeStatement)
            ];

            return StudentManagementAgent.Create(chatClient, tools, authenticatedUserContext);
        });

        return services;
    }
}