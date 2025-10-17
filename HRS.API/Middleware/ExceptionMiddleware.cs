using System.Net;
using System.Text.Json;
using FluentValidation;
using HRS.API.Common;
using HRS.API.Contracts.DTOs;

namespace HRS.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.FailResponse(
                string.Join(", ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))
            );

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var response = ApiResponse<string>.FailResponse(
                "Email or password is incorrect",
                new List<string> { ex.Message }
            );

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
        }
        catch (InvalidOperationException ex)
        {
            // print ex to console
            Console.WriteLine("InvalidOperationException: " + ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = ApiResponse<string>.FailResponse(
                ex.Message,
                new List<string> { ex.Message }
            );

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<string>.FailResponse(
                "An unexpected error occurred.",
                new List<string> { ex.Message }
            );

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
        }
    }
}
