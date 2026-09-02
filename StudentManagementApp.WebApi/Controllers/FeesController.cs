using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagementApp.WebApi.DTOs;
using StudentManagementApp.WebApi.Services;
using StudentManagement.Core.Security;
using StudentManagementApp.WebApi.Security;

namespace StudentManagementApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController(IFeeService feeService) : ControllerBase
    {
        private readonly IFeeService _feeService = feeService;

        [HttpGet]
        [RequirePermission(Permissions.Fees.Read)]
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

        [HttpGet("paged")]
        [RequirePermission(Permissions.Fees.Read)]
        public async Task<ActionResult<
            ApiResponse<
            PaginatedResult<
            FeeResponseDto>>>>
        GetPaged(
            [FromQuery]
            PaginationQuery pagination,

            [FromServices]
            ManagementPaginationStore paginationStore,

            CancellationToken cancellationToken)
        {
            var result =
                await paginationStore
                    .GetFeesAsync(
                        pagination.PageNumber,
                        pagination.PageSize,
                        cancellationToken);

            var items =
                result.Items
                    .Select(
                        fee =>
                            new FeeResponseDto
                            {
                                Id =
                                    fee.Id,

                                StudentId =
                                    fee.StudentId,

                                CourseId =
                                    fee.CourseId,

                                AmountDue =
                                    fee.AmountDue,

                                AmountPaid =
                                    fee.AmountPaid,

                                PaymentDate =
                                    fee.PaymentDate,

                                Remarks =
                                    fee.Remarks,

                                Status =
                                    (int)fee.Status,

                                RemainingBalance =
                                    fee.RemainingBalance,

                                IsFullySettled =
                                    fee.IsFullySettled
                            })
                    .ToList();

            var paginatedResult =
                new PaginatedResult<
                    FeeResponseDto>(
                        items,
                        result.PageNumber,
                        result.PageSize,
                        result.TotalCount);

            return Ok(
                new ApiResponse<
                    PaginatedResult<
                        FeeResponseDto>>
                {
                    Message =
                        "Fee records retrieved successfully.",

                    Data =
                        paginatedResult
                });
        }

        [HttpGet("{id}")]
        [RequirePermission(Permissions.Fees.Read)]
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
        [RequirePermission(Permissions.Fees.Read)]
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
        [RequirePermission(Permissions.Fees.RecordPayment)]
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
        [RequirePermission(Permissions.Fees.Read)]
        public ActionResult Delete(int id)
        {
            return BadRequest(new ApiResponse
            {
                Message = "Operation Denied: Financial transaction records are non-erasable for accounting security. Please post a reversal or void adjustment transaction instead."
            });
        }
    }
}