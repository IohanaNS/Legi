using FluentValidation;
using Legi.Identity.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Legi.SharedKernel;

namespace Legi.Identity.Api.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            LogException(context, ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private void LogException(HttpContext context, Exception exception)
    {
        switch (exception)
        {
            case ValidationException:
                logger.LogWarning(
                    "Validation failed for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;

            case UnauthorizedException:
            case UnauthorizedAccessException:
                logger.LogInformation(
                    "Unauthorized request for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);
                break;

            case HumanVerificationRequiredException:
            case EmailConfirmationRequiredException:
                logger.LogInformation(
                    "{ExceptionType} for {Method} {Path}",
                    exception.GetType().Name,
                    context.Request.Method,
                    context.Request.Path);
                break;

            case ConflictException:
            case DomainException:
                logger.LogWarning(
                    "{ExceptionType} for {Method} {Path}: {Message}",
                    exception.GetType().Name,
                    context.Request.Method,
                    context.Request.Path,
                    exception.Message);
                break;

            case NotFoundException:
                logger.LogInformation(
                    "Resource not found for {Method} {Path}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    exception.Message);
                break;

            default:
                logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
                break;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Extensions =
                {
                    ["errors"] = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                }
            },

            UnauthorizedException unauthorizedEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorizedEx.Message
            },

            HumanVerificationRequiredException humanVerificationEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Human Verification Required",
                Detail = humanVerificationEx.Message,
                Extensions =
                {
                    ["captchaRequired"] = true
                }
            },

            EmailConfirmationRequiredException emailConfirmationEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Forbidden,
                Title = "Email Confirmation Required",
                Detail = emailConfirmationEx.Message,
                Extensions =
                {
                    ["emailConfirmationRequired"] = true
                }
            },

            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = notFoundEx.Message
            },

            ConflictException conflictEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Conflict",
                Detail = conflictEx.Message
            },

            DomainException domainEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Domain Error",
                Detail = domainEx.Message
            },

            UnauthorizedAccessException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "Authentication required."
            },

            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }
}
