using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyInventory.Application.Common.Exceptions;

namespace PropertyInventory.Api.Middleware;

/// <summary>
/// Maps application and persistence exceptions to ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validationException => CreateProblem(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation Error",
                validationException.Message,
                validationException.Errors),
            NotFoundException notFoundException => CreateProblem(
                httpContext,
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundException.Message),
            ConflictException conflictException => CreateProblem(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Message),
            DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException) => CreateProblem(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                "A conflicting record already exists."),
            DbUpdateException dbUpdateException => CreateProblem(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Persistence Error",
                "The data could not be saved."),
            _ => null
        };

        if (problem is null)
        {
            _logger.LogError(exception, "Unhandled exception.");
            problem = CreateProblem(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Server Error",
                "An unexpected error occurred.");
        }
        else if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Persistence or server failure.");
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblem(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("IX_Contacts_Email", StringComparison.OrdinalIgnoreCase);
    }
}
