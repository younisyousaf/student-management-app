using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;
using StudentManagementApp.WebApi.Services;

namespace StudentManagementApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpGet]
        public ActionResult<ApiResponse<IEnumerable<AttendanceResponseDto>>> GetAll()
        {
            var records = _attendanceService.GetAllAttendance();

            return Ok(new ApiResponse<IEnumerable<AttendanceResponseDto>>
            {
                Message = "Attendance records retrieved successfully.",
                Data = records.Select(ToDto)
            });
        }

        [HttpGet("paged")]
        public async Task<ActionResult<
    ApiResponse<
        PaginatedResult<
            AttendanceResponseDto>>>>
    GetPaged(
        [FromQuery]
        PaginationQuery pagination,

        [FromServices]
        ManagementPaginationStore paginationStore,

        CancellationToken cancellationToken)
        {
            var result =
                await paginationStore
                    .GetAttendanceAsync(
                        pagination.PageNumber,
                        pagination.PageSize,
                        cancellationToken);

            var items =
                result.Items
                    .Select(ToDto)
                    .ToList();

            var paginatedResult =
                new PaginatedResult<
                    AttendanceResponseDto>(
                        items,
                        result.PageNumber,
                        result.PageSize,
                        result.TotalCount);

            return Ok(
                new ApiResponse<
                    PaginatedResult<
                        AttendanceResponseDto>>
                {
                    Message =
                        "Attendance records retrieved successfully.",

                    Data =
                        paginatedResult
                });
        }

        [HttpGet("{id}")]
        public ActionResult<ApiResponse<AttendanceResponseDto>> GetById(int id)
        {
            var record = _attendanceService.GetAttendanceById(id);
            if (record == null)
                return NotFound(new ApiResponse { Message = $"Attendance record with ID {id} not found." });

            return Ok(new ApiResponse<AttendanceResponseDto>
            {
                Message = "Attendance record retrieved successfully.",
                Data = ToDto(record)
            });
        }

        [HttpGet("student/{studentId}")]
        public ActionResult<ApiResponse<IEnumerable<AttendanceResponseDto>>> GetByStudent(int studentId)
        {
            var records = _attendanceService.GetAttendanceForStudent(studentId);

            return Ok(new ApiResponse<IEnumerable<AttendanceResponseDto>>
            {
                Message = "Attendance records retrieved successfully.",
                Data = records.Select(ToDto)
            });
        }

        [HttpGet("course")]
        public ActionResult<ApiResponse<IEnumerable<AttendanceResponseDto>>> GetByCourseOnDate([FromQuery] int courseId, [FromQuery] DateTime date)
        {
            var records = _attendanceService.GetAttendanceForCourseOnDate(courseId, date);

            return Ok(new ApiResponse<IEnumerable<AttendanceResponseDto>>
            {
                Message = "Attendance records retrieved successfully.",
                Data = records.Select(ToDto)
            });
        }

        [HttpPost]
        public ActionResult Mark([FromBody] MarkAttendanceDto request)
        {
            try
            {
                _attendanceService.MarkAttendance(
                    request.StudentId,
                    request.CourseId,
                    request.Date,
                    request.Status,
                    request.Remarks
                );

                return Ok(new ApiResponse { Message = "Attendance marked successfully." });
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
        public ActionResult Update(int id, [FromBody] UpdateAttendanceDto request)
        {
            try
            {
                _attendanceService.UpdateAttendance(id, request.Status, request.Remarks);
                return Ok(new ApiResponse { Message = "Attendance record updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse { Message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        private static AttendanceResponseDto ToDto(Attendance a) => new()
        {
            Id = a.Id,
            StudentId = a.StudentId,
            CourseId = a.CourseId,
            Date = a.Date,
            Status = (int)a.Status,
            Remarks = a.Remarks
        };
    }
}