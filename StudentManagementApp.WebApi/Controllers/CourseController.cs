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
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        [RequirePermission(Permissions.Courses.Read)]
        public ActionResult<ApiResponse<IEnumerable<Course>>> GetAll()
        {
            var courses = _courseService.GetAllCourses();
            return Ok(new ApiResponse<IEnumerable<Course>>
            {
                Message = "Courses retrieved successfully.",
                Data = courses
            });
        }

        [HttpGet("paged")]
        [RequirePermission(Permissions.Courses.Read)]
        public async Task<ActionResult<
        ApiResponse<PaginatedResult<Course>>>>
        GetPaged(
            [FromQuery]
            PaginationQuery pagination,

            [FromServices]
            ManagementPaginationStore paginationStore,

            CancellationToken cancellationToken)
        {
            var result =
                await paginationStore
                    .GetCoursesAsync(
                        pagination.PageNumber,
                        pagination.PageSize,
                        cancellationToken);

            return Ok(
                new ApiResponse<
                    PaginatedResult<Course>>
                {
                    Message =
                        "Courses retrieved successfully.",

                    Data =
                        result
                });
        }

        [HttpGet("{id}")]
        [RequirePermission(Permissions.Courses.Read)]
        public ActionResult<ApiResponse<Course>> GetById(int id)
        {
            var course = _courseService.GetCourseById(id);
            if (course == null)
                return NotFound(new ApiResponse { Message = $"Course with ID {id} not found." });

            return Ok(new ApiResponse<Course>
            {
                Message = "Course retrieved successfully.",
                Data = course
            });
        }

        [HttpPost]
        [RequirePermission(Permissions.Courses.Create)]
        public ActionResult Create([FromBody] CreateCourseDto request)
        {
            try
            {
                var course = new Course(
                    request.Code,
                    request.Name,
                    request.DurationMonths,
                    request.FeeAmount
                );

                course.Description = request.Description;

                _courseService.CreateCourse(course);
                return CreatedAtAction(nameof(GetById), new { id = course.Id }, new ApiResponse<Course>
                {
                    Message = "Course created successfully.",
                    Data = course
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission(Permissions.Courses.Update)]
        public ActionResult Update(int id, [FromBody] CreateCourseDto request)
        {
            try
            {
                var existingCourse = _courseService.GetCourseById(id);
                if (existingCourse == null)
                {
                    return NotFound(new ApiResponse { Message = $"Course with ID {id} not found." });
                }

                _courseService.UpdateCourseDetails(id, request.Name, request.Description, request.DurationMonths);

                _courseService.UpdateCoursePricing(id, request.FeeAmount);

                return Ok(new ApiResponse
                {
                    Message = "Course profile updated successfully."
                });
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
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = $"Internal framework server failure: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission(Permissions.Courses.Delete)]
        public ActionResult Delete(int id)
        {
            try
            {
                var existingCourse = _courseService.GetCourseById(id);
                if (existingCourse == null)
                {
                    return NotFound(new ApiResponse { Message = $"Course with ID {id} not found." });
                }

                _courseService.RemoveCourse(id);

                return Ok(new ApiResponse
                {
                    Message = "Course program track dropped and deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = $"Cannot erase record: {ex.Message}" });
            }
        }
    }
}