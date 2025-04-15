using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inventory.API
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                 await _next(context);
            }
            catch (Exception ex)
            {
                // Log exception details
                _logger.LogError(ex, "An unhandled exception has occurred.");
                
                // Prepare custom error response
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorResponse = _env.IsProduction()
                    ? new ErrorResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                    : new ErrorResponse
                    {
                        StatusCode = context.Response.StatusCode,
                        Message = "Internal Server Error. Please try again later."
                    };

                var jsonResponse = JsonSerializer.Serialize(errorResponse);
                _logger.LogError($"Error: {jsonResponse}");
                 await context.Response.WriteAsync(jsonResponse);
            }
        }
    }
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
