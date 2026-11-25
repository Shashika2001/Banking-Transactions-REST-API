using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.ExceptionHandling;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 <see cref="ProblemDetails"/> responses.
/// Only messages from known, safe exception types reach the client; anything else becomes a generic 500
/// with no stack trace or internal details.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="problemDetailsService">Writes problem details responses.</param>
    /// <param name="logger">The logger.</param>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected; there is nobody to send a response to.
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var problem = CreateProblemDetails(exception);
        LogException(exception, problem.Status ?? StatusCodes.Status500InternalServerError);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails CreateProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => CreateValidationProblem(validation),
        DomainValidationException => CreateProblem(StatusCodes.Status400BadRequest, "Invalid request", exception),
        InvalidCredentialsException => CreateProblem(StatusCodes.Status401Unauthorized, "Authentication failed", exception),
        ForbiddenAccessException => CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", exception),
        NotFoundException => CreateProblem(StatusCodes.Status404NotFound, "Resource not found", exception),
        ConflictException => CreateProblem(StatusCodes.Status409Conflict, "Conflict", exception),
        DomainException => CreateProblem(StatusCodes.Status422UnprocessableEntity, "Business rule violation", exception),
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "An unexpected error occurred. Quote the traceId when contacting support.",
        },
    };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred",
            Extensions = { ["errorCode"] = "Validation" },
        };
    }

    private static ProblemDetails CreateProblem(int status, string title, Exception exception) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Extensions = { ["errorCode"] = ErrorCodeFor(exception) },
        };

    // "InsufficientFundsException" -> "InsufficientFunds": a stable, machine-readable code for clients.
    private static string ErrorCodeFor(Exception exception)
    {
        var name = exception.GetType().Name;
        return name.EndsWith(nameof(Exception), StringComparison.Ordinal) ? name[..^nameof(Exception).Length] : name;
    }

    private void LogException(Exception exception, int status)
    {
        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(exception);
        }
        else if (_logger.IsEnabled(LogLevel.Information))
        {
            var exceptionType = exception.GetType().Name;
            var reason = exception.Message;
            LogHandledException(status, exceptionType, reason);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing the request")]
    private partial void LogUnhandledException(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request failed with {StatusCode}: {ExceptionType}: {Reason}")]
    private partial void LogHandledException(int statusCode, string exceptionType, string reason);
}
