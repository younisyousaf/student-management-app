using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;
using StudentManagementApp.WebApi.Services;
using StudentManagement.Core.Security;
using StudentManagementApp.WebApi.Security;

namespace StudentManagementApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentsController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpGet]
        [RequirePermission(Permissions.Enrollments.Read)]
        public ActionResult<ApiResponse<IEnumerable<Enrollment>>> GetAll()
        {
            var enrollments = _enrollmentService.GetAllEnrollments();
            return Ok(new ApiResponse<IEnumerable<Enrollment>>
            {
                Message = "Enrollments retrieved successfully.",
                Data = enrollments
            });
        }

        [HttpGet("paged")]
        [RequirePermission(Permissions.Enrollments.Read)]
        public async Task<ActionResult<
            ApiResponse<
            PaginatedResult<Enrollment>>>>
        GetPaged(
            [FromQuery]
            PaginationQuery pagination,

            [FromServices]
            ManagementPaginationStore paginationStore,

            CancellationToken cancellationToken)
        {
            var result =
                await paginationStore
                    .GetEnrollmentsAsync(
                        pagination.PageNumber,
                        pagination.PageSize,
                        cancellationToken);

            return Ok(
                new ApiResponse<
                    PaginatedResult<Enrollment>>
                {
                    Message =
                        "Enrollments retrieved successfully.",

                    Data =
                        result
                });
        }

        [HttpGet("{id}")]
        [RequirePermission(Permissions.Enrollments.Read)]
        public ActionResult<ApiResponse<Enrollment>> GetById(int id)
        {
            try
            {
                var enrollment = _enrollmentService.GetEnrollmentById(id);
                if (enrollment == null)
                    return NotFound(new ApiResponse { Message = $"Enrollment record #{id} not found." });

                return Ok(new ApiResponse<Enrollment>
                {
                    Message = "Enrollment retrieved successfully.",
                    Data = enrollment
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpGet("student/{studentId}")]
        [RequirePermission(Permissions.Enrollments.Read)]
        public ActionResult<ApiResponse<IEnumerable<Enrollment>>> GetByStudent(int studentId)
        {
            try
            {
                var enrollments = _enrollmentService.GetEnrollmentsByStudent(studentId);
                return Ok(new ApiResponse<IEnumerable<Enrollment>>
                {
                    Message = $"Enrollments for Student #{studentId} retrieved successfully.",
                    Data = enrollments
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission(Permissions.Enrollments.Create)]
        public ActionResult Enroll([FromBody] EnrollStudentDto request)
        {
            try
            {
                _enrollmentService.EnrollStudent(request.StudentId, request.CourseId);
                return Ok(new ApiResponse { Message = "Student enrolled successfully!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPost("drop")]
        [RequirePermission(Permissions.Enrollments.Drop)]
        public ActionResult Drop([FromBody] DropRequest request)
        {
            try
            {
                _enrollmentService.DropCourse(request.EnrollmentId);
                return Ok(new ApiResponse { Message = "Course dropped successfully!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPost("complete")]
        [RequirePermission(Permissions.Enrollments.Complete)]
        public ActionResult Complete([FromBody] CompleteRequest request)
        {
            try
            {
                _enrollmentService.CompleteCourse(request.EnrollmentId);
                return Ok(new ApiResponse { Message = "Course marked as completed successfully!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission(Permissions.Enrollments.Drop)]
        public ActionResult Delete(int id)
        {
            _enrollmentService.DropCourse(id);
            return Ok(new ApiResponse { Message = "Enrollment records updated to dropped status." });
        }
    }

    public record DropRequest(int EnrollmentId);

    public record CompleteRequest(int EnrollmentId);
}