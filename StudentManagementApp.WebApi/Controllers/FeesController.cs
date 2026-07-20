using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;

namespace StudentManagementApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController(IFeeService feeService) : ControllerBase
    {
        private readonly IFeeService _feeService = feeService;

        [HttpGet]
        public ActionResult<ApiResponse<IEnumerable<FeeResponseDto>>> GetAll()
        {
            var fees = _feeService.GetAllFeeLedgers();

            var data = fees.Select(f => new FeeResponseDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                CourseId = f.CourseId,
                AmountDue = f.AmountDue,
                AmountPaid = f.AmountPaid,
                PaymentDate = f.PaymentDate,
                Remarks = f.Remarks,
                Status = (int)f.Status,
                RemainingBalance = f.RemainingBalance,
                IsFullySettled = f.IsFullySettled
            });

            return Ok(new ApiResponse<IEnumerable<FeeResponseDto>>
            {
                Message = "Fee records retrieved successfully.",
                Data = data
            });
        }

        [HttpGet("{id}")]
        public ActionResult<ApiResponse<Fee>> GetById(int id)
        {
            try
            {
                var fee = _feeService.GetFeeById(id);
                if (fee == null)
                    return NotFound(new ApiResponse { Message = $"Fee record with ID {id} not found." });

                return Ok(new ApiResponse<Fee>
                {
                    Message = "Fee record retrieved successfully.",
                    Data = fee
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpGet("statement")]
        public ActionResult<ApiResponse<Fee>> GetStatement([FromQuery] int studentId, [FromQuery] int courseId)
        {
            try
            {
                var feeStatement = _feeService.GetFeeStatement(studentId, courseId);
                if (feeStatement == null)
                {
                    return NotFound(new ApiResponse { Message = "No outstanding fee balance statement discovered matching those parameters." });
                }

                return Ok(new ApiResponse<Fee>
                {
                    Message = "Fee statement generated successfully.",
                    Data = feeStatement
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpPost("pay")]
        public ActionResult ProcessPayment([FromBody] FeeDto request)
        {
            try
            {
                _feeService.ProcessStudentPayment(
                    request.StudentId,
                    request.CourseId,
                    request.AmountPaid,
                    request.Remarks
                );
                return Ok(new ApiResponse { Message = "Payment processed successfully!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            return BadRequest(new ApiResponse
            {
                Message = "Operation Denied: Financial transaction records are non-erasable for accounting security. Please post a reversal or void adjustment transaction instead."
            });
        }
    }
}