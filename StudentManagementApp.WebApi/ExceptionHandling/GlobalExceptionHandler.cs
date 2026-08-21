using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.AI.Reliability;
using StudentManagement.AI.Sessions;

namespace StudentManagementApp.WebApi.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails =
            exception switch
            {
                SessionStoreUnavailableException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status503ServiceUnavailable,

                        Title =
                            "Session service unavailable",

                        Detail =
                            "The Copilot session service is temporarily unavailable. Please try again later."
                    },

                AIProviderUnavailableException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status503ServiceUnavailable,

                        Title =
                            "AI service unavailable",

                        Detail =
                            "The AI service is temporarily unavailable. Please try again later."
                    },

                KeyNotFoundException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status404NotFound,

                        Title =
                            "Resource not found",

                        Detail =
                            exception.Message
                    },

                ArgumentException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status400BadRequest,

                        Title =
                            "Invalid request",

                        Detail =
                            exception.Message
                    },

                InvalidOperationException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status409Conflict,

                        Title =
                            "Operation conflict",

                        Detail =
                            exception.Message
                    },

                OperationCanceledException =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status408RequestTimeout,

                        Title =
                            "Request cancelled",

                        Detail =
                            "The request was cancelled before it could be completed."
                    },

                _ =>
                    new ProblemDetails
                    {
                        Status =
                            StatusCodes.Status500InternalServerError,

                        Title =
                            "Internal server error",

                        Detail =
                            "An unexpected error occurred while processing the request."
                    }
            };

        problemDetails.Instance =
            httpContext.Request.Path;

        if (problemDetails.Status >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. Method: {Method}, Path: {Path}",
                problemDetails.Status,
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode =
            problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}
