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
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public ActionResult<ApiResponse<IEnumerable<Student>>> GetAll()
        {
            var students = _studentService.GetAllStudents();
            return Ok(new ApiResponse<IEnumerable<Student>>
            {
                Message = "Students retrieved successfully.",
                Data = students
            });
        }

        [HttpGet("{id}")]
        public ActionResult<ApiResponse<Student>> GetById(int id)
        {
            var student = _studentService.GetStudentById(id);
            if (student == null)
                return NotFound(new ApiResponse { Message = $"Student with ID {id} not found." });

            return Ok(new ApiResponse<Student>
            {
                Message = "Student retrieved successfully.",
                Data = student
            });
        }

        [HttpGet("roll/{rollNumber}")]
        public ActionResult<ApiResponse<Student>> GetByRollNumber(string rollNumber)
        {
            var student = _studentService.GetStudentByRollNumber(rollNumber);
            if (student == null)
                return NotFound(new ApiResponse { Message = $"Student with Roll Number '{rollNumber}' not found." });

            return Ok(new ApiResponse<Student>
            {
                Message = "Student retrieved successfully.",
                Data = student
            });
        }

        [HttpPost]
        public ActionResult Create([FromBody] CreateStudentDto request)
        {
            try
            {
                var student = new Student(
                    request.RollNumber,
                    request.FirstName,
                    request.LastName,
                    request.Email,
                    request.DateOfBirth
                );

                if (!string.IsNullOrEmpty(request.Phone) || !string.IsNullOrEmpty(request.Address))
                {
                    student.UpdateProfile(student.FirstName, student.LastName, request.Phone, request.Address);
                }

                _studentService.RegisterStudent(student);
                return CreatedAtAction(nameof(GetById), new { id = student.Id }, new ApiResponse<Student>
                {
                    Message = "Student registered successfully.",
                    Data = student
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
        public ActionResult Update(int id, [FromBody] Student student)
        {
            if (id != student.Id)
            {
                return BadRequest(new ApiResponse { Message = "ID mismatch in request path and body." });
            }

            try
            {
                _studentService.UpdateStudentProfile(
                    student.Id,
                    student.FirstName,
                    student.LastName,
                    student.Phone,
                    student.Address,
                    student.Email
                );
                return Ok(new ApiResponse { Message = "Student profile updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                _studentService.RemoveStudent(id);
                return Ok(new ApiResponse { Message = "Student profile removed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
        }
    }
}