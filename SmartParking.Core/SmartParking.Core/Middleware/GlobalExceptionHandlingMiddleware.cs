using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartParking.Core.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var statusCode = HttpStatusCode.InternalServerError;
            var errorMessage = "An unexpected error occurred.";
            
            // Customize response based on exception type
            if (exception is ArgumentException || exception is FormatException)
            {
                statusCode = HttpStatusCode.BadRequest;
                errorMessage = exception.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = HttpStatusCode.Unauthorized;
                errorMessage = "Unauthorized access.";
            }
            else if (exception is TimeoutException)
            {
                statusCode = HttpStatusCode.RequestTimeout;
                errorMessage = "The request timed out.";
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                errorMessage = "The requested resource was not found.";
            }
            
            // Set status code
            context.Response.StatusCode = (int)statusCode;
            
            // Create error response
            var errorResponse = new
            {
                status = (int)statusCode,
                error = errorMessage,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path
            };
            
            // Serialize and write response
            var jsonResponse = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
