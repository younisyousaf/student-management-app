using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;

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
        public ActionResult<ApiResponse<IEnumerable<Enrollment>>> GetAll()
        {
            var enrollments = _enrollmentService.GetAllEnrollments();
            return Ok(new ApiResponse<IEnumerable<Enrollment>>
            {
                Message = "Enrollments retrieved successfully.",
                Data = enrollments
            });
        }

        [HttpGet("{id}")]
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
        public ActionResult Delete(int id)
        {
            _enrollmentService.DropCourse(id);
            return Ok(new ApiResponse { Message = "Enrollment records updated to dropped status." });
        }
    }

    public record DropRequest(int EnrollmentId);

    public record CompleteRequest(int EnrollmentId);
}