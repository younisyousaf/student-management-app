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
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public ActionResult<ApiResponse<IEnumerable<Course>>> GetAll()
        {
            var courses = _courseService.GetAllCourses();
            return Ok(new ApiResponse<IEnumerable<Course>>
            {
                Message = "Courses retrieved successfully.",
                Data = courses
            });
        }

        [HttpGet("{id}")]
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
        }

        [HttpPut("{id}")]
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